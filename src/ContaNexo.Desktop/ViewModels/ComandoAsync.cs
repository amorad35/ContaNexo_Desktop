using System.Windows.Input;

namespace ContaNexo.Desktop.ViewModels;

public sealed class ComandoAsync : ICommand
{
    private readonly Func<object?, Task> _ejecutarAsync;
    private readonly Predicate<object?>? _puedeEjecutar;
    private bool _estaEjecutando;

    public ComandoAsync(Func<Task> ejecutarAsync, Func<bool>? puedeEjecutar = null)
    {
        _ejecutarAsync = _ => ejecutarAsync();
        _puedeEjecutar = puedeEjecutar is null ? null : _ => puedeEjecutar();
    }

    public ComandoAsync(
        Func<object?, Task> ejecutarAsync,
        Predicate<object?>? puedeEjecutar = null)
    {
        _ejecutarAsync = ejecutarAsync;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_estaEjecutando && (_puedeEjecutar?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _estaEjecutando = true;
        NotificarPuedeEjecutar();

        try
        {
            await _ejecutarAsync(parameter);
        }
        finally
        {
            _estaEjecutando = false;
            NotificarPuedeEjecutar();
        }
    }

    public void NotificarPuedeEjecutar()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
