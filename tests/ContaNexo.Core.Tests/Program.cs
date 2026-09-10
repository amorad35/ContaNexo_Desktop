using ContaNexo.Core.Calculations;
using ContaNexo.Core.Models;

namespace ContaNexo.Core.Tests;

internal static class Program
{
    private static readonly List<(string Nombre, Action Prueba)> Pruebas =
    [
        (
            nameof(Cuenta_principal_sin_hijas_se_clasifica_por_codigos),
            Cuenta_principal_sin_hijas_se_clasifica_por_codigos),
        (
            nameof(Principal_suma_movimiento_directo_y_subcuentas_una_sola_vez),
            Principal_suma_movimiento_directo_y_subcuentas_una_sola_vez),
        (
            nameof(Principal_se_reconstruye_si_solo_la_hija_tiene_movimiento),
            Principal_se_reconstruye_si_solo_la_hija_tiene_movimiento),
        (
            nameof(Cuenta_con_importe_consolidado_cero_no_se_muestra),
            Cuenta_con_importe_consolidado_cero_no_se_muestra),
        (
            nameof(Reductoras_1202_y_1204_restan_activo_con_saldo_acreedor_normal),
            Reductoras_1202_y_1204_restan_activo_con_saldo_acreedor_normal),
        (
            nameof(Subcuenta_de_1202_conserva_tratamiento_reductor),
            Subcuenta_de_1202_conserva_tratamiento_reductor),
        (
            nameof(Saldo_deudor_normal_3204_reduce_patrimonio_sin_saldo_contrario),
            Saldo_deudor_normal_3204_reduce_patrimonio_sin_saldo_contrario),
        (
            nameof(Cuenta_deudora_con_saldo_acreedor_genera_diagnostico),
            Cuenta_deudora_con_saldo_acreedor_genera_diagnostico),
        (
            nameof(Cuenta_acreedora_con_saldo_deudor_genera_diagnostico),
            Cuenta_acreedora_con_saldo_deudor_genera_diagnostico),
        (
            nameof(Reductora_acreedora_con_saldo_deudor_conserva_signo_y_diagnostica),
            Reductora_acreedora_con_saldo_deudor_conserva_signo_y_diagnostica),
        (
            nameof(Utilidad_incorpora_obligaciones_y_resultado_neto_en_balance_cuadrado),
            Utilidad_incorpora_obligaciones_y_resultado_neto_en_balance_cuadrado),
        (
            nameof(Perdida_no_genera_obligaciones_y_reduce_patrimonio),
            Perdida_no_genera_obligaciones_y_reduce_patrimonio),
        (
            nameof(Resultado_cero_no_genera_obligaciones_y_balance_vacio_cuadra),
            Resultado_cero_no_genera_obligaciones_y_balance_vacio_cuadra),
        (
            nameof(Diferencia_conserva_el_descuadre_sin_ajustes),
            Diferencia_conserva_el_descuadre_sin_ajustes),
        (
            nameof(Calcular_rechaza_coleccion_nula),
            Calcular_rechaza_coleccion_nula),
        (
            nameof(Subcuenta_sin_metadatos_de_padre_genera_error_explicito),
            Subcuenta_sin_metadatos_de_padre_genera_error_explicito)
    ];

    private static int Main()
    {
        int aprobadas = 0;
        var fallos = new List<string>();

        foreach ((string nombre, Action prueba) in Pruebas)
        {
            try
            {
                prueba();
                aprobadas++;
                Console.WriteLine($"PASS {nombre}");
            }
            catch (Exception excepcion)
            {
                fallos.Add($"FAIL {nombre}: {excepcion.Message}");
            }
        }

        foreach (string fallo in fallos)
        {
            Console.Error.WriteLine(fallo);
        }

        Console.WriteLine($"{aprobadas} passed, {fallos.Count} failed");
        return fallos.Count == 0 ? 0 : 1;
    }

