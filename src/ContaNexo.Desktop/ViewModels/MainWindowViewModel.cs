using System.Globalization;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string MensajeSalidaNuevoAsiento =
        "Hay cambios sin guardar en este asiento.\n¿Deseas salir sin guardar?";
    private const string MensajeCierreNuevoAsiento =
        "Hay un asiento sin guardar.\n¿Deseas cerrar ContaNexo y perder estos cambios?";

    private readonly InicioViewModel _inicioViewModel;
    private readonly EmpresaViewModel _empresaViewModel;
    private readonly PeriodoContableViewModel _periodoContableViewModel;
    private readonly CatalogoCuentasViewModel _catalogoCuentasViewModel;
    private readonly LibroDiarioViewModel _libroDiarioViewModel;
    private readonly LibroMayorViewModel _libroMayorViewModel;
    private readonly BalanceSumasSaldosViewModel _balanceSumasSaldosViewModel;
    private readonly EstadoResultadosViewModel _estadoResultadosViewModel;
    private readonly BalanceGeneralViewModel _balanceGeneralViewModel;
    private readonly ReportesViewModel _reportesViewModel;
    private readonly Func<string, bool> _confirmarSalidaSinGuardar;
    private ViewModelBase _vistaActual;
    private Empresa? _empresaActiva;
    private PeriodoContableListado? _periodoActivo;
    private Task? _inicializacionTask;

    public MainWindowViewModel(
        RepositorioEmpresa repositorioEmpresa,
        RepositorioPeriodoContable repositorioPeriodoContable,
        RepositorioCuentaContable repositorioCuentaContable,
        RepositorioAsiento repositorioAsiento,
        RepositorioLibroMayor repositorioLibroMayor,
        Func<string, bool>? confirmarSalidaSinGuardar = null)
    {
        _confirmarSalidaSinGuardar = confirmarSalidaSinGuardar ?? (_ => true);
        _catalogoCuentasViewModel = new CatalogoCuentasViewModel(repositorioCuentaContable);
        _libroDiarioViewModel = new LibroDiarioViewModel(
            repositorioCuentaContable,
            repositorioAsiento,
            () => PeriodoActivo,
            () => _confirmarSalidaSinGuardar(MensajeSalidaNuevoAsiento));
        _libroMayorViewModel = new LibroMayorViewModel(
            repositorioLibroMayor,
            () => PeriodoActivo);
        _balanceSumasSaldosViewModel = new BalanceSumasSaldosViewModel(
            repositorioLibroMayor,
            () => PeriodoActivo);
        _estadoResultadosViewModel = new EstadoResultadosViewModel(
            repositorioLibroMayor,
            () => PeriodoActivo);
        _balanceGeneralViewModel = new BalanceGeneralViewModel(
            repositorioPeriodoContable,
            repositorioLibroMayor,
            () => _empresaActiva,
            () => PeriodoActivo);
        _reportesViewModel = new ReportesViewModel(
            repositorioPeriodoContable,
            repositorioAsiento,
            repositorioLibroMayor,
            () => _empresaActiva);
        NavegarLibroDiarioCommand = new ComandoAsync(NavegarALibroDiarioAsync);
        NavegarLibroMayorCommand = new ComandoAsync(NavegarALibroMayorAsync);
        NavegarBalanceSumasSaldosCommand = new ComandoAsync(NavegarABalanceSumasSaldosAsync);
        NavegarEstadoResultadosCommand = new ComandoAsync(NavegarAEstadoResultadosAsync);
        NavegarBalanceGeneralCommand = new ComandoAsync(NavegarABalanceGeneralAsync);
        NavegarReportesCommand = new ComandoAsync(NavegarAReportesAsync);
        _inicioViewModel = new InicioViewModel(
            NavegarACatalogoAsync,
            NavegarLibroDiarioCommand,
            NavegarLibroMayorCommand,
            NavegarBalanceSumasSaldosCommand,
            NavegarEstadoResultadosCommand,
            NavegarBalanceGeneralCommand);
        _empresaViewModel = new EmpresaViewModel(repositorioEmpresa, EstablecerEmpresaActiva);
        _periodoContableViewModel = new PeriodoContableViewModel(
            repositorioPeriodoContable,
            EstablecerPeriodoActivo);
        _vistaActual = _inicioViewModel;

        NavegarInicioCommand = new ComandoRelay(NavegarAInicio);
        NavegarPeriodosContablesCommand = new ComandoAsync(NavegarAPeriodosContablesAsync);
        NavegarCatalogoCommand = new ComandoAsync(NavegarACatalogoAsync);
        NavegarEmpresaCommand = new ComandoAsync(NavegarAEmpresaAsync);
    }

    public ViewModelBase VistaActual
    {
        get => _vistaActual;
        private set
        {
            if (EstablecerPropiedad(ref _vistaActual, value))
            {
                NotificarCambio(nameof(EsInicioActivo));
                NotificarCambio(nameof(EsPeriodosContablesActivo));
                NotificarCambio(nameof(EsCatalogoActivo));
                NotificarCambio(nameof(EsEmpresaActiva));
                NotificarCambio(nameof(EsReportesActivo));
            }
        }
    }

    public string EmpresaActivaTexto => _empresaActiva?.NombreEmpresa ?? "Sin configurar";

    public PeriodoContableListado? PeriodoActivo => _periodoActivo;

    public string PeriodoActivoTexto => _periodoActivo?.NombrePeriodo ?? "Sin seleccionar";

    public string FechaActualTexto { get; } =
        DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);

    public bool EsInicioActivo => ReferenceEquals(VistaActual, _inicioViewModel);

    public bool EsPeriodosContablesActivo =>
        ReferenceEquals(VistaActual, _periodoContableViewModel);

    public bool EsEmpresaActiva => ReferenceEquals(VistaActual, _empresaViewModel);

    public bool EsCatalogoActivo => ReferenceEquals(VistaActual, _catalogoCuentasViewModel);

    public bool EsReportesActivo => ReferenceEquals(VistaActual, _reportesViewModel);

    public ComandoRelay NavegarInicioCommand { get; }

    public ComandoAsync NavegarPeriodosContablesCommand { get; }

    public ComandoAsync NavegarCatalogoCommand { get; }

    public ComandoAsync NavegarLibroDiarioCommand { get; }

    public ComandoAsync NavegarLibroMayorCommand { get; }

    public ComandoAsync NavegarBalanceSumasSaldosCommand { get; }

    public ComandoAsync NavegarEstadoResultadosCommand { get; }

    public ComandoAsync NavegarBalanceGeneralCommand { get; }

    public ComandoAsync NavegarReportesCommand { get; }

    public ComandoAsync NavegarEmpresaCommand { get; }

    public Task InicializarAsync()
    {
        return _inicializacionTask ??= _empresaViewModel.CargarAsync();
    }

    public bool PuedeCerrarAplicacion()
    {
        return !_libroDiarioViewModel.TieneCambiosSinGuardar
            || _confirmarSalidaSinGuardar(MensajeCierreNuevoAsiento);
    }

    private void NavegarAInicio()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _inicioViewModel;
    }

    private async Task NavegarAPeriodosContablesAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _periodoContableViewModel;
        await InicializarAsync();
        await _periodoContableViewModel.CargarAsync(
            _empresaActiva,
            _empresaViewModel.MensajeError,
            _periodoActivo?.IdPeriodoContable);
    }

    private async Task NavegarACatalogoAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _catalogoCuentasViewModel;
        await _catalogoCuentasViewModel.CargarAsync();
    }

    private async Task NavegarALibroDiarioAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _libroDiarioViewModel;
        await _libroDiarioViewModel.CargarAsync();
    }

    private async Task NavegarALibroMayorAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _libroMayorViewModel;
        await _libroMayorViewModel.CargarAsync();
    }

    private async Task NavegarABalanceSumasSaldosAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _balanceSumasSaldosViewModel;
        await _balanceSumasSaldosViewModel.CargarAsync();
    }

    private async Task NavegarAEstadoResultadosAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _estadoResultadosViewModel;
        await _estadoResultadosViewModel.CargarAsync();
    }

    private async Task NavegarABalanceGeneralAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _balanceGeneralViewModel;
        await _balanceGeneralViewModel.CargarAsync();
    }

    private async Task NavegarAReportesAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _reportesViewModel;
        await InicializarAsync();
        await _reportesViewModel.CargarAsync();
    }

    private async Task NavegarAEmpresaAsync()
    {
        if (!IntentarAbandonarVistaActual())
        {
            return;
        }

        VistaActual = _empresaViewModel;
        await _empresaViewModel.CargarAsync();
    }

    private bool IntentarAbandonarVistaActual()
    {
        return !ReferenceEquals(VistaActual, _libroDiarioViewModel)
            || _libroDiarioViewModel.IntentarDescartarCaptura();
    }

    private void EstablecerEmpresaActiva(Empresa? empresa)
    {
        _empresaActiva = empresa;
        NotificarCambio(nameof(EmpresaActivaTexto));

        if (_periodoActivo is not null
            && (empresa is null || _periodoActivo.IdEmpresa != empresa.IdEmpresa))
        {
            EstablecerPeriodoActivo(null);
        }
    }

    private void EstablecerPeriodoActivo(PeriodoContableListado? periodo)
    {
        _periodoActivo = periodo;
        NotificarCambio(nameof(PeriodoActivo));
        NotificarCambio(nameof(PeriodoActivoTexto));
    }
}
