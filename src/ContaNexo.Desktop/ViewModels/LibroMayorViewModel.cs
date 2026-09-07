using System.Collections.ObjectModel;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class LibroMayorViewModel : ViewModelBase
{
    private readonly RepositorioLibroMayor _repositorioLibroMayor;
    private readonly Func<PeriodoContableListado?> _obtenerPeriodoActivo;
    private PeriodoContableListado? _periodoActivo;
    private bool _estaCargando;
    private string _mensajeError = string.Empty;

    public LibroMayorViewModel(
        RepositorioLibroMayor repositorioLibroMayor,
        Func<PeriodoContableListado?> obtenerPeriodoActivo)
    {
        _repositorioLibroMayor = repositorioLibroMayor;
        _obtenerPeriodoActivo = obtenerPeriodoActivo;
    }

    public ObservableCollection<LibroMayorCuentaItemViewModel> Cuentas { get; } = new();

    public PeriodoContableListado? PeriodoActivo => _periodoActivo;

    public string NombrePeriodoActivo => PeriodoActivo?.NombrePeriodo ?? "Sin seleccionar";

    public DateTime? FechaInicioPeriodoActivo => PeriodoActivo?.FechaInicioPeriodo;

    public DateTime? FechaFinPeriodoActivo => PeriodoActivo?.FechaFinPeriodo;

    public string EstadoPeriodoActivo => PeriodoActivo?.EstadoPeriodo ?? string.Empty;

    public bool TienePeriodoActivo => PeriodoActivo is not null;

    public bool EstaCargando
    {
        get => _estaCargando;
        private set => EstablecerPropiedad(ref _estaCargando, value);
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeError, value))
            {
                NotificarCambio(nameof(TieneError));
            }
        }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public bool TieneCuentas => Cuentas.Count > 0;

    public int CantidadCuentas => Cuentas.Count;

    public decimal TotalDebe => Cuentas.Sum(cuenta => cuenta.Cuenta.TotalDebe);

    public decimal TotalHaber => Cuentas.Sum(cuenta => cuenta.Cuenta.TotalHaber);

    public bool EstaCuadrado => TieneCuentas && TotalDebe == TotalHaber;

    public async Task CargarAsync()
    {
        ActualizarPeriodoActivo();
        LimpiarCuentas();
        MensajeError = string.Empty;

        if (PeriodoActivo is null)
        {
            return;
        }

        EstaCargando = true;

        try
        {
            IReadOnlyList<LibroMayorCuenta> cuentas =
                await _repositorioLibroMayor.ObtenerPorPeriodoAsync(
                    PeriodoActivo.IdPeriodoContable);

            foreach (LibroMayorCuenta cuenta in cuentas)
            {
                Cuentas.Add(new LibroMayorCuentaItemViewModel(cuenta));
            }

            NotificarResumen();
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError =
                "No se pudo cargar el Libro Mayor. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void ActualizarPeriodoActivo()
    {
        _periodoActivo = _obtenerPeriodoActivo();
        NotificarCambio(nameof(PeriodoActivo));
        NotificarCambio(nameof(NombrePeriodoActivo));
        NotificarCambio(nameof(FechaInicioPeriodoActivo));
        NotificarCambio(nameof(FechaFinPeriodoActivo));
        NotificarCambio(nameof(EstadoPeriodoActivo));
        NotificarCambio(nameof(TienePeriodoActivo));
    }

    private void LimpiarCuentas()
    {
        Cuentas.Clear();
        NotificarResumen();
    }

    private void NotificarResumen()
    {
        NotificarCambio(nameof(TieneCuentas));
        NotificarCambio(nameof(CantidadCuentas));
        NotificarCambio(nameof(TotalDebe));
        NotificarCambio(nameof(TotalHaber));
        NotificarCambio(nameof(EstaCuadrado));
    }
}

public sealed class LibroMayorCuentaItemViewModel
{
    public LibroMayorCuentaItemViewModel(LibroMayorCuenta cuenta)
    {
        Cuenta = cuenta;
        Movimientos = cuenta.Movimientos
            .Select(movimiento => new LibroMayorMovimientoItemViewModel(movimiento))
            .ToList();
    }

    public LibroMayorCuenta Cuenta { get; }

    public IReadOnlyList<LibroMayorMovimientoItemViewModel> Movimientos { get; }

    public bool EsSaldoDeudor => Cuenta.SaldoDeudor > 0;

    public bool EsSaldoAcreedor => Cuenta.SaldoAcreedor > 0;

    public bool EsSaldoCero => !EsSaldoDeudor && !EsSaldoAcreedor;

    public decimal ValorSaldo => EsSaldoDeudor
        ? Cuenta.SaldoDeudor
        : Cuenta.SaldoAcreedor;
}

public sealed class LibroMayorMovimientoItemViewModel
{
    public LibroMayorMovimientoItemViewModel(LibroMayorMovimiento movimiento)
    {
        Movimiento = movimiento;
    }

    public LibroMayorMovimiento Movimiento { get; }

    public bool EsDebe => Movimiento.Debe > 0;

    public bool EsHaber => Movimiento.Haber > 0;

    public string ReferenciaAsiento => string.Equals(
        Movimiento.TipoAsiento,
        "Ajuste",
        StringComparison.OrdinalIgnoreCase)
            ? $"(A{Movimiento.NumeroAsiento})"
            : $"({Movimiento.NumeroAsiento})";

    public string Descripcion => string.IsNullOrWhiteSpace(Movimiento.DescripcionAsiento)
        ? "Sin descripción"
        : Movimiento.DescripcionAsiento;
}