    private static void Cuenta_principal_sin_hijas_se_clasifica_por_codigos()
    {
        var cuenta = new LibroMayorCuenta
        {
            IdCuentaContable = 1,
            CodigoCuenta = "1101",
            NombreCuenta = "Caja",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 1,
            CodigoElemento = "1",
            CodigoGrupo = "11",
            NombreGrupo = "Nombre deliberadamente irrelevante",
            TotalDebe = 1250m,
            TotalHaber = 250m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([cuenta]);

        AssertEqual(1, resumen.ActivoCorriente.Cuentas.Count);
        AssertEqual("1101", resumen.ActivoCorriente.Cuentas[0].CodigoCuenta);
        AssertEqual(1000m, resumen.ActivoCorriente.Cuentas[0].ImporteConsolidado);
        AssertEqual(1000m, resumen.ActivoCorriente.TotalGrupo);
        AssertEqual(0, resumen.PasivoCorriente.Cuentas.Count);
        AssertEqual("Activo Corriente", resumen.ActivoCorriente.NombreGrupo);
    }

    private static void Principal_suma_movimiento_directo_y_subcuentas_una_sola_vez()
    {
        var principal = new LibroMayorCuenta
        {
            IdCuentaContable = 10,
            CodigoCuenta = "1201",
            NombreCuenta = "Propiedad, Planta y Equipo",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 18,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            TotalDebe = 500m,
            TotalHaber = 0m
        };
        var terrenos = new LibroMayorCuenta
        {
            IdCuentaContable = 11,
            CodigoCuenta = "120101",
            NombreCuenta = "Terrenos",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 19,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            IdCuentaPadre = 10,
            CodigoCuentaPadre = "1201",
            NombreCuentaPadre = "Propiedad, Planta y Equipo",
            OrdenCuentaPadre = 18,
            TotalDebe = 5000m,
            TotalHaber = 0m
        };
        var edificios = new LibroMayorCuenta
        {
            IdCuentaContable = 12,
            CodigoCuenta = "120102",
            NombreCuenta = "Edificios",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 20,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            IdCuentaPadre = 10,
            CodigoCuentaPadre = "1201",
            NombreCuentaPadre = "Propiedad, Planta y Equipo",
            OrdenCuentaPadre = 18,
            TotalDebe = 10000m,
            TotalHaber = 0m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [principal, terrenos, edificios]);

        AssertEqual(1, resumen.ActivoNoCorriente.Cuentas.Count);
        BalanceGeneralCuenta cuenta = resumen.ActivoNoCorriente.Cuentas[0];
        AssertEqual(500m, cuenta.ImporteDirecto);
        AssertEqual(15500m, cuenta.ImporteConsolidado);
        AssertEqual(15500m, cuenta.ImpactoEnTotal);
        AssertEqual(2, cuenta.Subcuentas.Count);
        AssertEqual("120101", cuenta.Subcuentas[0].CodigoCuenta);
        AssertEqual("120102", cuenta.Subcuentas[1].CodigoCuenta);
    }

    private static void Principal_se_reconstruye_si_solo_la_hija_tiene_movimiento()
    {
        var terrenos = new LibroMayorCuenta
        {
            IdCuentaContable = 11,
            CodigoCuenta = "120101",
            NombreCuenta = "Terrenos",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 19,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            IdCuentaPadre = 10,
            CodigoCuentaPadre = "1201",
            NombreCuentaPadre = "Propiedad, Planta y Equipo",
            OrdenCuentaPadre = 18,
            TotalDebe = 5000m,
            TotalHaber = 0m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([terrenos]);

        AssertEqual(1, resumen.ActivoNoCorriente.Cuentas.Count);
        BalanceGeneralCuenta cuenta = resumen.ActivoNoCorriente.Cuentas[0];
        AssertEqual("1201", cuenta.CodigoCuenta);
        AssertEqual("Propiedad, Planta y Equipo", cuenta.NombreCuenta);
        AssertEqual(18, cuenta.OrdenCuenta);
        AssertEqual(0m, cuenta.ImporteDirecto);
        AssertEqual(5000m, cuenta.ImporteConsolidado);
        AssertEqual(1, cuenta.Subcuentas.Count);
    }

    private static void Cuenta_con_importe_consolidado_cero_no_se_muestra()
    {
        var bancos = new LibroMayorCuenta
        {
            IdCuentaContable = 2,
            CodigoCuenta = "1102",
            NombreCuenta = "Bancos",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 2,
            CodigoElemento = "1",
            CodigoGrupo = "11",
            NombreGrupo = "Activo Corriente",
            TotalDebe = 100m,
            TotalHaber = 100m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([bancos]);

        AssertEqual(0, resumen.ActivoCorriente.Cuentas.Count);
        AssertEqual(0m, resumen.ActivoCorriente.TotalGrupo);
    }

    private static void Reductoras_1202_y_1204_restan_activo_con_saldo_acreedor_normal()
    {
        var propiedadPlantaEquipo = new LibroMayorCuenta
        {
            IdCuentaContable = 20,
            CodigoCuenta = "1201",
            NombreCuenta = "Propiedad, Planta y Equipo",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 18,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            TotalDebe = 2400m,
            TotalHaber = 0m
        };
        var depreciacion = new LibroMayorCuenta
        {
            IdCuentaContable = 21,
            CodigoCuenta = "1202",
            NombreCuenta = "Depreciación Acumulada",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 24,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            TotalDebe = 0m,
            TotalHaber = 533m
        };
        var intangibles = new LibroMayorCuenta
        {
            IdCuentaContable = 22,
            CodigoCuenta = "1203",
            NombreCuenta = "Intangibles",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 28,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            TotalDebe = 1500m,
            TotalHaber = 0m
        };
        var amortizacion = new LibroMayorCuenta
        {
            IdCuentaContable = 23,
            CodigoCuenta = "1204",
            NombreCuenta = "Amortización Acumulada",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 32,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            TotalDebe = 0m,
            TotalHaber = 46m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [propiedadPlantaEquipo, depreciacion, intangibles, amortizacion]);

        BalanceGeneralCuenta cuentaDepreciacion = resumen
            .ActivoNoCorriente
            .Cuentas
            .Single(cuenta => cuenta.CodigoCuenta == "1202");
        BalanceGeneralCuenta cuentaAmortizacion = resumen
            .ActivoNoCorriente
            .Cuentas
            .Single(cuenta => cuenta.CodigoCuenta == "1204");

        AssertEqual(533m, cuentaDepreciacion.ImporteConsolidado);
        AssertEqual(-533m, cuentaDepreciacion.ImpactoEnTotal);
        AssertEqual("(-) Depreciación Acumulada", cuentaDepreciacion.NombrePresentacion);
        AssertFalse(cuentaDepreciacion.TieneSaldoContrario);
        AssertEqual(46m, cuentaAmortizacion.ImporteConsolidado);
        AssertEqual(-46m, cuentaAmortizacion.ImpactoEnTotal);
        AssertFalse(cuentaAmortizacion.TieneSaldoContrario);
        AssertEqual(3321m, resumen.ActivoNoCorriente.TotalGrupo);
        AssertEqual(0, resumen.CantidadSaldosContrarios);
    }

    private static void Subcuenta_de_1202_conserva_tratamiento_reductor()
    {
        var depreciacionEdificaciones = new LibroMayorCuenta
        {
            IdCuentaContable = 25,
            CodigoCuenta = "120201",
            NombreCuenta = "Deprec. Acum. Edificaciones",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 25,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            IdCuentaPadre = 21,
            CodigoCuentaPadre = "1202",
            NombreCuentaPadre = "Depreciación Acumulada",
            OrdenCuentaPadre = 24,
            TotalDebe = 0m,
            TotalHaber = 200m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [depreciacionEdificaciones]);

        AssertEqual(1, resumen.ActivoNoCorriente.Cuentas.Count);
        BalanceGeneralCuenta cuenta = resumen.ActivoNoCorriente.Cuentas[0];
        AssertEqual("1202", cuenta.CodigoCuenta);
        AssertTrue(cuenta.EsReductora);
        AssertEqual(200m, cuenta.ImporteConsolidado);
        AssertEqual(-200m, cuenta.ImpactoEnTotal);
        AssertEqual(1, cuenta.Subcuentas.Count);
        AssertEqual(200m, cuenta.Subcuentas[0].Importe);
        AssertEqual(-200m, cuenta.Subcuentas[0].ImpactoEnTotal);
        AssertFalse(cuenta.TieneSaldoContrario);
    }

    private static void Saldo_deudor_normal_3204_reduce_patrimonio_sin_saldo_contrario()
    {
        var perdidaEjercicio = new LibroMayorCuenta
        {
            IdCuentaContable = 40,
            CodigoCuenta = "3204",
            NombreCuenta = "Pérdida del Ejercicio",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 58,
            CodigoElemento = "3",
            CodigoGrupo = "32",
            NombreGrupo = "Resultados y Reservas",
            TotalDebe = 1000m,
            TotalHaber = 0m,
            Movimientos =
            [
                new LibroMayorMovimiento
                {
                    IdDetalleAsiento = 1,
                    IdCuentaContable = 40,
                    IdAsiento = 1,
                    NumeroAsiento = 1,
                    FechaAsiento = new DateTime(2026, 1, 31),
                    TipoAsiento = "Ajuste",
                    Debe = 1000m,
                    Haber = 0m,
                    OrdenDetalle = 1
                }
            ]
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [perdidaEjercicio]);

        BalanceGeneralCuenta cuenta = resumen.ResultadosReservas.Cuentas.Single();
        AssertEqual(-1000m, cuenta.ImporteConsolidado);
        AssertEqual(-1000m, cuenta.ImpactoEnTotal);
        AssertFalse(cuenta.TieneSaldoContrario);
        AssertEqual(0, resumen.CantidadSaldosContrarios);
        AssertTrue(resumen.TieneCuentasResultadoEjercicioRegistradas);
        AssertEqual(1, resumen.CantidadCuentasResultadoEjercicioRegistradas);
    }

    private static void Cuenta_deudora_con_saldo_acreedor_genera_diagnostico()
    {
        var caja = new LibroMayorCuenta
        {
            IdCuentaContable = 50,
            CodigoCuenta = "1101",
            NombreCuenta = "Caja",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 1,
            CodigoElemento = "1",
            CodigoGrupo = "11",
            NombreGrupo = "Activo Corriente",
            TotalDebe = 0m,
            TotalHaber = 100m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([caja]);

        AssertTrue(resumen.ActivoCorriente.Cuentas.Single().TieneSaldoContrario);
        AssertEqual(1, resumen.CantidadSaldosContrarios);
        AssertTrue(resumen.TieneSaldosContrarios);
    }

    private static void Cuenta_acreedora_con_saldo_deudor_genera_diagnostico()
    {
        var proveedores = new LibroMayorCuenta
        {
            IdCuentaContable = 51,
            CodigoCuenta = "2101",
            NombreCuenta = "Proveedores",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 35,
            CodigoElemento = "2",
            CodigoGrupo = "21",
            NombreGrupo = "Pasivo Corriente",
            TotalDebe = 100m,
            TotalHaber = 0m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [proveedores]);

        AssertTrue(resumen.PasivoCorriente.Cuentas.Single().TieneSaldoContrario);
        AssertEqual(1, resumen.CantidadSaldosContrarios);
        AssertTrue(resumen.TieneSaldosContrarios);
    }

    private static void Reductora_acreedora_con_saldo_deudor_conserva_signo_y_diagnostica()
    {
        var amortizacion = new LibroMayorCuenta
        {
            IdCuentaContable = 52,
            CodigoCuenta = "1204",
            NombreCuenta = "Amortización Acumulada",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 32,
            CodigoElemento = "1",
            CodigoGrupo = "12",
            NombreGrupo = "Activo No Corriente",
            TotalDebe = 75m,
            TotalHaber = 0m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [amortizacion]);

        BalanceGeneralCuenta cuenta = resumen.ActivoNoCorriente.Cuentas.Single();
        AssertEqual(-75m, cuenta.ImporteConsolidado);
        AssertEqual(75m, cuenta.ImpactoEnTotal);
        AssertTrue(cuenta.TieneSaldoContrario);
        AssertEqual(1, resumen.CantidadSaldosContrarios);
    }

    private static void Utilidad_incorpora_obligaciones_y_resultado_neto_en_balance_cuadrado()
    {
        var caja = new LibroMayorCuenta
        {
            IdCuentaContable = 60,
            CodigoCuenta = "1101",
            NombreCuenta = "Caja",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 1,
            IdGrupoContable = 11,
            CodigoGrupo = "11",
            NombreGrupo = "Activo Corriente",
            IdElementoContable = 1,
            CodigoElemento = "1",
            NombreElemento = "Activo",
            TotalDebe = 1000m,
            TotalHaber = 0m
        };
        var ventas = new LibroMayorCuenta
        {
            IdCuentaContable = 61,
            CodigoCuenta = "4101",
            NombreCuenta = "Ventas",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 59,
            IdGrupoContable = 41,
            CodigoGrupo = "41",
            NombreGrupo = "Ingresos Operacionales",
            IdElementoContable = 4,
            CodigoElemento = "4",
            NombreElemento = "Ingresos",
            TotalDebe = 0m,
            TotalHaber = 1000m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([caja, ventas]);

        AssertEqual(0.15m, resumen.TasaParticipacionTrabajadores);
        AssertEqual(150m, resumen.ParticipacionTrabajadores);
        AssertEqual(0.25m, resumen.TasaImpuestoRenta);
        AssertEqual(212.50m, resumen.ImpuestoRenta);
        AssertEqual(637.50m, resumen.ResultadoNeto);
        AssertEqual(362.50m, resumen.TotalPasivoCorriente);
        AssertEqual(1000m, resumen.TotalActivo);
        AssertEqual(362.50m, resumen.TotalPasivo);
        AssertEqual(637.50m, resumen.TotalPatrimonio);
        AssertEqual(1000m, resumen.TotalPasivoPatrimonio);
        AssertEqual(0m, resumen.Diferencia);
        AssertTrue(resumen.EstaCuadrado);
    }

    private static void Perdida_no_genera_obligaciones_y_reduce_patrimonio()
    {
        var ingresos = new LibroMayorCuenta
        {
            IdCuentaContable = 62,
            CodigoCuenta = "4101",
            NombreCuenta = "Ventas",
            NaturalezaCuenta = "Acreedora",
            OrdenCuenta = 59,
            IdGrupoContable = 41,
            CodigoGrupo = "41",
            NombreGrupo = "Ingresos Operacionales",
            IdElementoContable = 4,
            CodigoElemento = "4",
            NombreElemento = "Ingresos",
            TotalDebe = 0m,
            TotalHaber = 400m
        };
        var gastos = new LibroMayorCuenta
        {
            IdCuentaContable = 63,
            CodigoCuenta = "5101",
            NombreCuenta = "Gastos administrativos",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 67,
            IdGrupoContable = 51,
            CodigoGrupo = "51",
            NombreGrupo = "Gastos Administrativos",
            IdElementoContable = 5,
            CodigoElemento = "5",
            NombreElemento = "Gastos",
            TotalDebe = 1000m,
            TotalHaber = 0m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular(
            [ingresos, gastos]);

        AssertEqual(0m, resumen.ParticipacionTrabajadores);
        AssertEqual(0m, resumen.ImpuestoRenta);
        AssertEqual(-600m, resumen.ResultadoNeto);
        AssertEqual(0m, resumen.TotalPasivo);
        AssertEqual(-600m, resumen.TotalPatrimonio);
        AssertEqual(-600m, resumen.TotalPasivoPatrimonio);
        AssertEqual(600m, resumen.Diferencia);
        AssertFalse(resumen.EstaCuadrado);
    }

    private static void Resultado_cero_no_genera_obligaciones_y_balance_vacio_cuadra()
    {
        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([]);

        AssertEqual(0m, resumen.ParticipacionTrabajadores);
        AssertEqual(0m, resumen.ImpuestoRenta);
        AssertEqual(0m, resumen.ResultadoNeto);
        AssertEqual(0m, resumen.TotalActivo);
        AssertEqual(0m, resumen.TotalPasivo);
        AssertEqual(0m, resumen.TotalPatrimonio);
        AssertEqual(0m, resumen.TotalPasivoPatrimonio);
        AssertEqual(0m, resumen.Diferencia);
        AssertTrue(resumen.EstaCuadrado);
    }

    private static void Diferencia_conserva_el_descuadre_sin_ajustes()
    {
        var caja = new LibroMayorCuenta
        {
            IdCuentaContable = 64,
            CodigoCuenta = "1101",
            NombreCuenta = "Caja",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 1,
            CodigoGrupo = "11",
            CodigoElemento = "1",
            TotalDebe = 100m,
            TotalHaber = 0m
        };

        BalanceGeneralResumen resumen = BalanceGeneralCalculador.Calcular([caja]);

        AssertEqual(100m, resumen.TotalActivo);
        AssertEqual(0m, resumen.TotalPasivoPatrimonio);
        AssertEqual(100m, resumen.Diferencia);
        AssertFalse(resumen.EstaCuadrado);
    }

    private static void Calcular_rechaza_coleccion_nula()
    {
        AssertThrows<ArgumentNullException>(() =>
            BalanceGeneralCalculador.Calcular(null!));
    }

    private static void Subcuenta_sin_metadatos_de_padre_genera_error_explicito()
    {
        var subcuenta = new LibroMayorCuenta
        {
            IdCuentaContable = 70,
            CodigoCuenta = "120101",
            NombreCuenta = "Terrenos",
            NaturalezaCuenta = "Deudora",
            OrdenCuenta = 19,
            CodigoGrupo = "12",
            CodigoElemento = "1",
            IdCuentaPadre = 20,
            TotalDebe = 100m,
            TotalHaber = 0m
        };

        InvalidOperationException excepcion =
            AssertThrows<InvalidOperationException>(() =>
                BalanceGeneralCalculador.Calcular([subcuenta]));

        if (!excepcion.Message.Contains("120101", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "El error de metadatos no identifica la subcuenta afectada.");
        }
    }

    private static void AssertTrue(bool actual)
    {
        AssertEqual(true, actual);
    }

    private static void AssertFalse(bool actual)
    {
        AssertEqual(false, actual);
    }

    private static TException AssertThrows<TException>(Action accion)
        where TException : Exception
    {
        try
        {
            accion();
        }
        catch (TException excepcion)
        {
            return excepcion;
        }

        throw new InvalidOperationException(
            $"Se esperaba una excepción {typeof(TException).Name}.");
    }

    private static void AssertEqual<T>(T esperado, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, actual))
        {
            throw new InvalidOperationException(
                $"Se esperaba <{esperado}> pero se obtuvo <{actual}>.");
        }
    }
}
