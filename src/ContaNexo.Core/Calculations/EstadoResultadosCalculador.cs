using ContaNexo.Core.Models;

namespace ContaNexo.Core.Calculations;

public static class EstadoResultadosCalculador
{
    private const string CodigoElementoIngresos = "4";
    private const string CodigoElementoGastos = "5";
    private const decimal TasaParticipacionTrabajadores = 0.15m;
    private const decimal TasaImpuestoRenta = 0.25m;

    public static EstadoResultadosResumen Calcular(IEnumerable<LibroMayorCuenta> cuentas)
    {
        ArgumentNullException.ThrowIfNull(cuentas);

        List<CuentaCalculada> cuentasNominales = cuentas
            .Where(cuenta => EsIngreso(cuenta) || EsGasto(cuenta))
            .Select(cuenta => new CuentaCalculada(
                cuenta,
                CalcularImporteDirecto(cuenta)))
            .ToList();

        List<EstadoResultadosCuenta> cuentasPrincipales = cuentasNominales
            .GroupBy(cuenta =>
                cuenta.Cuenta.IdCuentaPadre ?? cuenta.Cuenta.IdCuentaContable)
            .Select(CrearCuentaPrincipal)
            .ToList();

        IReadOnlyList<EstadoResultadosGrupo> gruposIngresos = CrearGrupos(
            cuentasPrincipales.Where(cuenta =>
                string.Equals(
                    cuenta.CodigoElemento,
                    CodigoElementoIngresos,
                    StringComparison.Ordinal)));
        IReadOnlyList<EstadoResultadosGrupo> gruposGastos = CrearGrupos(
            cuentasPrincipales.Where(cuenta =>
                string.Equals(
                    cuenta.CodigoElemento,
                    CodigoElementoGastos,
                    StringComparison.Ordinal)));

        decimal totalIngresos = gruposIngresos.Sum(grupo => grupo.TotalGrupo);
        decimal totalGastos = gruposGastos.Sum(grupo => grupo.TotalGrupo);
        decimal resultadoPeriodo = totalIngresos - totalGastos;
        decimal participacionTrabajadores = resultadoPeriodo > 0
            ? RedondearMoneda(resultadoPeriodo * TasaParticipacionTrabajadores)
            : 0;
        decimal resultadoAntesImpuestoRenta =
            resultadoPeriodo - participacionTrabajadores;
        decimal impuestoRenta = resultadoPeriodo > 0
            ? RedondearMoneda(resultadoAntesImpuestoRenta * TasaImpuestoRenta)
            : 0;
        decimal resultadoNeto = resultadoAntesImpuestoRenta - impuestoRenta;

        return new EstadoResultadosResumen
        {
            GruposIngresos = gruposIngresos,
            GruposGastos = gruposGastos,
            TotalIngresos = totalIngresos,
            TotalGastos = totalGastos,
            ResultadoPeriodo = resultadoPeriodo,
            TasaParticipacionTrabajadores = TasaParticipacionTrabajadores,
            ParticipacionTrabajadores = participacionTrabajadores,
            ResultadoAntesImpuestoRenta = resultadoAntesImpuestoRenta,
            TasaImpuestoRenta = TasaImpuestoRenta,
            ImpuestoRenta = impuestoRenta,
            ResultadoNeto = resultadoNeto,
            CantidadSaldosContrarios = cuentasNominales.Count(cuenta =>
                cuenta.Importe < 0)
        };
    }

    private static decimal RedondearMoneda(decimal importe)
    {
        return decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
    }

