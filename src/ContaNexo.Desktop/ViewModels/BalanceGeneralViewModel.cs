using System.Collections.ObjectModel;
using ContaNexo.Core.Calculations;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class BalanceGeneralViewModel : ViewModelBase
{
    private static readonly BalanceGeneralGrupo GrupoVacio = new();

    private readonly RepositorioPeriodoContable _repositorioPeriodoContable;
    private readonly RepositorioLibroMayor _repositorioLibroMayor;
    private readonly Func<Empresa?> _obtenerEmpresaActiva;
    private readonly Func<PeriodoContableListado?> _obtenerPeriodoActivo;
    private readonly ComandoAsync _consultarPeriodoCommand;
    private Empresa? _empresaActual;
    private BalanceGeneralPeriodoItemViewModel? _periodoSeleccionado;
    private PeriodoContableListado? _periodoConsultado;
    private BalanceGeneralResumen? _resumen;
    private bool _estaCargando;
    private string _mensajeError = string.Empty;

    public BalanceGeneralViewModel(
        RepositorioPeriodoContable repositorioPeriodoContable,
        RepositorioLibroMayor repositorioLibroMayor,
        Func<Empresa?> obtenerEmpresaActiva,
        Func<PeriodoContableListado?> obtenerPeriodoActivo)
    {
        _repositorioPeriodoContable = repositorioPeriodoContable;
        _repositorioLibroMayor = repositorioLibroMayor;
        _obtenerEmpresaActiva = obtenerEmpresaActiva;
        _obtenerPeriodoActivo = obtenerPeriodoActivo;
        _consultarPeriodoCommand = new ComandoAsync(
            ConsultarPeriodoSeleccionadoAsync,
            PuedeConsultarPeriodo);
    }

    public ObservableCollection<BalanceGeneralPeriodoItemViewModel> Periodos { get; } = new();

    public Empresa? EmpresaActual
    {
        get => _empresaActual;
        private set
        {
            if (EstablecerPropiedad(ref _empresaActual, value))
            {
                NotificarCambio(nameof(TieneEmpresa));
            }
        }
    }

    public bool TieneEmpresa => EmpresaActual is not null;

    public bool TienePeriodos => Periodos.Count > 0;

    public BalanceGeneralPeriodoItemViewModel? PeriodoSeleccionado
    {
        get => _periodoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref _periodoSeleccionado, value))
            {
                LimpiarConsulta();
                NotificarCambio(nameof(TienePeriodoSeleccionado));
                _consultarPeriodoCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public bool TienePeriodoSeleccionado => PeriodoSeleccionado is not null;

    public PeriodoContableListado? PeriodoConsultado
    {
        get => _periodoConsultado;
        private set
        {
            if (EstablecerPropiedad(ref _periodoConsultado, value))
            {
                NotificarCambio(nameof(TienePeriodoConsultado));
                NotificarCambio(nameof(NombrePeriodoConsultado));
                NotificarCambio(nameof(FechaInicioPeriodoConsultado));
                NotificarCambio(nameof(FechaFinPeriodoConsultado));
                NotificarCambio(nameof(EstadoPeriodoConsultado));
            }
        }
    }

    public bool TienePeriodoConsultado => PeriodoConsultado is not null;

    public string NombrePeriodoConsultado =>
        PeriodoConsultado?.NombrePeriodo ?? "Sin seleccionar";

    public DateTime? FechaInicioPeriodoConsultado =>
        PeriodoConsultado?.FechaInicioPeriodo;

    public DateTime? FechaFinPeriodoConsultado =>
        PeriodoConsultado?.FechaFinPeriodo;

    public string EstadoPeriodoConsultado =>
        PeriodoConsultado?.EstadoPeriodo ?? string.Empty;

    public BalanceGeneralResumen? Resumen
    {
        get => _resumen;
        private set
        {
            if (EstablecerPropiedad(ref _resumen, value))
            {
                NotificarResumen();
            }
        }
    }

    public bool TieneResumen => Resumen is not null;

    public BalanceGeneralGrupo ActivoCorriente =>
        Resumen?.ActivoCorriente ?? GrupoVacio;

    public BalanceGeneralGrupo ActivoNoCorriente =>
        Resumen?.ActivoNoCorriente ?? GrupoVacio;

    public BalanceGeneralGrupo PasivoCorriente =>
        Resumen?.PasivoCorriente ?? GrupoVacio;

    public BalanceGeneralGrupo PasivoNoCorriente =>
        Resumen?.PasivoNoCorriente ?? GrupoVacio;

    public BalanceGeneralGrupo CapitalContable =>
        Resumen?.CapitalContable ?? GrupoVacio;

    public BalanceGeneralGrupo ResultadosReservas =>
        Resumen?.ResultadosReservas ?? GrupoVacio;

    public decimal TasaParticipacionTrabajadores =>
        Resumen?.TasaParticipacionTrabajadores ?? 0m;

    public decimal ParticipacionTrabajadores =>
        Resumen?.ParticipacionTrabajadores ?? 0m;

    public decimal TasaImpuestoRenta => Resumen?.TasaImpuestoRenta ?? 0m;

    public decimal ImpuestoRenta => Resumen?.ImpuestoRenta ?? 0m;

    public decimal ResultadoNeto => Resumen?.ResultadoNeto ?? 0m;

    public decimal TotalPasivoCorriente =>
        Resumen?.TotalPasivoCorriente ?? 0m;

    public decimal TotalActivo => Resumen?.TotalActivo ?? 0m;

    public decimal TotalPasivo => Resumen?.TotalPasivo ?? 0m;

    public decimal TotalPatrimonio => Resumen?.TotalPatrimonio ?? 0m;

    public decimal TotalPasivoPatrimonio =>
        Resumen?.TotalPasivoPatrimonio ?? 0m;

    public decimal Diferencia => Resumen?.Diferencia ?? 0m;

    public bool EstaCuadrado => Resumen?.EstaCuadrado ?? false;

    public int CantidadSaldosContrarios =>
        Resumen?.CantidadSaldosContrarios ?? 0;

    public bool TieneSaldosContrarios =>
        Resumen?.TieneSaldosContrarios ?? false;

    public int CantidadCuentasResultadoEjercicioRegistradas =>
        Resumen?.CantidadCuentasResultadoEjercicioRegistradas ?? 0;

    public bool TieneCuentasResultadoEjercicioRegistradas =>
        Resumen?.TieneCuentasResultadoEjercicioRegistradas ?? false;

    public bool EstaCargando
    {
        get => _estaCargando;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargando, value))
            {
                _consultarPeriodoCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeError, value))
            {
                NotificarCambio(nameof(TieneError));
            }
        }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public ComandoAsync ConsultarPeriodoCommand => _consultarPeriodoCommand;

    public async Task CargarAsync()
    {
        LimpiarPeriodosYConsulta();
        EmpresaActual = _obtenerEmpresaActiva();

        if (EmpresaActual is null)
        {
            return;
        }

        int? idPeriodoInicial = null;
        EstaCargando = true;

        try
        {
            IEnumerable<PeriodoContableListado> periodos =
                await _repositorioPeriodoContable.ListarAsync(EmpresaActual.IdEmpresa);

            foreach (PeriodoContableListado periodo in periodos)
            {
                Periodos.Add(new BalanceGeneralPeriodoItemViewModel(periodo));
            }

            NotificarCambio(nameof(TienePeriodos));

            PeriodoContableListado? periodoActivo = _obtenerPeriodoActivo();
            BalanceGeneralPeriodoItemViewModel? periodoInicial = periodoActivo is null
                || periodoActivo.IdEmpresa != EmpresaActual.IdEmpresa
                ? null
                : Periodos.FirstOrDefault(periodo =>
                    periodo.IdPeriodoContable == periodoActivo.IdPeriodoContable);

            if (periodoInicial is not null)
            {
                PeriodoSeleccionado = periodoInicial;
                idPeriodoInicial = periodoInicial.IdPeriodoContable;
            }
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError =
                "No se pudieron cargar los períodos para el Balance General. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaCargando = false;
        }

        if (idPeriodoInicial is int idPeriodoContable && !TieneError)
        {
            await CargarPeriodoAsync(idPeriodoContable);
        }
    }

    public async Task CargarPeriodoAsync(int idPeriodoContable)
    {
        BalanceGeneralPeriodoItemViewModel? periodo = Periodos.FirstOrDefault(
            item => item.IdPeriodoContable == idPeriodoContable);

        if (periodo is null)
        {
            MensajeError =
                "El período seleccionado no pertenece a la empresa actual.";
            return;
        }

        if (!ReferenceEquals(PeriodoSeleccionado, periodo))
        {
            PeriodoSeleccionado = periodo;
        }

        LimpiarConsulta();
        EstaCargando = true;

        try
        {
            IReadOnlyList<LibroMayorCuenta> cuentas =
                await _repositorioLibroMayor.ObtenerPorPeriodoAsync(
                    idPeriodoContable);

            Resumen = BalanceGeneralCalculador.Calcular(cuentas);
            PeriodoConsultado = periodo.Periodo;
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError =
                "No se pudo cargar el Balance General. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task ConsultarPeriodoSeleccionadoAsync()
    {
        if (PeriodoSeleccionado is not null)
        {
            await CargarPeriodoAsync(PeriodoSeleccionado.IdPeriodoContable);
        }
    }

    private bool PuedeConsultarPeriodo()
    {
        return TieneEmpresa
            && TienePeriodoSeleccionado
            && !EstaCargando;
    }

    private void LimpiarPeriodosYConsulta()
    {
        Periodos.Clear();
        PeriodoSeleccionado = null;
        LimpiarConsulta();
        MensajeError = string.Empty;
        NotificarCambio(nameof(TienePeriodos));
    }

    private void LimpiarConsulta()
    {
        Resumen = null;
        PeriodoConsultado = null;
        MensajeError = string.Empty;
    }

    private void NotificarResumen()
    {
        NotificarCambio(nameof(TieneResumen));
        NotificarCambio(nameof(ActivoCorriente));
        NotificarCambio(nameof(ActivoNoCorriente));
        NotificarCambio(nameof(PasivoCorriente));
        NotificarCambio(nameof(PasivoNoCorriente));
        NotificarCambio(nameof(CapitalContable));
        NotificarCambio(nameof(ResultadosReservas));
        NotificarCambio(nameof(TasaParticipacionTrabajadores));
        NotificarCambio(nameof(ParticipacionTrabajadores));
        NotificarCambio(nameof(TasaImpuestoRenta));
        NotificarCambio(nameof(ImpuestoRenta));
        NotificarCambio(nameof(ResultadoNeto));
        NotificarCambio(nameof(TotalPasivoCorriente));
        NotificarCambio(nameof(TotalActivo));
        NotificarCambio(nameof(TotalPasivo));
        NotificarCambio(nameof(TotalPatrimonio));
        NotificarCambio(nameof(TotalPasivoPatrimonio));
        NotificarCambio(nameof(Diferencia));
        NotificarCambio(nameof(EstaCuadrado));
        NotificarCambio(nameof(CantidadSaldosContrarios));
        NotificarCambio(nameof(TieneSaldosContrarios));
        NotificarCambio(nameof(CantidadCuentasResultadoEjercicioRegistradas));
        NotificarCambio(nameof(TieneCuentasResultadoEjercicioRegistradas));
    }
}

public sealed class BalanceGeneralPeriodoItemViewModel
{
    public BalanceGeneralPeriodoItemViewModel(PeriodoContableListado periodo)
    {
        Periodo = periodo;
    }

    public PeriodoContableListado Periodo { get; }

    public int IdPeriodoContable => Periodo.IdPeriodoContable;

    public string NombrePeriodo => Periodo.NombrePeriodo;

    public DateTime FechaInicioPeriodo => Periodo.FechaInicioPeriodo;

    public DateTime FechaFinPeriodo => Periodo.FechaFinPeriodo;

    public string EstadoPeriodo => Periodo.EstadoPeriodo;

    public bool EsCerrado => string.Equals(
        EstadoPeriodo,
        "Cerrado",
        StringComparison.Ordinal);
}
