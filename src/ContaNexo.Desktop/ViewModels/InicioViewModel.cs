namespace ContaNexo.Desktop.ViewModels;

public sealed class InicioViewModel : ViewModelBase
{
    public InicioViewModel(
        Func<Task> navegarCatalogoAsync,
        ComandoAsync navegarLibroDiarioCommand,
        ComandoAsync navegarLibroMayorCommand,
        ComandoAsync navegarBalanceSumasSaldosCommand,
        ComandoAsync navegarEstadoResultadosCommand,
        ComandoAsync navegarBalanceGeneralCommand)
    {
        NavegarCatalogoCommand = new ComandoAsync(navegarCatalogoAsync);
        NavegarLibroDiarioCommand = navegarLibroDiarioCommand;
        NavegarLibroMayorCommand = navegarLibroMayorCommand;
        NavegarBalanceSumasSaldosCommand = navegarBalanceSumasSaldosCommand;
        NavegarEstadoResultadosCommand = navegarEstadoResultadosCommand;
        NavegarBalanceGeneralCommand = navegarBalanceGeneralCommand;
    }

    public ComandoAsync NavegarCatalogoCommand { get; }

    public ComandoAsync NavegarLibroDiarioCommand { get; }

    public ComandoAsync NavegarLibroMayorCommand { get; }

    public ComandoAsync NavegarBalanceSumasSaldosCommand { get; }

    public ComandoAsync NavegarEstadoResultadosCommand { get; }

    public ComandoAsync NavegarBalanceGeneralCommand { get; }
}
