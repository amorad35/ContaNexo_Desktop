using System.Data;
using ContaNexo.Core.Models;
using ContaNexo.Data.Connections;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ContaNexo.Data.Repositories;

public class RepositorioPeriodoContable
{
    private readonly ConexionBD _conexionBD;

    public RepositorioPeriodoContable(ConexionBD conexionBD)
    {
        _conexionBD = conexionBD;
    }

    public async Task<IEnumerable<PeriodoContableListado>> ListarAsync(
        int? idEmpresa = null,
        string? estadoPeriodo = null)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idEmpresa", idEmpresa);
        parametros.Add("@estadoPeriodo", estadoPeriodo);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QueryAsync<PeriodoContableListado>(
                "SP_PeriodoContable_Listar",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudieron obtener los períodos contables.",
                excepcion);
        }
    }

    public async Task<PeriodoContableDetalle?> ObtenerPorIdAsync(int idPeriodoContable)
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleOrDefaultAsync<PeriodoContableDetalle>(
                "SP_PeriodoContable_ObtenerPorId",
                new { idPeriodoContable },
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 53101)
        {
            return null;
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo obtener el período contable.",
                excepcion);
        }
    }

    public async Task<PeriodoContable> CrearAsync(PeriodoContable periodo)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idEmpresa", periodo.IdEmpresa);
        parametros.Add("@nombrePeriodo", periodo.NombrePeriodo);
        parametros.Add("@fechaInicioPeriodo", periodo.FechaInicioPeriodo);
        parametros.Add("@fechaFinPeriodo", periodo.FechaFinPeriodo);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<PeriodoContable>(
                "SP_PeriodoContable_Crear",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 53206)
        {
            throw new InvalidOperationException(
                "El rango de fechas se superpone con otro período de la empresa.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo crear el período contable.",
                excepcion);
        }
    }

    public async Task<PeriodoContable> ActualizarAsync(PeriodoContable periodo)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idPeriodoContable", periodo.IdPeriodoContable);
        parametros.Add("@nombrePeriodo", periodo.NombrePeriodo);
        parametros.Add("@fechaInicioPeriodo", periodo.FechaInicioPeriodo);
        parametros.Add("@fechaFinPeriodo", periodo.FechaFinPeriodo);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<PeriodoContable>(
                "SP_PeriodoContable_Actualizar",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 53308)
        {
            throw new InvalidOperationException(
                "El nuevo rango de fechas se superpone con otro período de la empresa.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo actualizar el período contable.",
                excepcion);
        }
    }

    public async Task<PeriodoContableCierre> CerrarAsync(int idPeriodoContable)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idPeriodoContable", idPeriodoContable);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<PeriodoContableCierre>(
                "SP_PeriodoContable_Cerrar",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 53401)
        {
            throw new InvalidOperationException(
                "El período contable que intentas cerrar ya no existe.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 53402)
        {
            throw new InvalidOperationException(
                "El período contable ya está cerrado.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo cerrar el período contable.",
                excepcion);
        }
    }
}
