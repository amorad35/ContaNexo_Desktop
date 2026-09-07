namespace ContaNexo.Core.Models;

public class LibroMayorCuenta
{
    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public string NaturalezaCuenta { get; set; } = string.Empty;

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

    public int? OrdenCuentaPadre { get; set; }

    public bool TieneHijas { get; set; }

    public decimal TotalDebe { get; set; }

    public decimal TotalHaber { get; set; }

    public decimal SaldoDeudor { get; set; }

    public decimal SaldoAcreedor { get; set; }

    public IReadOnlyList<LibroMayorMovimiento> Movimientos { get; set; } = [];
}
