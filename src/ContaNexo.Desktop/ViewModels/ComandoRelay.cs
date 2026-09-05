using System.Windows.Input;

namespace ContaNexo.Desktop.ViewModels;

public sealed class ComandoRelay : ICommand
{
    private readonly Action _ejecutar;
    private readonly Func<bool>? _puedeEjecutar;

    public ComandoRelay(Action ejecutar, Func<bool>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _puedeEjecutar?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _ejecutar();
    }

    public void NotificarPuedeEjecutar()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
