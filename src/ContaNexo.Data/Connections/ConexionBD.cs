using Microsoft.Data.SqlClient;

namespace ContaNexo.Data.Connections;

public class ConexionBD
{
    private readonly string _cadenaConexion;

    public ConexionBD(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    public SqlConnection CrearConexion()
    {
        return new SqlConnection(_cadenaConexion);
    }

    public async Task<bool> ProbarConexionAsync()
    {
        try
        {
            await using SqlConnection conexion = CrearConexion();
            await conexion.OpenAsync();

            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }
}
