namespace ContaNexo.Core.Models;

public class DetalleAsientoConsulta
{
    public int IdDetalleAsiento { get; set; }

    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public decimal DebeDetalle { get; set; }

    public decimal HaberDetalle { get; set; }

    public short OrdenDetalle { get; set; }
}
