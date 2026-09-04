namespace ContaNexo.Core.Models;

public class DetalleCuenta
{
    public int IdDetalleCuenta { get; set; }

    public int IdCuentaContable { get; set; }

    public string? DescripcionDetalle { get; set; }

    public string? DinamicaDebitoDetalle { get; set; }

    public string? DinamicaCreditoDetalle { get; set; }
}
