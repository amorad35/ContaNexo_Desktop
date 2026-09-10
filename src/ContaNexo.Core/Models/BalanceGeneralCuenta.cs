namespace ContaNexo.Core.Models;

public sealed class BalanceGeneralCuenta
{
    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public string NaturalezaCuenta { get; set; } = string.Empty;

    public int OrdenCuenta { get; set; }

    public string CodigoGrupo { get; set; } = string.Empty;

    public string CodigoElemento { get; set; } = string.Empty;

    public decimal ImporteDirecto { get; set; }

    public decimal ImporteConsolidado { get; set; }

    public decimal ImpactoEnTotal { get; set; }

    public bool EsReductora { get; set; }

    public bool TieneSaldoContrarioDirecto { get; set; }

    public IReadOnlyList<BalanceGeneralSubcuenta> Subcuentas { get; set; } = [];

    public string NombrePresentacion => EsReductora
        ? $"(-) {NombreCuenta}"
        : NombreCuenta;

    public bool TieneSaldoContrario =>
        TieneSaldoContrarioDirecto
        || Subcuentas.Any(subcuenta => subcuenta.TieneSaldoContrario);
}
