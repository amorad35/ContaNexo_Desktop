namespace ContaNexo.Data.Connections;

public static class ConfiguracionBD
{
    public static string CadenaConexion { get; } =
        "Server=MSI\\SQLEXPRESS01;Database=ContaNexoDB;Integrated Security=True;TrustServerCertificate=True;";
}
