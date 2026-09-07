namespace ContaNexo.Core.Models;

public class EstadoResultadosCuenta
{
    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public int OrdenCuenta { get; set; }

    public int IdGrupoContable { get; set; }

    public string CodigoGrupo { get; set; } = string.Empty;

    public string NombreGrupo { get; set; } = string.Empty;

    public int IdElementoContable { get; set; }

    public string CodigoElemento { get; set; } = string.Empty;

    public string NombreElemento { get; set; } = string.Empty;

    public decimal ImporteDirecto { get; set; }

    public decimal ImporteConsolidado { get; set; }

    public bool TieneSaldoContrarioDirecto { get; set; }

    public IReadOnlyList<EstadoResultadosSubcuenta> Subcuentas { get; set; } = [];

    public bool TieneSubcuentas => Subcuentas.Count > 0;

    public bool TieneSubcuentasConSaldoContrario =>
        Subcuentas.Any(subcuenta => subcuenta.TieneSaldoContrario);

    public bool TieneSaldoContrario =>
        TieneSaldoContrarioDirecto || TieneSubcuentasConSaldoContrario;
}
