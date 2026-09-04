namespace ContaNexo.Core.Models;

public class GrupoContable
{
    public int IdGrupoContable { get; set; }

    public int IdElementoContable { get; set; }

    public string CodigoGrupo { get; set; } = string.Empty;

    public string NombreGrupo { get; set; } = string.Empty;

    public bool EstadoGrupo { get; set; }
}
