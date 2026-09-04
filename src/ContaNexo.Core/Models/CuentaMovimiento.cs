namespace ContaNexo.Core.Models;

public class CuentaMovimiento
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
}
