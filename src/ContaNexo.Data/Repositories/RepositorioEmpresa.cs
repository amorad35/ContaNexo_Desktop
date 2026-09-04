using System.Data;
using ContaNexo.Core.Models;
using ContaNexo.Data.Connections;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ContaNexo.Data.Repositories;

public class RepositorioEmpresa
{
    private readonly ConexionBD _conexionBD;

    public RepositorioEmpresa(ConexionBD conexionBD)
    {
        _conexionBD = conexionBD;
    }

    public async Task<IEnumerable<Empresa>> ListarAsync()
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QueryAsync<Empresa>(
                "SP_Empresa_Listar",
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudieron obtener las empresas.",
                excepcion);
        }
    }
}
