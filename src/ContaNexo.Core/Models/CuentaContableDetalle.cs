namespace ContaNexo.Core.Models;

public class CuentaContableDetalle
{
    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public string NaturalezaCuenta { get; set; } = string.Empty;

    public bool EstadoCuenta { get; set; }

    public bool PermiteMovimientoCuenta { get; set; }

    public int OrdenCuenta { get; set; }

    public int IdGrupoContable { get; set; }

    public string CodigoGrupo { get; set; } = string.Empty;

    public string NombreGrupo { get; set; } = string.Empty;

    public int IdElementoContable { get; set; }

    public string CodigoElemento { get; set; } = string.Empty;

    public string NombreElemento { get; set; } = string.Empty;

    public int? IdCuentaPadre { get; set; }

    public string? CodigoCuentaPadre { get; set; }

    public string? NombreCuentaPadre { get; set; }

    public bool TieneHijas { get; set; }

    public int? IdDetalleCuenta { get; set; }

    public string? DescripcionDetalle { get; set; }

    public string? DinamicaDebitoDetalle { get; set; }

    public string? DinamicaCreditoDetalle { get; set; }
}
