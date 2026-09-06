using ContaNexo.Core.Models;

namespace ContaNexo.Desktop.ViewModels;

public sealed class LineaAsientoViewModel : ViewModelBase
{
    private CuentaMovimiento? _cuentaSeleccionada;
    private decimal _debe;
    private decimal _haber;

    public CuentaMovimiento? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set => EstablecerPropiedad(ref _cuentaSeleccionada, value);
    }

    public decimal Debe
    {
        get => _debe;
        set => EstablecerPropiedad(ref _debe, value);
    }

    public decimal Haber
    {
        get => _haber;
        set => EstablecerPropiedad(ref _haber, value);
    }
}
