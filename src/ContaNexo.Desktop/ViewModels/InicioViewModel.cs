namespace ContaNexo.Desktop.ViewModels;

public sealed class InicioViewModel : ViewModelBase
{
    public InicioViewModel(Func<Task> navegarCatalogoAsync)
    {
        NavegarCatalogoCommand = new ComandoAsync(navegarCatalogoAsync);
    }

    public ComandoAsync NavegarCatalogoCommand { get; }
}
