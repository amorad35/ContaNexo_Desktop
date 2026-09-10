namespace ContaNexo.Core.Models;

public sealed class BalanceGeneralSubcuenta
{
    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public string NaturalezaCuenta { get; set; } = string.Empty;

    public int OrdenCuenta { get; set; }

    public decimal Importe { get; set; }

    public decimal ImpactoEnTotal { get; set; }

    public bool TieneSaldoContrario { get; set; }
}
