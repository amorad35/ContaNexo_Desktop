using ContaNexo.Core.Models;

namespace ContaNexo.Core.Calculations;

public static class BalanceGeneralCalculador
{
    private const string CodigoElementoActivo = "1";
    private const string CodigoElementoPasivo = "2";
    private const string CodigoElementoPatrimonio = "3";
    private const string CodigoGrupoActivoCorriente = "11";
    private const string CodigoGrupoActivoNoCorriente = "12";
    private const string CodigoGrupoPasivoCorriente = "21";
    private const string CodigoGrupoPasivoNoCorriente = "22";
    private const string CodigoGrupoCapitalContable = "31";
    private const string CodigoGrupoResultadosReservas = "32";

    public static BalanceGeneralResumen Calcular(
        IEnumerable<LibroMayorCuenta> cuentas)
    {
        ArgumentNullException.ThrowIfNull(cuentas);

        List<LibroMayorCuenta> listaCuentas = cuentas.ToList();
        EstadoResultadosResumen resultado =
            EstadoResultadosCalculador.Calcular(listaCuentas);
        List<LibroMayorCuenta> cuentasBalance = listaCuentas
            .Where(EsCuentaBalanceGeneral)
            .ToList();
        BalanceGeneralGrupo activoCorriente = CrearGrupo(
            listaCuentas,
            CodigoElementoActivo,
            CodigoGrupoActivoCorriente,
            "Activo Corriente");
        BalanceGeneralGrupo activoNoCorriente = CrearGrupo(
            listaCuentas,
            CodigoElementoActivo,
            CodigoGrupoActivoNoCorriente,
            "Activo No Corriente");
        BalanceGeneralGrupo pasivoCorriente = CrearGrupo(
            listaCuentas,
            CodigoElementoPasivo,
            CodigoGrupoPasivoCorriente,
            "Pasivo Corriente");
        BalanceGeneralGrupo pasivoNoCorriente = CrearGrupo(
            listaCuentas,
            CodigoElementoPasivo,
            CodigoGrupoPasivoNoCorriente,
            "Pasivo No Corriente");
        BalanceGeneralGrupo capitalContable = CrearGrupo(
            listaCuentas,
            CodigoElementoPatrimonio,
            CodigoGrupoCapitalContable,
            "Capital Contable");
        BalanceGeneralGrupo resultadosReservas = CrearGrupo(
            listaCuentas,
            CodigoElementoPatrimonio,
            CodigoGrupoResultadosReservas,
            "Resultados y Reservas");

        decimal totalPasivoCorriente = pasivoCorriente.TotalGrupo
            + resultado.ParticipacionTrabajadores
            + resultado.ImpuestoRenta;
        decimal totalActivo = activoCorriente.TotalGrupo
            + activoNoCorriente.TotalGrupo;
        decimal totalPasivo = totalPasivoCorriente
            + pasivoNoCorriente.TotalGrupo;
        decimal totalPatrimonio = capitalContable.TotalGrupo
            + resultadosReservas.TotalGrupo
            + resultado.ResultadoNeto;
        decimal totalPasivoPatrimonio = totalPasivo + totalPatrimonio;
        decimal diferencia = totalActivo - totalPasivoPatrimonio;

        return new BalanceGeneralResumen
        {
            ActivoCorriente = activoCorriente,
            ActivoNoCorriente = activoNoCorriente,
            PasivoCorriente = pasivoCorriente,
            PasivoNoCorriente = pasivoNoCorriente,
            CapitalContable = capitalContable,
            ResultadosReservas = resultadosReservas,
            TasaParticipacionTrabajadores =
                resultado.TasaParticipacionTrabajadores,
            ParticipacionTrabajadores = resultado.ParticipacionTrabajadores,
            TasaImpuestoRenta = resultado.TasaImpuestoRenta,
            ImpuestoRenta = resultado.ImpuestoRenta,
            ResultadoNeto = resultado.ResultadoNeto,
            TotalPasivoCorriente = totalPasivoCorriente,
            TotalActivo = totalActivo,
            TotalPasivo = totalPasivo,
            TotalPatrimonio = totalPatrimonio,
            TotalPasivoPatrimonio = totalPasivoPatrimonio,
            Diferencia = diferencia,
            CantidadSaldosContrarios = cuentasBalance.Count(
                TieneSaldoContrario),
            CantidadCuentasResultadoEjercicioRegistradas = listaCuentas.Count(
                TieneRegistroResultadoEjercicio)
        };
    }

    private static BalanceGeneralGrupo CrearGrupo(
        IEnumerable<LibroMayorCuenta> cuentas,
        string codigoElemento,
        string codigoGrupo,
        string nombreGrupo)
    {
        List<BalanceGeneralCuenta> cuentasGrupo = cuentas
            .Where(cuenta =>
                string.Equals(
                    cuenta.CodigoElemento,
                    codigoElemento,
                    StringComparison.Ordinal)
                && string.Equals(
                    cuenta.CodigoGrupo,
                    codigoGrupo,
                    StringComparison.Ordinal))
            .Select(CrearCuentaCalculada)
            .GroupBy(cuenta =>
                cuenta.Cuenta.IdCuentaPadre
                ?? cuenta.Cuenta.IdCuentaContable)
            .Select(CrearCuentaPrincipal)
            .Where(cuenta => cuenta.ImporteConsolidado != 0m)
            .OrderBy(cuenta => cuenta.OrdenCuenta)
            .ThenBy(cuenta => cuenta.CodigoCuenta, StringComparer.Ordinal)
            .ToList();

        return new BalanceGeneralGrupo
        {
            CodigoElemento = codigoElemento,
            CodigoGrupo = codigoGrupo,
            NombreGrupo = nombreGrupo,
            Cuentas = cuentasGrupo,
            TotalGrupo = cuentasGrupo.Sum(cuenta => cuenta.ImpactoEnTotal)
        };
    }

