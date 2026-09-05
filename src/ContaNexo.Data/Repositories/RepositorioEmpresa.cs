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

    public async Task<Empresa> CrearAsync(Empresa empresa)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@nombreEmpresa", empresa.NombreEmpresa);
        parametros.Add("@rucEmpresa", empresa.RucEmpresa);
        parametros.Add("@direccionEmpresa", empresa.DireccionEmpresa);
        parametros.Add("@telefonoEmpresa", empresa.TelefonoEmpresa);
        parametros.Add("@correoEmpresa", empresa.CorreoEmpresa);
        parametros.Add("@logoEmpresa", empresa.LogoEmpresa, DbType.Binary);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<Empresa>(
                "SP_Empresa_Crear",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo crear la empresa.",
                excepcion);
        }
    }

    public async Task<Empresa> ActualizarAsync(Empresa empresa)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idEmpresa", empresa.IdEmpresa);
        parametros.Add("@nombreEmpresa", empresa.NombreEmpresa);
        parametros.Add("@rucEmpresa", empresa.RucEmpresa);
        parametros.Add("@direccionEmpresa", empresa.DireccionEmpresa);
        parametros.Add("@telefonoEmpresa", empresa.TelefonoEmpresa);
        parametros.Add("@correoEmpresa", empresa.CorreoEmpresa);
        parametros.Add("@logoEmpresa", empresa.LogoEmpresa, DbType.Binary);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<Empresa>(
                "SP_Empresa_Actualizar",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo actualizar la empresa.",
                excepcion);
        }
    }
}
