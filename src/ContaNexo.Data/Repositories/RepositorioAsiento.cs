using System.Data;
using ContaNexo.Core.Models;
using ContaNexo.Data.Connections;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ContaNexo.Data.Repositories;

public class RepositorioAsiento
{
    private const string TipoDetalleAsientoCreacion = "dbo.DetalleAsientoCreacionTipo";

    private readonly ConexionBD _conexionBD;

    public RepositorioAsiento(ConexionBD conexionBD)
    {
        _conexionBD = conexionBD;
    }

    public async Task<AsientoCreacionResultado> CrearAsync(AsientoCreacion asiento)
    {
        ArgumentNullException.ThrowIfNull(asiento);
        ArgumentNullException.ThrowIfNull(asiento.Detalles);

        var parametros = new DynamicParameters();
        parametros.Add("@idPeriodoContable", asiento.IdPeriodoContable, DbType.Int32);
        parametros.Add("@fechaAsiento", asiento.FechaAsiento.Date, DbType.Date);
        parametros.Add("@tipoAsiento", asiento.TipoAsiento, DbType.AnsiString, size: 10);
        parametros.Add("@descripcionAsiento", asiento.DescripcionAsiento, DbType.String, size: 500);
        parametros.Add("@detalles", CrearTablaDetalles(asiento.Detalles).AsTableValuedParameter(TipoDetalleAsientoCreacion));

        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            return await conexion.QuerySingleAsync<AsientoCreacionResultado>(
                "dbo.SP_Asiento_Crear",
                parametros,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException excepcion) when (excepcion.Number is >= 52501 and <= 52516)
        {
            throw new InvalidOperationException(ObtenerMensajeValidacion(excepcion.Number), excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo crear el asiento contable.",
                excepcion);
        }
    }

    public async Task<IReadOnlyList<AsientoListado>> ListarPorPeriodoAsync(
        int idPeriodoContable)
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();

            IEnumerable<AsientoListado> asientos =
                await conexion.QueryAsync<AsientoListado>(
                    "dbo.SP_Asiento_ListarPorPeriodo",
                    new { idPeriodoContable },
                    commandType: CommandType.StoredProcedure);

            return asientos.ToList();
        }
        catch (SqlException excepcion) when (excepcion.Number == 52601)
        {
            throw new InvalidOperationException(
                "El período contable indicado no existe.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudieron obtener los asientos del período.",
                excepcion);
        }
    }

    public async Task<AsientoDetalleConsulta> ObtenerDetalleAsync(
        int idPeriodoContable,
        int idAsiento)
    {
        try
        {
            await using SqlConnection conexion = _conexionBD.CrearConexion();
            using SqlMapper.GridReader resultados =
                await conexion.QueryMultipleAsync(
                    "dbo.SP_Asiento_ObtenerDetalle",
                    new { idPeriodoContable, idAsiento },
                    commandType: CommandType.StoredProcedure);

            AsientoDetalleConsulta asiento =
                await resultados.ReadSingleAsync<AsientoDetalleConsulta>();
            IEnumerable<DetalleAsientoConsulta> movimientos =
                await resultados.ReadAsync<DetalleAsientoConsulta>();

            asiento.Movimientos = movimientos.ToList();
            return asiento;
        }
        catch (SqlException excepcion) when (excepcion.Number == 52602)
        {
            throw new InvalidOperationException(
                "El asiento indicado no existe en el período contable seleccionado.",
                excepcion);
        }
        catch (SqlException excepcion)
        {
            throw new InvalidOperationException(
                "No se pudo obtener el detalle del asiento.",
                excepcion);
        }
    }

    private static DataTable CrearTablaDetalles(IEnumerable<DetalleAsientoCreacion> detalles)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("idCuentaContable", typeof(int));
        tabla.Columns.Add("debeDetalle", typeof(decimal));
        tabla.Columns.Add("haberDetalle", typeof(decimal));
        tabla.Columns.Add("ordenDetalle", typeof(short));

        foreach (DetalleAsientoCreacion detalle in detalles)
        {
            tabla.Rows.Add(
                detalle.IdCuentaContable,
                detalle.DebeDetalle,
                detalle.HaberDetalle,
                detalle.OrdenDetalle);
        }

        return tabla;
    }

    private static string ObtenerMensajeValidacion(int numeroError)
    {
        return numeroError switch
        {
            52501 => "El período contable indicado no existe.",
            52502 => "Solo se pueden registrar asientos en un período abierto.",
            52503 => "La fecha del asiento es obligatoria.",
            52504 => "La fecha del asiento debe estar dentro del período contable.",
            52505 => "El tipo de asiento debe ser Normal o Ajuste.",
            52506 => "El asiento debe contener al menos un detalle.",
            52507 => "Cada detalle debe tener un valor positivo únicamente en Debe o únicamente en Haber.",
            52508 => "El orden de los detalles debe comenzar en 1 y ser siempre mayor que cero.",
            52509 => "El orden de los detalles no puede repetirse dentro del asiento.",
            52510 => "Uno o más detalles contienen una cuenta contable inexistente.",
            52511 => "Uno o más detalles contienen una cuenta contable inactiva.",
            52512 => "Uno o más detalles contienen una cuenta que no permite movimiento.",
            52513 => "El total Debe debe ser igual al total Haber.",
            52514 => "El total del asiento debe ser mayor que cero.",
            52515 => "No se puede generar otro número de asiento para el período.",
            52516 => "No se pudo asignar un número de asiento único. Intente nuevamente.",
            _ => "No se pudo crear el asiento contable."
        };
    }
}
