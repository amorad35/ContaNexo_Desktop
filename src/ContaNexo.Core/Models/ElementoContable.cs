namespace ContaNexo.Core.Models;

public class ElementoContable
{
    public int IdElementoContable { get; set; }

    public string CodigoElemento { get; set; } = string.Empty;

    public string NombreElemento { get; set; } = string.Empty;

    public bool EstadoElemento { get; set; }
}
