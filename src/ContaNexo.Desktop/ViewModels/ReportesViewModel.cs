using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ContaNexo.Core.Calculations;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;
using ContaNexo.Desktop.Reporting;
using Microsoft.Win32;

namespace ContaNexo.Desktop.ViewModels;

public enum TipoReporte
{
    LibroDiario,
    LibroMayor,
    BalanceSumasSaldos,
    EstadoResultados,
    BalanceGeneral,
    TodosLosReportes
}

public sealed record ReporteOpcion(TipoReporte Tipo, string Nombre);

public sealed class ReportesViewModel : ViewModelBase
{
    public static IReadOnlyList<ReporteOpcion> OpcionesReporte { get; } =
    [
        new(TipoReporte.LibroDiario, "Libro Diario"),
        new(TipoReporte.LibroMayor, "Libro Mayor"),
        new(TipoReporte.BalanceSumasSaldos, "Balance de Sumas y Saldos"),
        new(TipoReporte.EstadoResultados, "Estado de Resultados"),
        new(TipoReporte.BalanceGeneral, "Balance General"),
        new(TipoReporte.TodosLosReportes, "Todos los reportes (PDF único)")
    ];

    private readonly Func<Empresa?> _obtenerEmpresaActual;
    private readonly Func<int, Task<IEnumerable<PeriodoContableListado>>> _listarPeriodosAsync;
    private readonly RepositorioAsiento? _repositorioAsiento;
    private readonly RepositorioLibroMayor? _repositorioLibroMayor;
    private readonly ReportesPdfGenerador _generador;
    private readonly ComandoAsync _generarPdfCommand;
    private PeriodoContableListado? _periodoSeleccionado;
    private ReporteOpcion? _reporteSeleccionado;
    private bool _estaCargando;
    private bool _estaGenerando;
    private string _mensajeError = string.Empty;
    private string _mensajeExito = string.Empty;

    public ReportesViewModel(
        RepositorioPeriodoContable repositorioPeriodoContable,
        RepositorioAsiento repositorioAsiento,
        RepositorioLibroMayor repositorioLibroMayor,
        Func<Empresa?> obtenerEmpresaActual)
        : this(
            obtenerEmpresaActual,
            idEmpresa => repositorioPeriodoContable.ListarAsync(idEmpresa),
            repositorioAsiento,
            repositorioLibroMayor,
            new ReportesPdfGenerador())
    {
    }

    private ReportesViewModel(
        Func<Empresa?> obtenerEmpresaActual,
        Func<int, Task<IEnumerable<PeriodoContableListado>>> listarPeriodosAsync,
        RepositorioAsiento? repositorioAsiento,
        RepositorioLibroMayor? repositorioLibroMayor,
        ReportesPdfGenerador generador)
    {
        _obtenerEmpresaActual = obtenerEmpresaActual;
        _listarPeriodosAsync = listarPeriodosAsync;
        _repositorioAsiento = repositorioAsiento;
        _repositorioLibroMayor = repositorioLibroMayor;
        _generador = generador;
        _reporteSeleccionado = OpcionesReporte[0];
        _generarPdfCommand = new ComandoAsync(GenerarPdfAsync, PuedeGenerarPdf);
    }

    internal static ReportesViewModel CrearParaPruebas(
        Func<Empresa?> obtenerEmpresaActual,
        Func<int, Task<IEnumerable<PeriodoContableListado>>> listarPeriodosAsync) =>
        new(obtenerEmpresaActual, listarPeriodosAsync, null, null, new ReportesPdfGenerador());

    public ObservableCollection<PeriodoContableListado> Periodos { get; } = new();

    public Empresa? EmpresaActual { get; private set; }

    public bool TieneEmpresa => EmpresaActual is not null;

    public bool TienePeriodos => Periodos.Count > 0;

