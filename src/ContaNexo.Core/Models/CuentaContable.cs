namespace ContaNexo.Core.Models;

public class CuentaContable
{
    public int IdCuentaContable { get; set; }

    public int IdGrupoContable { get; set; }

    public int? IdCuentaPadre { get; set; }

    public string CodigoCuenta { get; set; } = string.Empty;

    public string NombreCuenta { get; set; } = string.Empty;

    public string NaturalezaCuenta { get; set; } = string.Empty;

    public bool PermiteMovimientoCuenta { get; set; }

    public bool EstadoCuenta { get; set; }

    public int OrdenCuenta { get; set; }
}
