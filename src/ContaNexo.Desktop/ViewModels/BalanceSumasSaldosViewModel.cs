using System.Collections.ObjectModel;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class BalanceSumasSaldosViewModel : ViewModelBase
{
    private readonly RepositorioLibroMayor _repositorioLibroMayor;
    private readonly Func<PeriodoContableListado?> _obtenerPeriodoActivo;
    private PeriodoContableListado? _periodoActivo;
    private bool _estaCargando;
    private string _mensajeError = string.Empty;

    public BalanceSumasSaldosViewModel(
        RepositorioLibroMayor repositorioLibroMayor,
        Func<PeriodoContableListado?> obtenerPeriodoActivo)
    {
        _repositorioLibroMayor = repositorioLibroMayor;
        _obtenerPeriodoActivo = obtenerPeriodoActivo;
    }

    public ObservableCollection<BalanceSumasSaldosItemViewModel> Cuentas { get; } = new();

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

    public decimal TotalDebe => Cuentas.Sum(cuenta => cuenta.TotalDebe);

    public decimal TotalHaber => Cuentas.Sum(cuenta => cuenta.TotalHaber);

    public decimal TotalSaldoDeudor => Cuentas.Sum(cuenta => cuenta.SaldoDeudor);

    public decimal TotalSaldoAcreedor => Cuentas.Sum(cuenta => cuenta.SaldoAcreedor);

    public bool EstaCuadrado => TotalDebe == TotalHaber;

    public bool SaldosCuadrados => TotalSaldoDeudor == TotalSaldoAcreedor;

    public int CantidadInconsistenciasSaldo =>
        Cuentas.Count(cuenta => cuenta.TieneInconsistenciaSaldo);

    public bool TieneInconsistenciasSaldo => CantidadInconsistenciasSaldo > 0;

    public int CantidadSaldosContrariosNaturaleza =>
        Cuentas.Count(cuenta => cuenta.TieneSaldoContrarioNaturaleza);

    public bool TieneAdvertenciasNaturaleza =>
        CantidadSaldosContrariosNaturaleza > 0;

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
                Cuentas.Add(new BalanceSumasSaldosItemViewModel(cuenta));
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
                "No se pudo cargar el Balance de Sumas y Saldos. Verifica la conexión e inténtalo nuevamente.";
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
        NotificarCambio(nameof(TotalDebe));
        NotificarCambio(nameof(TotalHaber));
        NotificarCambio(nameof(TotalSaldoDeudor));
        NotificarCambio(nameof(TotalSaldoAcreedor));
        NotificarCambio(nameof(EstaCuadrado));
        NotificarCambio(nameof(SaldosCuadrados));
        NotificarCambio(nameof(CantidadInconsistenciasSaldo));
        NotificarCambio(nameof(TieneInconsistenciasSaldo));
        NotificarCambio(nameof(CantidadSaldosContrariosNaturaleza));
        NotificarCambio(nameof(TieneAdvertenciasNaturaleza));
    }
}

public sealed class BalanceSumasSaldosItemViewModel
{
    public BalanceSumasSaldosItemViewModel(LibroMayorCuenta cuenta)
    {
        CodigoCuenta = cuenta.CodigoCuenta;
        NombreCuenta = cuenta.NombreCuenta;
        NaturalezaCuenta = cuenta.NaturalezaCuenta;
        TotalDebe = cuenta.TotalDebe;
        TotalHaber = cuenta.TotalHaber;
        SaldoDeudor = cuenta.SaldoDeudor;
        SaldoAcreedor = cuenta.SaldoAcreedor;
    }

    public string CodigoCuenta { get; }

    public string NombreCuenta { get; }

    public string NaturalezaCuenta { get; }

    public decimal TotalDebe { get; }

    public decimal TotalHaber { get; }

    public decimal SaldoDeudor { get; }

    public decimal SaldoAcreedor { get; }

    public bool TieneInconsistenciaSaldo =>
        SaldoDeudor < 0
        || SaldoAcreedor < 0
        || (SaldoDeudor > 0 && SaldoAcreedor > 0)
        || SaldoDeudor != SaldoDeudorEsperado
        || SaldoAcreedor != SaldoAcreedorEsperado;

    public bool TieneSaldoContrarioNaturaleza =>
        string.Equals(NaturalezaCuenta, "Deudora", StringComparison.Ordinal)
            ? SaldoAcreedor > 0
            : string.Equals(NaturalezaCuenta, "Acreedora", StringComparison.Ordinal)
                && SaldoDeudor > 0;

    private decimal SaldoDeudorEsperado => TotalDebe > TotalHaber
        ? TotalDebe - TotalHaber
        : 0;

    private decimal SaldoAcreedorEsperado => TotalHaber > TotalDebe
        ? TotalHaber - TotalDebe
        : 0;
}
