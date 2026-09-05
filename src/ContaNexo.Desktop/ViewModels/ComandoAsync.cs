using System.Windows.Input;

namespace ContaNexo.Desktop.ViewModels;

public sealed class ComandoAsync : ICommand
{
    private readonly Func<Task> _ejecutarAsync;
    private readonly Func<bool>? _puedeEjecutar;
    private bool _estaEjecutando;

    public ComandoAsync(Func<Task> ejecutarAsync, Func<bool>? puedeEjecutar = null)
    {
        _ejecutarAsync = ejecutarAsync;
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_estaEjecutando && (_puedeEjecutar?.Invoke() ?? true);
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
            await _ejecutarAsync();
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
