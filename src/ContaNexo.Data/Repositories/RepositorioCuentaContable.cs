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

    public async Task<IEnumerable<CuentaContableListado>> ListarAsync()
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QueryAsync<CuentaContableListado>(
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

    public async Task<IEnumerable<CuentaMovimiento>> ListarMovimientoAsync()
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QueryAsync<CuentaMovimiento>(
                "SP_CuentaContable_ListarMovimiento",
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudieron obtener las cuentas habilitadas para movimiento.",
                excepcion);
        }
    }

    public async Task<CuentaContableDetalle?> ObtenerPorIdAsync(int idCuentaContable)
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleOrDefaultAsync<CuentaContableDetalle>(
                "SP_CuentaContable_ObtenerPorId",
                new { idCuentaContable },
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52001)
        {
            return null;
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo obtener la cuenta contable.",
                excepcion);
        }
    }

    public async Task<int> CrearAsync(CuentaContable cuenta)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idGrupoContable", cuenta.IdGrupoContable);
        parametros.Add("@idCuentaPadre", cuenta.IdCuentaPadre);
        parametros.Add("@codigoCuenta", cuenta.CodigoCuenta);
        parametros.Add("@nombreCuenta", cuenta.NombreCuenta);
        parametros.Add("@naturalezaCuenta", cuenta.NaturalezaCuenta);
        parametros.Add("@permiteMovimientoCuenta", cuenta.PermiteMovimientoCuenta);
        parametros.Add("@estadoCuenta", cuenta.EstadoCuenta);
        parametros.Add("@ordenCuenta", cuenta.OrdenCuenta);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<int>(
                "SP_CuentaContable_Crear",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52101)
        {
            throw new InvalidOperationException(
                "El código de la cuenta es obligatorio.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52102)
        {
            throw new InvalidOperationException(
                "El nombre de la cuenta es obligatorio.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52103)
        {
            throw new InvalidOperationException(
                "El orden de la cuenta debe ser un entero mayor que cero.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52104)
        {
            throw new InvalidOperationException(
                "La naturaleza debe ser Deudora o Acreedora.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number is 52105 or 52106)
        {
            throw new InvalidOperationException(
                "Los datos de estado o movimiento de la cuenta no son válidos.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52107)
        {
            throw new InvalidOperationException(
                "El grupo contable seleccionado no existe o está inactivo.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52108)
        {
            throw new InvalidOperationException(
                "Ya existe una cuenta con el código indicado.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52109)
        {
            throw new InvalidOperationException(
                "La cuenta padre seleccionada ya no existe.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52110)
        {
            throw new InvalidOperationException(
                "La cuenta padre debe pertenecer al mismo grupo contable.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo crear la cuenta contable.",
                excepcion);
        }
    }

    public async Task<int> ActualizarAsync(CuentaContable cuenta)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idCuentaContable", cuenta.IdCuentaContable);
        parametros.Add("@idGrupoContable", cuenta.IdGrupoContable);
        parametros.Add("@idCuentaPadre", cuenta.IdCuentaPadre);
        parametros.Add("@codigoCuenta", cuenta.CodigoCuenta);
        parametros.Add("@nombreCuenta", cuenta.NombreCuenta);
        parametros.Add("@naturalezaCuenta", cuenta.NaturalezaCuenta);
        parametros.Add("@ordenCuenta", cuenta.OrdenCuenta);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<int>(
                "SP_CuentaContable_Actualizar",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52201)
        {
            throw new InvalidOperationException(
                "La cuenta que intentas actualizar ya no existe.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52202)
        {
            throw new InvalidOperationException(
                "El código de la cuenta es obligatorio.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52203)
        {
            throw new InvalidOperationException(
                "El nombre de la cuenta es obligatorio.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52204)
        {
            throw new InvalidOperationException(
                "El orden actual de la cuenta no es válido.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52205)
        {
            throw new InvalidOperationException(
                "La naturaleza debe ser Deudora o Acreedora.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52206)
        {
            throw new InvalidOperationException(
                "El grupo contable seleccionado no existe o está inactivo.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52207)
        {
            throw new InvalidOperationException(
                "Ya existe otra cuenta con el código indicado.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52208)
        {
            throw new InvalidOperationException(
                "Una cuenta no puede ser su propia cuenta padre.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52209)
        {
            throw new InvalidOperationException(
                "La cuenta padre seleccionada ya no existe.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52210)
        {
            throw new InvalidOperationException(
                "La cuenta debe pertenecer al mismo grupo que su padre.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52211)
        {
            throw new InvalidOperationException(
                "La cuenta padre seleccionada produciría un ciclo jerárquico.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52212)
        {
            throw new InvalidOperationException(
                "No se puede cambiar el grupo porque una o más cuentas hijas quedarían en otro grupo.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo actualizar la cuenta contable.",
                excepcion);
        }
    }

    public async Task ConfigurarMovimientoAsync(
        int idCuentaContable,
        bool permiteMovimiento)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idCuentaContable", idCuentaContable);
        parametros.Add("@permiteMovimientoCuenta", permiteMovimiento);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            await conexion.ExecuteAsync(
                "SP_CuentaContable_ConfigurarMovimiento",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52301)
        {
            throw new InvalidOperationException(
                "El valor indicado para configurar el movimiento no es válido.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52302)
        {
            throw new InvalidOperationException(
                "La cuenta cuya configuración de movimiento intentas cambiar ya no existe.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo configurar el movimiento de la cuenta contable.",
                excepcion);
        }
    }

    public async Task CambiarEstadoAsync(int idCuentaContable, bool estado)
    {
        var parametros = new DynamicParameters();
        parametros.Add("@idCuentaContable", idCuentaContable);
        parametros.Add("@estadoCuenta", estado);

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            await conexion.ExecuteAsync(
                "SP_CuentaContable_CambiarEstado",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52401)
        {
            throw new InvalidOperationException(
                "El estado indicado para la cuenta no es válido.",
                excepcion);
        }
        catch (SqlException excepcion) when (excepcion.Number == 52402)
        {
            throw new InvalidOperationException(
                "La cuenta cuyo estado intentas cambiar ya no existe.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo cambiar el estado de la cuenta contable.",
                excepcion);
        }
    }
}
