using System.Data;
using ContaNexo.Core.Models;
using ContaNexo.Data.Connections;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ContaNexo.Data.Repositories;

public class RepositorioCuentaContable
{
    private readonly ConexionBD _conexionBD;

    public RepositorioCuentaContable(ConexionBD conexionBD)
    {
        _conexionBD = conexionBD;
    }

    public async Task<IEnumerable<CuentaContable>> ListarAsync()
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QueryAsync<CuentaContable>(
                "SP_CuentaContable_Listar",
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudieron obtener las cuentas contables.",
                excepcion);
        }
    }
}
