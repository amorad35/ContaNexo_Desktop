using System.Globalization;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly InicioViewModel _inicioViewModel;
    private readonly EmpresaViewModel _empresaViewModel;
    private ViewModelBase _vistaActual;
    private Empresa? _empresaActiva;

    public MainWindowViewModel(RepositorioEmpresa repositorioEmpresa)
    {
        _inicioViewModel = new InicioViewModel();
        _empresaViewModel = new EmpresaViewModel(repositorioEmpresa, EstablecerEmpresaActiva);
        _vistaActual = _inicioViewModel;

        NavegarInicioCommand = new ComandoRelay(NavegarAInicio);
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
                NotificarCambio(nameof(EsEmpresaActiva));
            }
        }
    }

    public string EmpresaActivaTexto => _empresaActiva?.NombreEmpresa ?? "Sin configurar";

    public string PeriodoActivoTexto => "Sin seleccionar";

    public string FechaActualTexto { get; } =
        DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);

    public bool EsInicioActivo => ReferenceEquals(VistaActual, _inicioViewModel);

    public bool EsEmpresaActiva => ReferenceEquals(VistaActual, _empresaViewModel);

    public ComandoRelay NavegarInicioCommand { get; }

    public ComandoAsync NavegarEmpresaCommand { get; }

    private void NavegarAInicio()
    {
        VistaActual = _inicioViewModel;
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
    }
}
