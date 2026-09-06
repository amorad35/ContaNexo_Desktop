namespace ContaNexo.Desktop.ViewModels;

public sealed class InicioViewModel : ViewModelBase
{
    public InicioViewModel(
        Func<Task> navegarCatalogoAsync,
        ComandoAsync navegarLibroDiarioCommand)
    {
        NavegarCatalogoCommand = new ComandoAsync(navegarCatalogoAsync);
        NavegarLibroDiarioCommand = navegarLibroDiarioCommand;
    }

    public ComandoAsync NavegarCatalogoCommand { get; }

    public ComandoAsync NavegarLibroDiarioCommand { get; }
}
