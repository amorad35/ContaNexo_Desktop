namespace ContaNexo.Core.Models;

public class PeriodoContable
{
    public int IdPeriodoContable { get; set; }

    public int IdEmpresa { get; set; }

    public string NombrePeriodo { get; set; } = string.Empty;

    public DateTime FechaInicioPeriodo { get; set; }

    public DateTime FechaFinPeriodo { get; set; }

    public string EstadoPeriodo { get; set; } = string.Empty;

    public DateTime? FechaCierrePeriodo { get; set; }
}
