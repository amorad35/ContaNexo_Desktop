namespace ContaNexo.Core.Models;

public class LibroMayorMovimiento
{
    public int IdDetalleAsiento { get; set; }

    public int IdCuentaContable { get; set; }

    public int IdAsiento { get; set; }

    public int NumeroAsiento { get; set; }

    public DateTime FechaAsiento { get; set; }

    public string TipoAsiento { get; set; } = string.Empty;

    public string? DescripcionAsiento { get; set; }

    public decimal Debe { get; set; }

    public decimal Haber { get; set; }

    public short OrdenDetalle { get; set; }
}
