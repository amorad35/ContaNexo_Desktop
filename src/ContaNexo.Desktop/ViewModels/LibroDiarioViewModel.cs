using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class LibroDiarioViewModel : ViewModelBase
{
    private const string MensajeSinPeriodoActivo =
        "Selecciona un período contable activo antes de registrar un asiento.";

    private const string MensajePeriodoNoAbierto =
        "El período contable activo debe estar abierto para registrar asientos.";

    private readonly RepositorioCuentaContable _repositorioCuentaContable;
    private readonly RepositorioAsiento _repositorioAsiento;
    private readonly Func<PeriodoContableListado?> _obtenerPeriodoActivo;
    private readonly ComandoRelay _agregarLineaCommand;
    private readonly ComandoRelay _eliminarLineaCommand;
    private readonly ComandoAsync _nuevoAsientoCommand;
    private readonly ComandoAsync _verAsientoCommand;
    private readonly ComandoRelay _volverAlLibroCommand;
    private readonly ComandoAsync _guardarCommand;

    private PeriodoContableListado? _periodoActivo;
    private DateTime _fechaAsiento = DateTime.Today;
    private string _tipoAsiento = "Normal";
    private string? _descripcionAsiento;
    private decimal _totalDebe;
    private decimal _totalHaber;
    private decimal _diferencia;
    private bool _estaCuadrado;
    private bool _estaCargandoCuentas;
    private bool _estaCargandoAsientos;
    private bool _cuentasCargadas;
    private bool _estaGuardando;
    private bool _estaCreandoAsiento;
    private bool _estaViendoAsiento;
    private bool _estaCargandoDetalleAsiento;
    private AsientoDetalleConsulta? _asientoEnConsulta;
    private string _mensajeError = string.Empty;
    private string _mensajeExito = string.Empty;
    private int? _ultimoIdAsientoRegistrado;
    private int? _ultimoNumeroAsientoRegistrado;

    public LibroDiarioViewModel(
        RepositorioCuentaContable repositorioCuentaContable,
        RepositorioAsiento repositorioAsiento,
        Func<PeriodoContableListado?> obtenerPeriodoActivo)
    {
        _repositorioCuentaContable = repositorioCuentaContable;
        _repositorioAsiento = repositorioAsiento;
        _obtenerPeriodoActivo = obtenerPeriodoActivo;
        _agregarLineaCommand = new ComandoRelay(AgregarLinea, PuedeAgregarLinea);
        _eliminarLineaCommand = new ComandoRelay(EliminarLinea, PuedeEliminarLinea);
        _nuevoAsientoCommand = new ComandoAsync(
            IniciarNuevoAsientoAsync,
            PuedeIniciarNuevoAsiento);
        _verAsientoCommand = new ComandoAsync(
            VerAsientoAsync,
            PuedeVerAsiento);
        _volverAlLibroCommand = new ComandoRelay(
            VolverAlLibro,
            PuedeVolverAlLibro);
        _guardarCommand = new ComandoAsync(GuardarAsync, PuedeGuardar);

        TiposAsiento = ["Normal", "Ajuste"];
        ReiniciarLineas();
        ActualizarPeriodoActivo();
    }

    public ObservableCollection<CuentaMovimiento> CuentasMovimiento { get; } = new();

    public ObservableCollection<LineaAsientoViewModel> Lineas { get; } = new();

    public ObservableCollection<AsientoListado> AsientosRegistrados { get; } = new();

    public IReadOnlyList<string> TiposAsiento { get; }

    public PeriodoContableListado? PeriodoActivo => _periodoActivo;

    public int? IdPeriodoContableActivo => PeriodoActivo?.IdPeriodoContable;

    public string NombrePeriodoActivo => PeriodoActivo?.NombrePeriodo ?? "Sin seleccionar";

    public DateTime? FechaInicioPeriodoActivo => PeriodoActivo?.FechaInicioPeriodo;

    public DateTime? FechaFinPeriodoActivo => PeriodoActivo?.FechaFinPeriodo;

    public string EstadoPeriodoActivo => PeriodoActivo?.EstadoPeriodo ?? string.Empty;

    public bool TienePeriodoActivo => PeriodoActivo is not null;

    public bool PeriodoActivoAbierto => PeriodoActivo is not null
        && string.Equals(
            PeriodoActivo.EstadoPeriodo,
            "Abierto",
            StringComparison.OrdinalIgnoreCase);

    public DateTime FechaAsiento
    {
        get => _fechaAsiento;
        set
        {
            if (EstablecerPropiedad(ref _fechaAsiento, value.Date))
            {
                LimpiarMensajesPorEdicion();
            }
        }
    }

    public string TipoAsiento
    {
        get => _tipoAsiento;
        set
        {
            if (EstablecerPropiedad(ref _tipoAsiento, value))
            {
                LimpiarMensajesPorEdicion();
            }
        }
    }

    public string? DescripcionAsiento
    {
        get => _descripcionAsiento;
        set
        {
            if (EstablecerPropiedad(ref _descripcionAsiento, value))
            {
                LimpiarMensajesPorEdicion();
            }
        }
    }

    public decimal TotalDebe
    {
        get => _totalDebe;
        private set => EstablecerPropiedad(ref _totalDebe, value);
    }

    public decimal TotalHaber
    {
        get => _totalHaber;
        private set => EstablecerPropiedad(ref _totalHaber, value);
    }

    public decimal Diferencia
    {
        get => _diferencia;
        private set => EstablecerPropiedad(ref _diferencia, value);
    }

    public bool EstaCuadrado
    {
        get => _estaCuadrado;
        private set => EstablecerPropiedad(ref _estaCuadrado, value);
    }

    public bool EstaCargandoCuentas
    {
        get => _estaCargandoCuentas;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargandoCuentas, value))
            {
                NotificarCambio(nameof(FormularioHabilitado));
                NotificarEstadoComandos();
            }
        }
    }

    public bool EstaGuardando
    {
        get => _estaGuardando;
        private set
        {
            if (EstablecerPropiedad(ref _estaGuardando, value))
            {
                NotificarCambio(nameof(FormularioHabilitado));
                NotificarCambio(nameof(TextoGuardar));
                NotificarEstadoComandos();
            }
        }
    }

    public bool EstaCargandoAsientos
    {
        get => _estaCargandoAsientos;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargandoAsientos, value))
            {
                NotificarEstadoComandos();
            }
        }
    }

    public bool EstaCreandoAsiento
    {
        get => _estaCreandoAsiento;
        private set
        {
            if (EstablecerPropiedad(ref _estaCreandoAsiento, value))
            {
                NotificarEstadoComandos();
            }
        }
    }

    public bool EstaViendoAsiento
    {
        get => _estaViendoAsiento;
        private set
        {
            if (EstablecerPropiedad(ref _estaViendoAsiento, value))
            {
                NotificarEstadoComandos();
            }
        }
    }

    public bool EstaCargandoDetalleAsiento
    {
        get => _estaCargandoDetalleAsiento;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargandoDetalleAsiento, value))
            {
                NotificarEstadoComandos();
            }
        }
    }

    public AsientoDetalleConsulta? AsientoEnConsulta
    {
        get => _asientoEnConsulta;
        private set
        {
            if (EstablecerPropiedad(ref _asientoEnConsulta, value))
            {
                NotificarCambio(nameof(TituloAsientoEnConsulta));
            }
        }
    }

    public string TituloAsientoEnConsulta => AsientoEnConsulta is null
        ? "Asiento"
        : $"Asiento N.º {AsientoEnConsulta.NumeroAsiento}";

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

    public string MensajeExito
    {
        get => _mensajeExito;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeExito, value))
            {
                NotificarCambio(nameof(TieneMensajeExito));
            }
        }
    }

    public int? UltimoIdAsientoRegistrado
    {
        get => _ultimoIdAsientoRegistrado;
        private set => EstablecerPropiedad(ref _ultimoIdAsientoRegistrado, value);
    }

    public int? UltimoNumeroAsientoRegistrado
    {
        get => _ultimoNumeroAsientoRegistrado;
        private set => EstablecerPropiedad(ref _ultimoNumeroAsientoRegistrado, value);
    }

    public bool TieneCuentasMovimiento => CuentasMovimiento.Count > 0;

    public bool TieneAsientosRegistrados => AsientosRegistrados.Count > 0;

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public bool TieneMensajeExito => !string.IsNullOrWhiteSpace(MensajeExito);

    public bool FormularioHabilitado => !EstaCargandoCuentas && !EstaGuardando;

    public string TextoGuardar => EstaGuardando ? "Guardando..." : "Guardar asiento";

    public ComandoRelay AgregarLineaCommand => _agregarLineaCommand;

    public ComandoRelay EliminarLineaCommand => _eliminarLineaCommand;

    public ComandoAsync NuevoAsientoCommand => _nuevoAsientoCommand;

    public ComandoAsync VerAsientoCommand => _verAsientoCommand;

    public ComandoRelay VolverAlLibroCommand => _volverAlLibroCommand;

    public ComandoAsync GuardarCommand => _guardarCommand;

    public async Task CargarAsync()
    {
        EstaCreandoAsiento = false;
        EstaViendoAsiento = false;
        AsientoEnConsulta = null;
        ActualizarPeriodoActivo();

        await RefrescarAsientosRegistradosAsync();
    }

    private async Task CargarCuentasAsync()
    {

        if (EstaCargandoCuentas || _cuentasCargadas)
        {
            return;
        }

        if (PeriodoActivoAbierto)
        {
            MensajeError = string.Empty;
        }

        EstaCargandoCuentas = true;

        try
        {
            IEnumerable<CuentaMovimiento> cuentas =
                await _repositorioCuentaContable.ListarMovimientoAsync();

            CuentasMovimiento.Clear();

            foreach (CuentaMovimiento cuenta in cuentas)
            {
                CuentasMovimiento.Add(cuenta);
            }

            _cuentasCargadas = true;
            NotificarCambio(nameof(TieneCuentasMovimiento));
            RestablecerMensajeContextoPeriodo();
        }
        catch (InvalidOperationException excepcion)
        {
            CuentasMovimiento.Clear();
            MensajeExito = string.Empty;
            MensajeError = excepcion.Message;
            NotificarCambio(nameof(TieneCuentasMovimiento));
        }
        catch (Exception)
        {
            CuentasMovimiento.Clear();
            MensajeExito = string.Empty;
            MensajeError =
                "No se pudieron cargar las cuentas habilitadas para movimiento. Verifica la conexión e inténtalo nuevamente.";
            NotificarCambio(nameof(TieneCuentasMovimiento));
        }
        finally
        {
            EstaCargandoCuentas = false;
        }
    }

    public void ActualizarPeriodoActivo()
    {
        PeriodoContableListado? periodo = _obtenerPeriodoActivo();
        bool cambioPeriodo = EsContextoPeriodoDiferente(_periodoActivo, periodo);
        _periodoActivo = periodo;

        NotificarCambio(nameof(PeriodoActivo));
        NotificarCambio(nameof(IdPeriodoContableActivo));
        NotificarCambio(nameof(NombrePeriodoActivo));
        NotificarCambio(nameof(FechaInicioPeriodoActivo));
        NotificarCambio(nameof(FechaFinPeriodoActivo));
        NotificarCambio(nameof(EstadoPeriodoActivo));
        NotificarCambio(nameof(TienePeriodoActivo));
        NotificarCambio(nameof(PeriodoActivoAbierto));

        if (cambioPeriodo)
        {
            EstaCreandoAsiento = false;
            EstaViendoAsiento = false;
            AsientoEnConsulta = null;
            MensajeError = string.Empty;
            MensajeExito = string.Empty;
            AsientosRegistrados.Clear();
            NotificarCambio(nameof(TieneAsientosRegistrados));

            if (periodo is not null)
            {
                DateTime fechaHoy = DateTime.Today;
                DateTime fechaInicial = fechaHoy >= periodo.FechaInicioPeriodo.Date
                    && fechaHoy <= periodo.FechaFinPeriodo.Date
                        ? fechaHoy
                        : periodo.FechaInicioPeriodo.Date;

                EstablecerPropiedad(
                    ref _fechaAsiento,
                    fechaInicial,
                    nameof(FechaAsiento));
            }
        }

        RestablecerMensajeContextoPeriodo();
        NotificarEstadoComandos();
    }

    private bool PuedeAgregarLinea()
    {
        return !EstaGuardando;
    }

    private void AgregarLinea()
    {
        AgregarLineaInterna();
        LimpiarMensajesPorEdicion();
    }

    private bool PuedeEliminarLinea(object? parametro)
    {
        return !EstaGuardando
            && Lineas.Count > 2
            && parametro is LineaAsientoViewModel linea
            && Lineas.Contains(linea);
    }

    private void EliminarLinea(object? parametro)
    {
        if (parametro is not LineaAsientoViewModel linea
            || !PuedeEliminarLinea(linea))
        {
            return;
        }

        linea.PropertyChanged -= AlCambiarLinea;
        Lineas.Remove(linea);
        RecalcularTotales();
        LimpiarMensajesPorEdicion();
        NotificarEstadoComandos();
    }

    private bool PuedeIniciarNuevoAsiento()
    {
        return !EstaCreandoAsiento
            && !EstaCargandoAsientos
            && !EstaGuardando
            && PeriodoActivoAbierto;
    }

    private async Task IniciarNuevoAsientoAsync()
    {
        ActualizarPeriodoActivo();

        if (!PeriodoActivoAbierto)
        {
            return;
        }

        ReiniciarCapturaDespuesDeGuardar();
        RestablecerFechaParaPeriodoActivo();
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        EstaCreandoAsiento = true;
        await CargarCuentasAsync();
    }

    private bool PuedeVerAsiento(object? parametro)
    {
        return parametro is AsientoListado
            && PeriodoActivo is not null
            && !EstaCreandoAsiento
            && !EstaViendoAsiento
            && !EstaCargandoAsientos
            && !EstaCargandoDetalleAsiento;
    }

    private async Task VerAsientoAsync(object? parametro)
    {
        if (parametro is not AsientoListado asientoListado)
        {
            return;
        }

        ActualizarPeriodoActivo();
        PeriodoContableListado? periodo = PeriodoActivo;

        if (periodo is null)
        {
            MensajeExito = string.Empty;
            MensajeError = "Selecciona un período contable para consultar el asiento.";
            return;
        }

        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        EstaCargandoDetalleAsiento = true;

        try
        {
            AsientoDetalleConsulta detalle =
                await _repositorioAsiento.ObtenerDetalleAsync(
                    periodo.IdPeriodoContable,
                    asientoListado.IdAsiento);

            AsientoEnConsulta = detalle;
            EstaViendoAsiento = true;
        }
        catch (InvalidOperationException excepcion)
        {
            AsientoEnConsulta = null;
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            AsientoEnConsulta = null;
            MensajeError =
                "No se pudo cargar el detalle del asiento. Inténtalo nuevamente.";
        }
        finally
        {
            EstaCargandoDetalleAsiento = false;
        }
    }

    private bool PuedeVolverAlLibro()
    {
        return (EstaCreandoAsiento && !EstaGuardando)
            || EstaViendoAsiento;
    }

    private void VolverAlLibro()
    {
        if (EstaViendoAsiento)
        {
            AsientoEnConsulta = null;
            EstaViendoAsiento = false;
            MensajeError = string.Empty;
            MensajeExito = string.Empty;
            RestablecerMensajeContextoPeriodo();
            return;
        }

        ReiniciarCapturaDespuesDeGuardar();
        RestablecerFechaParaPeriodoActivo();
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        EstaCreandoAsiento = false;
        RestablecerMensajeContextoPeriodo();
    }

    private bool PuedeGuardar()
    {
        return !EstaGuardando
            && !EstaCargandoCuentas
            && PeriodoActivoAbierto;
    }

    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        ActualizarPeriodoActivo();

        if (!ValidarCaptura(out PeriodoContableListado? periodo))
        {
            return;
        }

        EstaGuardando = true;

        try
        {
            var asiento = new AsientoCreacion
            {
                IdPeriodoContable = periodo.IdPeriodoContable,
                FechaAsiento = FechaAsiento.Date,
                TipoAsiento = TipoAsiento,
                DescripcionAsiento = NormalizarDescripcion(DescripcionAsiento),
                Detalles = Lineas
                    .Select((linea, indice) => new DetalleAsientoCreacion
                    {
                        IdCuentaContable = linea.CuentaSeleccionada!.IdCuentaContable,
                        DebeDetalle = linea.Debe,
                        HaberDetalle = linea.Haber,
                        OrdenDetalle = checked((short)(indice + 1))
                    })
                    .ToList()
            };

            AsientoCreacionResultado resultado =
                await _repositorioAsiento.CrearAsync(asiento);

            UltimoIdAsientoRegistrado = resultado.IdAsiento;
            UltimoNumeroAsientoRegistrado = resultado.NumeroAsiento;
            ReiniciarCapturaDespuesDeGuardar();
            await RefrescarAsientosRegistradosAsync();
            EstaCreandoAsiento = false;
            MensajeExito =
                $"Asiento N.º {resultado.NumeroAsiento} registrado correctamente.";
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError =
                "No se pudo registrar el asiento. Verifica los datos e inténtalo nuevamente.";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private bool ValidarCaptura(
        [NotNullWhen(true)] out PeriodoContableListado? periodo)
    {
        periodo = PeriodoActivo;

        if (periodo is null)
        {
            MensajeError = MensajeSinPeriodoActivo;
            return false;
        }

        if (!string.Equals(
                periodo.EstadoPeriodo,
                "Abierto",
                StringComparison.OrdinalIgnoreCase))
        {
            MensajeError = MensajePeriodoNoAbierto;
            return false;
        }

        if (FechaAsiento.Date < periodo.FechaInicioPeriodo.Date
            || FechaAsiento.Date > periodo.FechaFinPeriodo.Date)
        {
            MensajeError =
                "La fecha del asiento debe estar dentro del período contable activo.";
            return false;
        }

        if (!TiposAsiento.Contains(TipoAsiento))
        {
            MensajeError = "El tipo de asiento debe ser Normal o Ajuste.";
            return false;
        }

        if (Lineas.Count < 2)
        {
            MensajeError = "El asiento debe contener al menos dos líneas.";
            return false;
        }

        if (Lineas.Count > short.MaxValue)
        {
            MensajeError =
                $"El asiento no puede contener más de {short.MaxValue} líneas.";
            return false;
        }

        if (Lineas.Any(linea => linea.CuentaSeleccionada is null))
        {
            MensajeError = "Selecciona una cuenta contable en cada línea.";
            return false;
        }

        if (Lineas.Any(linea => linea.Debe < 0 || linea.Haber < 0))
        {
            MensajeError = "Los valores de Debe y Haber no pueden ser negativos.";
            return false;
        }

        if (Lineas.Any(linea =>
                !((linea.Debe > 0 && linea.Haber == 0)
                    || (linea.Debe == 0 && linea.Haber > 0))))
        {
            MensajeError =
                "Cada línea debe tener un valor positivo únicamente en Debe o únicamente en Haber.";
            return false;
        }

        if (TotalDebe != TotalHaber)
        {
            MensajeError = "El total Debe debe ser igual al total Haber.";
            return false;
        }

        if (TotalDebe <= 0)
        {
            MensajeError = "El total del asiento debe ser mayor que cero.";
            return false;
        }

        return true;
    }

    private async Task RefrescarAsientosRegistradosAsync()
    {
        if (PeriodoActivo is null)
        {
            AsientosRegistrados.Clear();
            NotificarCambio(nameof(TieneAsientosRegistrados));
            return;
        }

        EstaCargandoAsientos = true;

        try
        {
            IReadOnlyList<AsientoListado> asientos =
                await _repositorioAsiento.ListarPorPeriodoAsync(
                    PeriodoActivo.IdPeriodoContable);

            AsientosRegistrados.Clear();

            foreach (AsientoListado asiento in asientos)
            {
                AsientosRegistrados.Add(asiento);
            }

            NotificarCambio(nameof(TieneAsientosRegistrados));
        }
        catch (InvalidOperationException excepcion)
        {
            AsientosRegistrados.Clear();
            MensajeError = excepcion.Message;
            NotificarCambio(nameof(TieneAsientosRegistrados));
        }
        catch (Exception)
        {
            AsientosRegistrados.Clear();
            MensajeError =
                "No se pudieron cargar los asientos registrados del período.";
            NotificarCambio(nameof(TieneAsientosRegistrados));
        }
        finally
        {
            EstaCargandoAsientos = false;
        }
    }

    private void AgregarLineaInterna()
    {
        var linea = new LineaAsientoViewModel();
        linea.PropertyChanged += AlCambiarLinea;
        Lineas.Add(linea);
        NotificarEstadoComandos();
    }

    private void AlCambiarLinea(object? sender, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName is nameof(LineaAsientoViewModel.Debe)
            or nameof(LineaAsientoViewModel.Haber))
        {
            RecalcularTotales();
        }

        LimpiarMensajesPorEdicion();
    }

    private void RecalcularTotales()
    {
        decimal totalDebe = Lineas.Sum(linea => linea.Debe);
        decimal totalHaber = Lineas.Sum(linea => linea.Haber);

        TotalDebe = totalDebe;
        TotalHaber = totalHaber;
        Diferencia = totalDebe - totalHaber;
        EstaCuadrado = totalDebe == totalHaber && totalDebe > 0;
    }

    private void ReiniciarLineas()
    {
        foreach (LineaAsientoViewModel linea in Lineas)
        {
            linea.PropertyChanged -= AlCambiarLinea;
        }

        Lineas.Clear();
        AgregarLineaInterna();
        AgregarLineaInterna();
        RecalcularTotales();
        NotificarEstadoComandos();
    }

    private void ReiniciarCapturaDespuesDeGuardar()
    {
        _tipoAsiento = "Normal";
        _descripcionAsiento = null;
        NotificarCambio(nameof(TipoAsiento));
        NotificarCambio(nameof(DescripcionAsiento));
        ReiniciarLineas();
    }

    private void RestablecerFechaParaPeriodoActivo()
    {
        if (PeriodoActivo is null)
        {
            return;
        }

        DateTime fechaHoy = DateTime.Today;
        FechaAsiento = fechaHoy >= PeriodoActivo.FechaInicioPeriodo.Date
            && fechaHoy <= PeriodoActivo.FechaFinPeriodo.Date
                ? fechaHoy
                : PeriodoActivo.FechaInicioPeriodo.Date;
    }

    private void LimpiarMensajesPorEdicion()
    {
        MensajeExito = string.Empty;

        if (PeriodoActivoAbierto)
        {
            MensajeError = string.Empty;
        }
    }

    private void RestablecerMensajeContextoPeriodo()
    {
        if (PeriodoActivo is null)
        {
            if (EstaCreandoAsiento)
            {
                MensajeError = MensajeSinPeriodoActivo;
            }
            else if (MensajeError is MensajeSinPeriodoActivo or MensajePeriodoNoAbierto)
            {
                MensajeError = string.Empty;
            }

            return;
        }

        if (!PeriodoActivoAbierto)
        {
            if (EstaCreandoAsiento)
            {
                MensajeError = MensajePeriodoNoAbierto;
            }
            else if (MensajeError is MensajeSinPeriodoActivo or MensajePeriodoNoAbierto)
            {
                MensajeError = string.Empty;
            }

            return;
        }

        if (MensajeError is MensajeSinPeriodoActivo or MensajePeriodoNoAbierto)
        {
            MensajeError = string.Empty;
        }
    }

    private void NotificarEstadoComandos()
    {
        _agregarLineaCommand.NotificarPuedeEjecutar();
        _eliminarLineaCommand.NotificarPuedeEjecutar();
        _nuevoAsientoCommand.NotificarPuedeEjecutar();
        _verAsientoCommand.NotificarPuedeEjecutar();
        _volverAlLibroCommand.NotificarPuedeEjecutar();
        _guardarCommand.NotificarPuedeEjecutar();
    }

    private static bool EsContextoPeriodoDiferente(
        PeriodoContableListado? anterior,
        PeriodoContableListado? actual)
    {
        if (anterior is null || actual is null)
        {
            return anterior is not null || actual is not null;
        }

        return anterior.IdPeriodoContable != actual.IdPeriodoContable
            || anterior.FechaInicioPeriodo.Date != actual.FechaInicioPeriodo.Date
            || anterior.FechaFinPeriodo.Date != actual.FechaFinPeriodo.Date
            || !string.Equals(
                anterior.EstadoPeriodo,
                actual.EstadoPeriodo,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizarDescripcion(string? descripcion)
    {
        return string.IsNullOrWhiteSpace(descripcion)
            ? null
            : descripcion.Trim();
    }
}
