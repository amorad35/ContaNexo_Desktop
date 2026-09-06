namespace ContaNexo.Core.Models;

public class DetalleAsientoCreacion
{
    public int IdCuentaContable { get; set; }

    public decimal DebeDetalle { get; set; }

    public decimal HaberDetalle { get; set; }

    public short OrdenDetalle { get; set; }
}
