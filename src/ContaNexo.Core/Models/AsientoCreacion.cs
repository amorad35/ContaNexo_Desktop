namespace ContaNexo.Core.Models;

public class AsientoCreacion
{
    public int IdPeriodoContable { get; set; }

    public DateTime FechaAsiento { get; set; }

    public string TipoAsiento { get; set; } = string.Empty;

    public string? DescripcionAsiento { get; set; }

    public List<DetalleAsientoCreacion> Detalles { get; set; } = new();
}
