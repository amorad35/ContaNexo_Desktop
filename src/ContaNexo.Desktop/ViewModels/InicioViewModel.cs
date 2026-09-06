namespace ContaNexo.Desktop.ViewModels;

public sealed class InicioViewModel : ViewModelBase
{
    public InicioViewModel(
        Func<Task> navegarCatalogoAsync,
        Func<Task> navegarLibroDiarioAsync)
    {
        NavegarCatalogoCommand = new ComandoAsync(navegarCatalogoAsync);
        NavegarLibroDiarioCommand = new ComandoAsync(navegarLibroDiarioAsync);
    }

    public ComandoAsync NavegarCatalogoCommand { get; }

    public ComandoAsync NavegarLibroDiarioCommand { get; }
}
