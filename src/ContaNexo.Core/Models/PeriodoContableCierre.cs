namespace ContaNexo.Core.Models;

public class PeriodoContableCierre
{
    public int IdPeriodoContable { get; set; }

    public string EstadoPeriodo { get; set; } = string.Empty;

    public DateTime? FechaCierrePeriodo { get; set; }
}
