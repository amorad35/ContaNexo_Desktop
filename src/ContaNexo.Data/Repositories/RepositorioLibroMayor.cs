using System.Data;
using ContaNexo.Core.Models;
using ContaNexo.Data.Connections;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ContaNexo.Data.Repositories;

public class RepositorioLibroMayor
{
    private readonly ConexionBD _conexionBD;

    public RepositorioLibroMayor(ConexionBD conexionBD)
    {
        _conexionBD = conexionBD;
    }

    public async Task<IReadOnlyList<LibroMayorCuenta>> ObtenerPorPeriodoAsync(
        int idPeriodoContable)
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();
            using SqlMapper.GridReader resultados =
                await conexion.QueryMultipleAsync(
                    "dbo.SP_LibroMayor_ObtenerPorPeriodo",
                    new { idPeriodoContable },
                    commandType: CommandType.StoredProcedure);

            List<LibroMayorCuenta> cuentas =
                (await resultados.ReadAsync<LibroMayorCuenta>()).ToList();
            IEnumerable<LibroMayorMovimiento> movimientos =
                await resultados.ReadAsync<LibroMayorMovimiento>();
            ILookup<int, LibroMayorMovimiento> movimientosPorCuenta =
                movimientos.ToLookup(movimiento => movimiento.IdCuentaContable);

            foreach (LibroMayorCuenta cuenta in cuentas)
            {
                cuenta.Movimientos = movimientosPorCuenta[cuenta.IdCuentaContable].ToList();
            }

            return cuentas;
        }
        catch (SqlException excepcion) when (excepcion.Number == 52701)
        {
            throw new InvalidOperationException(
                "El período contable indicado no existe.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo obtener el Libro Mayor del período.",
                excepcion);
        }
    }
}
