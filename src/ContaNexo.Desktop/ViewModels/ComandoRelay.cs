using System.Windows.Input;

namespace ContaNexo.Desktop.ViewModels;

public sealed class ComandoRelay : ICommand
{
    private readonly Action<object?> _ejecutar;
    private readonly Predicate<object?>? _puedeEjecutar;

    public ComandoRelay(Action ejecutar, Func<bool>? puedeEjecutar = null)
    {
        _ejecutar = _ => ejecutar();
        _puedeEjecutar = puedeEjecutar is null ? null : _ => puedeEjecutar();
    }

    public ComandoRelay(
        Action<object?> ejecutar,
        Predicate<object?>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _puedeEjecutar?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _ejecutar(parameter);
    }

    public void NotificarPuedeEjecutar()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
