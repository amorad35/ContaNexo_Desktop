using System.Globalization;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly InicioViewModel _inicioViewModel;
    private readonly EmpresaViewModel _empresaViewModel;
    private readonly PeriodoContableViewModel _periodoContableViewModel;
    private ViewModelBase _vistaActual;
    private Empresa? _empresaActiva;
    private PeriodoContableListado? _periodoActivo;
    private Task? _inicializacionTask;

    public MainWindowViewModel(
        RepositorioEmpresa repositorioEmpresa,
        RepositorioPeriodoContable repositorioPeriodoContable)
    {
        _inicioViewModel = new InicioViewModel();
        _empresaViewModel = new EmpresaViewModel(repositorioEmpresa, EstablecerEmpresaActiva);
        _periodoContableViewModel = new PeriodoContableViewModel(
            repositorioPeriodoContable,
            EstablecerPeriodoActivo);
        _vistaActual = _inicioViewModel;

        NavegarInicioCommand = new ComandoRelay(NavegarAInicio);
        NavegarPeriodosContablesCommand = new ComandoAsync(NavegarAPeriodosContablesAsync);
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
                NotificarCambio(nameof(EsEmpresaActiva));
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

    public ComandoRelay NavegarInicioCommand { get; }

    public ComandoAsync NavegarPeriodosContablesCommand { get; }

    public ComandoAsync NavegarEmpresaCommand { get; }

    public Task InicializarAsync()
    {
        return _inicializacionTask ??= _empresaViewModel.CargarAsync();
    }

    private void NavegarAInicio()
    {
        VistaActual = _inicioViewModel;
    }

    private async Task NavegarAPeriodosContablesAsync()
    {
        VistaActual = _periodoContableViewModel;
        await InicializarAsync();
        await _periodoContableViewModel.CargarAsync(
            _empresaActiva,
            _empresaViewModel.MensajeError,
            _periodoActivo?.IdPeriodoContable);
    }

    private async Task NavegarAEmpresaAsync()
    {
        VistaActual = _empresaViewModel;
        await _empresaViewModel.CargarAsync();
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