    public PeriodoContableListado? PeriodoSeleccionado
    {
        get => _periodoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref _periodoSeleccionado, value))
            {
                LimpiarMensajes();
                NotificarCambio(nameof(TienePeriodoSeleccionado));
                NotificarCambio(nameof(EstadoDocumento));
                _generarPdfCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public bool TienePeriodoSeleccionado => PeriodoSeleccionado is not null;

    public ReporteOpcion? ReporteSeleccionado
    {
        get => _reporteSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref _reporteSeleccionado, value))
            {
                LimpiarMensajes();
                _generarPdfCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public string EstadoDocumento => PeriodoSeleccionado is null
        ? string.Empty
        : ReportesPdfGenerador.ObtenerEstadoReporte(PeriodoSeleccionado);

    public bool EstaCargando
    {
        get => _estaCargando;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargando, value))
                _generarPdfCommand.NotificarPuedeEjecutar();
        }
    }

    public bool EstaGenerando
    {
        get => _estaGenerando;
        private set
        {
            if (EstablecerPropiedad(ref _estaGenerando, value))
            {
                NotificarCambio(nameof(TextoGenerar));
                _generarPdfCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public string TextoGenerar => EstaGenerando ? "Generando..." : "Generar PDF";

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeError, value))
                NotificarCambio(nameof(TieneError));
        }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public string MensajeExito
    {
        get => _mensajeExito;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeExito, value))
                NotificarCambio(nameof(TieneMensajeExito));
        }
    }

    public bool TieneMensajeExito => !string.IsNullOrWhiteSpace(MensajeExito);

    public ComandoAsync GenerarPdfCommand => _generarPdfCommand;

    public async Task CargarAsync()
    {
        Periodos.Clear();
        PeriodoSeleccionado = null;
        EmpresaActual = _obtenerEmpresaActual();
        NotificarCambio(nameof(EmpresaActual));
        NotificarCambio(nameof(TieneEmpresa));
        LimpiarMensajes();

        if (EmpresaActual is null)
        {
            NotificarCambio(nameof(TienePeriodos));
            return;
        }

        EstaCargando = true;
        try
        {
            IEnumerable<PeriodoContableListado> periodos =
                await _listarPeriodosAsync(EmpresaActual.IdEmpresa);
            foreach (PeriodoContableListado periodo in periodos)
                Periodos.Add(periodo);

            PeriodoSeleccionado = Periodos.FirstOrDefault();
            NotificarCambio(nameof(TienePeriodos));
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError = "No se pudieron cargar los períodos contables.";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    public static string SanitizarNombreArchivo(string nombre)
    {
        HashSet<char> invalidos = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(nombre.Select(caracter => invalidos.Contains(caracter) ? '_' : caracter).ToArray());
    }

    internal static string CrearNombreSugerido(
        ReporteOpcion reporte,
        PeriodoContableListado periodo)
    {
        string tipo = reporte.Tipo == TipoReporte.TodosLosReportes
            ? "Reporte_Completo"
            : reporte.Nombre;
        return SanitizarNombreArchivo($"ContaNexo_{tipo}_{periodo.NombrePeriodo}.pdf");
    }

    internal static bool IncluyeReporte(TipoReporte seleccion, TipoReporte reporte) =>
        seleccion == reporte || seleccion == TipoReporte.TodosLosReportes;

    private bool PuedeGenerarPdf() =>
        TieneEmpresa
        && PeriodoSeleccionado is not null
        && ReporteSeleccionado is not null
        && !EstaCargando
        && !EstaGenerando;

    private async Task GenerarPdfAsync()
    {
        if (EmpresaActual is null || PeriodoSeleccionado is null || ReporteSeleccionado is null)
            return;

        string nombreSugerido = CrearNombreSugerido(
            ReporteSeleccionado,
            PeriodoSeleccionado);
        var dialogo = new SaveFileDialog
        {
            Title = "Guardar reporte PDF",
            Filter = "Documento PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            FileName = nombreSugerido
        };

        if (dialogo.ShowDialog() != true)
            return;

        LimpiarMensajes();
        EstaGenerando = true;
        try
        {
            byte[] pdf = await CrearPdfAsync(
                ReporteSeleccionado.Tipo,
                EmpresaActual,
                PeriodoSeleccionado);
            await File.WriteAllBytesAsync(dialogo.FileName, pdf);
            MensajeExito = $"PDF generado correctamente: {Path.GetFileName(dialogo.FileName)}";
            MessageBox.Show(
                MensajeExito,
                "Reportes",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError = "No se pudo generar el PDF. Verifica la ruta e inténtalo nuevamente.";
        }
        finally
        {
            EstaGenerando = false;
        }
    }

    private async Task<byte[]> CrearPdfAsync(
        TipoReporte tipo,
        Empresa empresa,
        PeriodoContableListado periodo)
    {
        if (_repositorioLibroMayor is null || _repositorioAsiento is null)
            throw new InvalidOperationException("La generación de reportes no está disponible.");

        IReadOnlyList<AsientoDetalleConsulta> detalles = [];
        if (IncluyeReporte(tipo, TipoReporte.LibroDiario))
        {
            detalles = await ObtenerDetallesDiarioAsync(periodo.IdPeriodoContable);
        }

        if (tipo == TipoReporte.LibroDiario)
        {
            return await Task.Run(() =>
                _generador.GenerarLibroDiario(empresa, periodo, detalles));
        }

        IReadOnlyList<LibroMayorCuenta> cuentas =
            await _repositorioLibroMayor.ObtenerPorPeriodoAsync(periodo.IdPeriodoContable);

        return await Task.Run(() => tipo switch
        {
            TipoReporte.BalanceSumasSaldos =>
                _generador.GenerarBalanceSumasSaldos(empresa, periodo, cuentas),
            TipoReporte.EstadoResultados =>
                _generador.GenerarEstadoResultados(
                    empresa,
                    periodo,
                    EstadoResultadosCalculador.Calcular(cuentas)),
            TipoReporte.BalanceGeneral =>
                _generador.GenerarBalanceGeneral(
                    empresa,
                    periodo,
                    BalanceGeneralCalculador.Calcular(cuentas)),
            TipoReporte.LibroMayor =>
                _generador.GenerarLibroMayor(empresa, periodo, cuentas),
            TipoReporte.TodosLosReportes =>
                _generador.GenerarReporteCompleto(
                    empresa,
                    periodo,
                    detalles,
                    cuentas,
                    EstadoResultadosCalculador.Calcular(cuentas),
                    BalanceGeneralCalculador.Calcular(cuentas)),
            _ => throw new InvalidOperationException("El tipo de reporte no es válido.")
        });
    }

    private async Task<IReadOnlyList<AsientoDetalleConsulta>> ObtenerDetallesDiarioAsync(
        int idPeriodoContable)
    {
        IReadOnlyList<AsientoListado> listado =
            await _repositorioAsiento!.ListarPorPeriodoAsync(idPeriodoContable);
        using var limiteConsultas = new SemaphoreSlim(4);
        Task<AsientoDetalleConsulta>[] consultas = listado.Select(async asiento =>
        {
            await limiteConsultas.WaitAsync();
            try
            {
                return await _repositorioAsiento.ObtenerDetalleAsync(
                    idPeriodoContable,
                    asiento.IdAsiento);
            }
            finally
            {
                limiteConsultas.Release();
            }
        }).ToArray();
        return await Task.WhenAll(consultas);
    }

    private void LimpiarMensajes()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }
}
