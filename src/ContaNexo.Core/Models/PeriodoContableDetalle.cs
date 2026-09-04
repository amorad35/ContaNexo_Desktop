namespace ContaNexo.Core.Models;

public class PeriodoContableDetalle
{
    public int IdPeriodoContable { get; set; }

    public int IdEmpresa { get; set; }

    public string NombreEmpresa { get; set; } = string.Empty;

    public string? RucEmpresa { get; set; }

    public string NombrePeriodo { get; set; } = string.Empty;

    public DateTime FechaInicioPeriodo { get; set; }

    public DateTime FechaFinPeriodo { get; set; }

    public string EstadoPeriodo { get; set; } = string.Empty;

    public DateTime? FechaCierrePeriodo { get; set; }
}
