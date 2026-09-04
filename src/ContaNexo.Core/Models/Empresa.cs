namespace ContaNexo.Core.Models;

public class Empresa
{
    public int IdEmpresa { get; set; }

    public string NombreEmpresa { get; set; } = string.Empty;

    public string? RucEmpresa { get; set; }

    public string? DireccionEmpresa { get; set; }

    public string? TelefonoEmpresa { get; set; }

    public string? CorreoEmpresa { get; set; }
}