    private static BalanceGeneralCuenta CrearCuentaPrincipal(
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

        List<BalanceGeneralSubcuenta> subcuentas = filas
            .Where(fila => fila.Cuenta.IdCuentaPadre is not null)
            .OrderBy(fila => fila.Cuenta.OrdenCuenta)
            .ThenBy(
                fila => fila.Cuenta.CodigoCuenta,
                StringComparer.Ordinal)
            .Select(fila => new BalanceGeneralSubcuenta
            {
                IdCuentaContable = fila.Cuenta.IdCuentaContable,
                CodigoCuenta = fila.Cuenta.CodigoCuenta,
                NombreCuenta = fila.Cuenta.NombreCuenta,
                NaturalezaCuenta = fila.Cuenta.NaturalezaCuenta,
                OrdenCuenta = fila.Cuenta.OrdenCuenta,
                Importe = fila.Importe,
                ImpactoEnTotal = fila.Impacto,
                TieneSaldoContrario = fila.TieneSaldoContrario
            })
            .ToList();

        decimal importeDirecto = filasPrincipales.Sum(fila => fila.Importe);
        decimal impactoDirecto = filasPrincipales.Sum(fila => fila.Impacto);
        bool esReductora = filas.Any(fila => fila.EsReductora);

        return new BalanceGeneralCuenta
        {
            IdCuentaContable = agrupacion.Key,
            CodigoCuenta = principalSinMovimientoDirecto
                ? ObtenerCodigoPadre(cuentaReferencia)
                : cuentaReferencia.CodigoCuenta,
            NombreCuenta = principalSinMovimientoDirecto
                ? ObtenerNombrePadre(cuentaReferencia)
                : cuentaReferencia.NombreCuenta,
            NaturalezaCuenta = cuentaReferencia.NaturalezaCuenta,
            OrdenCuenta = principalSinMovimientoDirecto
                ? ObtenerOrdenPadre(cuentaReferencia)
                : cuentaReferencia.OrdenCuenta,
            CodigoGrupo = cuentaReferencia.CodigoGrupo,
            CodigoElemento = cuentaReferencia.CodigoElemento,
            ImporteDirecto = importeDirecto,
            ImporteConsolidado = importeDirecto
                + subcuentas.Sum(subcuenta => subcuenta.Importe),
            ImpactoEnTotal = impactoDirecto
                + subcuentas.Sum(subcuenta => subcuenta.ImpactoEnTotal),
            EsReductora = esReductora,
            TieneSaldoContrarioDirecto = filasPrincipales.Any(fila =>
                fila.TieneSaldoContrario),
            Subcuentas = subcuentas
        };
    }

    private static CuentaCalculada CrearCuentaCalculada(
        LibroMayorCuenta cuenta)
    {
        bool esReductora = EsReductoraActivo(cuenta);
        decimal importe = esReductora
            ? cuenta.TotalHaber - cuenta.TotalDebe
            : CalcularImporteEstado(cuenta);
        decimal impacto = esReductora ? -importe : importe;

        return new CuentaCalculada(
            cuenta,
            importe,
            impacto,
            TieneSaldoContrario(cuenta),
            esReductora);
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

    private static decimal CalcularImporteEstado(LibroMayorCuenta cuenta)
    {
        return string.Equals(
            cuenta.CodigoElemento,
            CodigoElementoActivo,
            StringComparison.Ordinal)
                ? cuenta.TotalDebe - cuenta.TotalHaber
                : cuenta.TotalHaber - cuenta.TotalDebe;
    }

    private static bool EsCuentaBalanceGeneral(LibroMayorCuenta cuenta)
    {
        return cuenta.CodigoElemento switch
        {
            CodigoElementoActivo =>
                cuenta.CodigoGrupo is CodigoGrupoActivoCorriente
                    or CodigoGrupoActivoNoCorriente,
            CodigoElementoPasivo =>
                cuenta.CodigoGrupo is CodigoGrupoPasivoCorriente
                    or CodigoGrupoPasivoNoCorriente,
            CodigoElementoPatrimonio =>
                cuenta.CodigoGrupo is CodigoGrupoCapitalContable
                    or CodigoGrupoResultadosReservas,
            _ => false
        };
    }

    private static bool EsReductoraActivo(LibroMayorCuenta cuenta)
    {
        return string.Equals(
                cuenta.CodigoGrupo,
                CodigoGrupoActivoNoCorriente,
                StringComparison.Ordinal)
            && (cuenta.CodigoCuenta is "1202" or "1204"
                || cuenta.CodigoCuentaPadre is "1202" or "1204");
    }

    private static bool TieneSaldoContrario(LibroMayorCuenta cuenta)
    {
        return cuenta.NaturalezaCuenta switch
        {
            "Deudora" => cuenta.TotalHaber > cuenta.TotalDebe,
            "Acreedora" => cuenta.TotalDebe > cuenta.TotalHaber,
            _ => false
        };
    }

    private static bool TieneRegistroResultadoEjercicio(
        LibroMayorCuenta cuenta)
    {
        return cuenta.CodigoCuenta is "3203" or "3204"
            && (cuenta.Movimientos.Count > 0
                || cuenta.TotalDebe != 0m
                || cuenta.TotalHaber != 0m);
    }

    private sealed record CuentaCalculada(
        LibroMayorCuenta Cuenta,
        decimal Importe,
        decimal Impacto,
        bool TieneSaldoContrario,
        bool EsReductora);
}