    private static EstadoResultadosCuenta CrearCuentaPrincipal(
        IGrouping<int, CuentaCalculada> agrupacion)
    {
        List<CuentaCalculada> filas = agrupacion.ToList();
        List<CuentaCalculada> filasPrincipales = filas
            .Where(fila => fila.Cuenta.IdCuentaPadre is null)
            .ToList();
        CuentaCalculada referencia = filasPrincipales.FirstOrDefault()
            ?? filas[0];
        LibroMayorCuenta cuentaReferencia = referencia.Cuenta;
        bool principalSinMovimientoDirecto = filasPrincipales.Count == 0;

        List<EstadoResultadosSubcuenta> subcuentas = filas
            .Where(fila => fila.Cuenta.IdCuentaPadre is not null)
            .OrderBy(fila => fila.Cuenta.OrdenCuenta)
            .ThenBy(fila => fila.Cuenta.CodigoCuenta, StringComparer.Ordinal)
            .Select(fila => new EstadoResultadosSubcuenta
            {
                IdCuentaContable = fila.Cuenta.IdCuentaContable,
                CodigoCuenta = fila.Cuenta.CodigoCuenta,
                NombreCuenta = fila.Cuenta.NombreCuenta,
                OrdenCuenta = fila.Cuenta.OrdenCuenta,
                Importe = fila.Importe,
                TieneSaldoContrario = fila.Importe < 0
            })
            .ToList();

        decimal importeDirecto = filasPrincipales.Sum(fila => fila.Importe);

        return new EstadoResultadosCuenta
        {
            IdCuentaContable = agrupacion.Key,
            CodigoCuenta = principalSinMovimientoDirecto
                ? ObtenerCodigoPadre(cuentaReferencia)
                : cuentaReferencia.CodigoCuenta,
            NombreCuenta = principalSinMovimientoDirecto
                ? ObtenerNombrePadre(cuentaReferencia)
                : cuentaReferencia.NombreCuenta,
            OrdenCuenta = principalSinMovimientoDirecto
                ? ObtenerOrdenPadre(cuentaReferencia)
                : cuentaReferencia.OrdenCuenta,
            IdGrupoContable = cuentaReferencia.IdGrupoContable,
            CodigoGrupo = cuentaReferencia.CodigoGrupo,
            NombreGrupo = cuentaReferencia.NombreGrupo,
            IdElementoContable = cuentaReferencia.IdElementoContable,
            CodigoElemento = cuentaReferencia.CodigoElemento,
            NombreElemento = cuentaReferencia.NombreElemento,
            ImporteDirecto = importeDirecto,
            ImporteConsolidado = importeDirecto
                + subcuentas.Sum(subcuenta => subcuenta.Importe),
            TieneSaldoContrarioDirecto = importeDirecto < 0,
            Subcuentas = subcuentas
        };
    }

    private static IReadOnlyList<EstadoResultadosGrupo> CrearGrupos(
        IEnumerable<EstadoResultadosCuenta> cuentas)
    {
        return cuentas
            .GroupBy(cuenta => cuenta.IdGrupoContable)
            .Select(agrupacion =>
            {
                EstadoResultadosCuenta referencia = agrupacion.First();
                List<EstadoResultadosCuenta> cuentasOrdenadas = agrupacion
                    .OrderBy(cuenta => cuenta.OrdenCuenta)
                    .ThenBy(cuenta => cuenta.CodigoCuenta, StringComparer.Ordinal)
                    .ToList();

                return new EstadoResultadosGrupo
                {
                    IdGrupoContable = referencia.IdGrupoContable,
                    CodigoGrupo = referencia.CodigoGrupo,
                    NombreGrupo = referencia.NombreGrupo,
                    IdElementoContable = referencia.IdElementoContable,
                    CodigoElemento = referencia.CodigoElemento,
                    NombreElemento = referencia.NombreElemento,
                    Cuentas = cuentasOrdenadas,
                    TotalGrupo = cuentasOrdenadas.Sum(cuenta =>
                        cuenta.ImporteConsolidado)
                };
            })
            .OrderBy(grupo => grupo.CodigoGrupo, StringComparer.Ordinal)
            .ToList();
    }

    private static decimal CalcularImporteDirecto(LibroMayorCuenta cuenta)
    {
        return EsIngreso(cuenta)
            ? cuenta.TotalHaber - cuenta.TotalDebe
            : cuenta.TotalDebe - cuenta.TotalHaber;
    }

    private static bool EsIngreso(LibroMayorCuenta cuenta)
    {
        return string.Equals(
            cuenta.CodigoElemento,
            CodigoElementoIngresos,
            StringComparison.Ordinal);
    }

    private static bool EsGasto(LibroMayorCuenta cuenta)
    {
        return string.Equals(
            cuenta.CodigoElemento,
            CodigoElementoGastos,
            StringComparison.Ordinal);
    }

    private static string ObtenerCodigoPadre(LibroMayorCuenta cuenta)
    {
        if (string.IsNullOrWhiteSpace(cuenta.CodigoCuentaPadre))
        {
            throw CrearExcepcionMetadatosPadre(cuenta);
        }

        return cuenta.CodigoCuentaPadre;
    }

    private static string ObtenerNombrePadre(LibroMayorCuenta cuenta)
    {
        if (string.IsNullOrWhiteSpace(cuenta.NombreCuentaPadre))
        {
            throw CrearExcepcionMetadatosPadre(cuenta);
        }

        return cuenta.NombreCuentaPadre;
    }

    private static int ObtenerOrdenPadre(LibroMayorCuenta cuenta)
    {
        if (cuenta.OrdenCuentaPadre is not int ordenCuentaPadre)
        {
            throw CrearExcepcionMetadatosPadre(cuenta);
        }

        return ordenCuentaPadre;
    }

    private static InvalidOperationException CrearExcepcionMetadatosPadre(
        LibroMayorCuenta cuenta)
    {
        return new InvalidOperationException(
            $"La subcuenta {cuenta.CodigoCuenta} no contiene los metadatos de su cuenta principal.");
    }

    private sealed record CuentaCalculada(
        LibroMayorCuenta Cuenta,
        decimal Importe);
}
