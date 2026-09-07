namespace ContaNexo.Desktop.ViewModels;

public sealed class InicioViewModel : ViewModelBase
{
    public InicioViewModel(
        Func<Task> navegarCatalogoAsync,
        ComandoAsync navegarLibroDiarioCommand,
        ComandoAsync navegarLibroMayorCommand)
    {
        NavegarCatalogoCommand = new ComandoAsync(navegarCatalogoAsync);
        NavegarLibroDiarioCommand = navegarLibroDiarioCommand;
        NavegarLibroMayorCommand = navegarLibroMayorCommand;
    }

    public ComandoAsync NavegarCatalogoCommand { get; }

    public ComandoAsync NavegarLibroDiarioCommand { get; }

    public ComandoAsync NavegarLibroMayorCommand { get; }
}
