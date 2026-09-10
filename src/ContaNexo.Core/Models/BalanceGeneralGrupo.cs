namespace ContaNexo.Core.Models;

public sealed class BalanceGeneralGrupo
{
    public string CodigoElemento { get; set; } = string.Empty;

    public string CodigoGrupo { get; set; } = string.Empty;

    public string NombreGrupo { get; set; } = string.Empty;

    public IReadOnlyList<BalanceGeneralCuenta> Cuentas { get; set; } = [];

    public decimal TotalGrupo { get; set; }
}
