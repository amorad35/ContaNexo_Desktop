namespace ContaNexo.Core.Models;

public class EstadoResultadosGrupo
{
    public int IdGrupoContable { get; set; }

    public string CodigoGrupo { get; set; } = string.Empty;

    public string NombreGrupo { get; set; } = string.Empty;

    public int IdElementoContable { get; set; }

    public string CodigoElemento { get; set; } = string.Empty;

    public string NombreElemento { get; set; } = string.Empty;

    public IReadOnlyList<EstadoResultadosCuenta> Cuentas { get; set; } = [];

    public decimal TotalGrupo { get; set; }
}
