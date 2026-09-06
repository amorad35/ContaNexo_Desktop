namespace ContaNexo.Core.Models;

public class AsientoListado
{
    public int IdAsiento { get; set; }

    public int NumeroAsiento { get; set; }

    public DateTime FechaAsiento { get; set; }

    public string TipoAsiento { get; set; } = string.Empty;

    public string? DescripcionAsiento { get; set; }

    public string EstadoAsiento { get; set; } = string.Empty;

    public decimal TotalDebe { get; set; }

    public decimal TotalHaber { get; set; }

    public int CantidadDetalles { get; set; }
}
