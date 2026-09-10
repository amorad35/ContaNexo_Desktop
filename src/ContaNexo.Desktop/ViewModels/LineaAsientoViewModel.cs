using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using ContaNexo.Core.Models;

namespace ContaNexo.Desktop.ViewModels;

public sealed class LineaAsientoViewModel : ViewModelBase
{
    private CuentaMovimiento? _cuentaSeleccionada;
    private string _textoBusquedaCuenta = string.Empty;
    private string _textoDebe = string.Empty;
    private string _textoHaber = string.Empty;
    private decimal? _debe;
    private decimal? _haber;
    private bool _actualizandoTextoDesdeSeleccion;

    public LineaAsientoViewModel(
        ObservableCollection<CuentaMovimiento> cuentasMovimiento)
    {
        CuentasFiltradas = new ListCollectionView(cuentasMovimiento)
        {
            Filter = FiltrarCuenta
        };
    }

    public ICollectionView CuentasFiltradas { get; }

    public CuentaMovimiento? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set
        {
            if (!EstablecerPropiedad(ref _cuentaSeleccionada, value)
                || value is null)
            {
                return;
            }

            _actualizandoTextoDesdeSeleccion = true;
            TextoBusquedaCuenta = FormatearCuenta(value);
            _actualizandoTextoDesdeSeleccion = false;
        }
    }

    public string TextoBusquedaCuenta
    {
        get => _textoBusquedaCuenta;
        set
        {
            string texto = value ?? string.Empty;

            if (!EstablecerPropiedad(ref _textoBusquedaCuenta, texto))
            {
                return;
            }

            if (!_actualizandoTextoDesdeSeleccion
                && CuentaSeleccionada is not null
                && !string.Equals(
                    texto,
                    FormatearCuenta(CuentaSeleccionada),
                    StringComparison.CurrentCulture))
            {
                CuentaSeleccionada = null;
            }

            CuentasFiltradas.Refresh();
        }
    }

    public decimal? Debe
    {
        get => _debe;
        set
        {
            if (EstablecerPropiedad(ref _debe, value))
            {
                EstablecerPropiedad(
                    ref _textoDebe,
                    FormatearImporte(value),
                    nameof(TextoDebe));
            }
        }
    }

    public decimal? Haber
    {
        get => _haber;
        set
        {
            if (EstablecerPropiedad(ref _haber, value))
            {
                EstablecerPropiedad(
                    ref _textoHaber,
                    FormatearImporte(value),
                    nameof(TextoHaber));
            }
        }
    }

    public string TextoDebe
    {
        get => _textoDebe;
        set => EstablecerTextoImporte(
            value,
            ref _textoDebe,
            ref _debe,
            nameof(TextoDebe),
            nameof(Debe));
    }

    public string TextoHaber
    {
        get => _textoHaber;
        set => EstablecerTextoImporte(
            value,
            ref _textoHaber,
            ref _haber,
            nameof(TextoHaber),
            nameof(Haber));
    }

    private void EstablecerTextoImporte(
        string? valor,
        ref string campoTexto,
        ref decimal? campoImporte,
        string nombrePropiedadTexto,
        string nombrePropiedadImporte)
    {
        string texto = valor ?? string.Empty;

        if (!IntentarObtenerImporte(texto, out decimal? importe))
        {
            NotificarCambio(nombrePropiedadTexto);
            return;
        }

        if (!EstablecerPropiedad(
                ref campoTexto,
                texto,
                nombrePropiedadTexto))
        {
            return;
        }

        EstablecerPropiedad(
            ref campoImporte,
            importe,
            nombrePropiedadImporte);
    }

    private static bool IntentarObtenerImporte(
        string texto,
        out decimal? importe)
    {
        if (texto.Length == 0)
        {
            importe = null;
            return true;
        }

        if (decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal valor))
        {
            importe = valor;
            return true;
        }

        importe = null;
        return false;
    }

    private static string FormatearImporte(decimal? importe)
    {
        return importe?.ToString("N2", CultureInfo.CurrentCulture)
            ?? string.Empty;
    }

    private bool FiltrarCuenta(object elemento)
    {
        if (elemento is not CuentaMovimiento cuenta)
        {
            return false;
        }

        string busqueda = TextoBusquedaCuenta.Trim();

        return busqueda.Length == 0
            || cuenta.CodigoCuenta.Contains(
                busqueda,
                StringComparison.CurrentCultureIgnoreCase)
            || cuenta.NombreCuenta.Contains(
                busqueda,
                StringComparison.CurrentCultureIgnoreCase)
            || FormatearCuenta(cuenta).Contains(
                busqueda,
                StringComparison.CurrentCultureIgnoreCase);
    }

    internal static string FormatearCuenta(CuentaMovimiento cuenta)
    {
        return $"{cuenta.CodigoCuenta} — {cuenta.NombreCuenta}";
    }
}
