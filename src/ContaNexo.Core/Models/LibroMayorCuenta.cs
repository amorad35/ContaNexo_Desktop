namespace ContaNexo.Core.Models;

public class LibroMayorCuenta
{
    public int IdCuentaContable { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public string NaturalezaCuenta { get; set; } = string.Empty;

    public int OrdenCuenta { get; set; }

    public decimal TotalDebe { get; set; }

    public decimal TotalHaber { get; set; }

    public decimal SaldoDeudor { get; set; }

    public decimal SaldoAcreedor { get; set; }

    public IReadOnlyList<LibroMayorMovimiento> Movimientos { get; set; } = [];
}
