using ContaNexo.Core.Calculations;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class EstadoResultadosViewModel : ViewModelBase
{
    private readonly RepositorioLibroMayor _repositorioLibroMayor;
    private readonly Func<PeriodoContableListado?> _obtenerPeriodoActivo;
    private PeriodoContableListado? _periodoActivo;
    private EstadoResultadosResumen? _resumen;
    private bool _estaCargando;
    private string _mensajeError = string.Empty;

    public EstadoResultadosViewModel(
        RepositorioLibroMayor repositorioLibroMayor,
        Func<PeriodoContableListado?> obtenerPeriodoActivo)
    {
        _repositorioLibroMayor = repositorioLibroMayor;
        _obtenerPeriodoActivo = obtenerPeriodoActivo;
    }

    public PeriodoContableListado? PeriodoActivo => _periodoActivo;

    public string NombrePeriodoActivo => PeriodoActivo?.NombrePeriodo ?? "Sin seleccionar";

    public DateTime? FechaInicioPeriodoActivo => PeriodoActivo?.FechaInicioPeriodo;

    public DateTime? FechaFinPeriodoActivo => PeriodoActivo?.FechaFinPeriodo;

    public string EstadoPeriodoActivo => PeriodoActivo?.EstadoPeriodo ?? string.Empty;

    public bool TienePeriodoActivo => PeriodoActivo is not null;

    public EstadoResultadosResumen? Resumen
    {
        get => _resumen;
        private set
        {
            if (EstablecerPropiedad(ref _resumen, value))
            {
                NotificarResumen();
            }
        }
    }

    public IReadOnlyList<EstadoResultadosGrupo> GruposIngresos =>
        Resumen?.GruposIngresos ?? [];

    public IReadOnlyList<EstadoResultadosGrupo> GruposGastos =>
        Resumen?.GruposGastos ?? [];

    public decimal TotalIngresos => Resumen?.TotalIngresos ?? 0;

    public decimal TotalGastos => Resumen?.TotalGastos ?? 0;

    public decimal ResultadoPeriodo => Resumen?.ResultadoPeriodo ?? 0;

    public int CantidadSaldosContrarios => Resumen?.CantidadSaldosContrarios ?? 0;

    public bool TieneSaldosContrarios => Resumen?.TieneSaldosContrarios ?? false;

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

    public async Task CargarAsync()
    {
        ActualizarPeriodoActivo();
        Resumen = null;
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

            Resumen = EstadoResultadosCalculador.Calcular(cuentas);
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError =
                "No se pudo cargar el Estado de Resultados. Verifica la conexión e inténtalo nuevamente.";
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

    private void NotificarResumen()
    {
        NotificarCambio(nameof(GruposIngresos));
        NotificarCambio(nameof(GruposGastos));
        NotificarCambio(nameof(TotalIngresos));
        NotificarCambio(nameof(TotalGastos));
        NotificarCambio(nameof(ResultadoPeriodo));
        NotificarCambio(nameof(CantidadSaldosContrarios));
        NotificarCambio(nameof(TieneSaldosContrarios));
    }
}
