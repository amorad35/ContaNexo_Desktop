using System.Globalization;
using ContaNexo.Core.Models;
using ContaNexo.Desktop.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContaNexo.Desktop.Reporting;

public sealed class ReportesPdfGenerador
{
    private static readonly CultureInfo Cultura = CultureInfo.CurrentCulture;

    internal static IReadOnlyList<string> OrdenReporteCompleto { get; } =
    [
        "Libro Diario",
        "Libro Mayor",
        "Balance de Sumas y Saldos",
        "Estado de Resultados",
        "Balance General"
    ];

    public static string ObtenerEstadoReporte(PeriodoContableListado periodo) =>
        string.Equals(periodo.EstadoPeriodo, "Cerrado", StringComparison.OrdinalIgnoreCase)
            ? "FINAL / HISTÓRICO"
            : "PROVISIONAL";

    internal static IReadOnlyList<string> ObtenerAdvertenciasBalance(
        IReadOnlyList<LibroMayorCuenta> cuentas)
    {
        List<BalanceSumasSaldosItemViewModel> filas = cuentas
            .Select(cuenta => new BalanceSumasSaldosItemViewModel(cuenta))
            .ToList();
        var advertencias = new List<string>();
        decimal totalDebe = filas.Sum(fila => fila.TotalDebe);
        decimal totalHaber = filas.Sum(fila => fila.TotalHaber);
        decimal totalSaldoDeudor = filas.Sum(fila => fila.SaldoDeudor);
        decimal totalSaldoAcreedor = filas.Sum(fila => fila.SaldoAcreedor);
        int inconsistencias = filas.Count(fila => fila.TieneInconsistenciaSaldo);
        int saldosContrarios = filas.Count(fila => fila.TieneSaldoContrarioNaturaleza);

        if (totalDebe != totalHaber)
            advertencias.Add("Revisión requerida: los totales Debe y Haber no coinciden.");
        if (totalSaldoDeudor != totalSaldoAcreedor)
            advertencias.Add("Revisión requerida: los saldos Deudor y Acreedor no coinciden.");
        if (inconsistencias > 0 || saldosContrarios > 0)
        {
            advertencias.Add(
                $"Revisión requerida: {inconsistencias} inconsistencia(s) de saldo y {saldosContrarios} saldo(s) contrario(s) a la naturaleza.");
        }

        return advertencias;
    }

    public byte[] GenerarBalanceSumasSaldos(
        Empresa empresa,
        PeriodoContableListado periodo,
        IReadOnlyList<LibroMayorCuenta> cuentas) =>
        GenerarDocumento(empresa, periodo, OrdenReporteCompleto[2],
            CrearComposicionBalanceSumasSaldos(cuentas));

    public byte[] GenerarEstadoResultados(
        Empresa empresa,
        PeriodoContableListado periodo,
        EstadoResultadosResumen resumen) =>
        GenerarDocumento(empresa, periodo, OrdenReporteCompleto[3],
            CrearComposicionEstadoResultados(resumen));

    public byte[] GenerarBalanceGeneral(
        Empresa empresa,
        PeriodoContableListado periodo,
        BalanceGeneralResumen resumen) =>
        GenerarDocumento(empresa, periodo, OrdenReporteCompleto[4],
            CrearComposicionBalanceGeneral(resumen));

    public byte[] GenerarLibroMayor(
        Empresa empresa,
        PeriodoContableListado periodo,
        IReadOnlyList<LibroMayorCuenta> cuentas) =>
        GenerarDocumento(empresa, periodo, OrdenReporteCompleto[1],
            CrearComposicionLibroMayor(cuentas));

    public byte[] GenerarLibroDiario(
        Empresa empresa,
        PeriodoContableListado periodo,
        IReadOnlyList<AsientoDetalleConsulta> asientos) =>
        GenerarDocumento(empresa, periodo, OrdenReporteCompleto[0],
            CrearComposicionLibroDiario(asientos));

    public byte[] GenerarReporteCompleto(
        Empresa empresa,
        PeriodoContableListado periodo,
        IReadOnlyList<AsientoDetalleConsulta> asientos,
        IReadOnlyList<LibroMayorCuenta> cuentas,
        EstadoResultadosResumen estadoResultados,
        BalanceGeneralResumen balanceGeneral)
    {
        return Document.Create(documento =>
        {
            AgregarPagina(documento, empresa, periodo, OrdenReporteCompleto[0], CrearComposicionLibroDiario(asientos));
            AgregarPagina(documento, empresa, periodo, OrdenReporteCompleto[1], CrearComposicionLibroMayor(cuentas));
            AgregarPagina(documento, empresa, periodo, OrdenReporteCompleto[2], CrearComposicionBalanceSumasSaldos(cuentas));
            AgregarPagina(documento, empresa, periodo, OrdenReporteCompleto[3], CrearComposicionEstadoResultados(estadoResultados));
            AgregarPagina(documento, empresa, periodo, OrdenReporteCompleto[4], CrearComposicionBalanceGeneral(balanceGeneral));
        }).GeneratePdf();
    }

    private static Action<IContainer> CrearComposicionBalanceSumasSaldos(
        IReadOnlyList<LibroMayorCuenta> cuentas) => contenido =>
        contenido.Column(columna =>
        {
            foreach (string advertencia in ObtenerAdvertenciasBalance(cuentas))
                Advertencia(columna, advertencia);
            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.ConstantColumn(55);
                    columnas.RelativeColumn(2.2f);
                    columnas.ConstantColumn(58);
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                });
                tabla.Header(cabecera =>
                {
                    CeldaCabecera(cabecera, "Código");
                    CeldaCabecera(cabecera, "Cuenta");
                    CeldaCabecera(cabecera, "Naturaleza");
                    CeldaCabecera(cabecera, "Suma Debe", true);
                    CeldaCabecera(cabecera, "Suma Haber", true);
                    CeldaCabecera(cabecera, "Saldo Deudor", true);
                    CeldaCabecera(cabecera, "Saldo Acreedor", true);
                });
                foreach (LibroMayorCuenta cuenta in cuentas)
                {
                    Celda(tabla, cuenta.CodigoCuenta);
                    Celda(tabla, cuenta.NombreCuenta);
                    Celda(tabla, cuenta.NaturalezaCuenta);
                    Celda(tabla, Moneda(cuenta.TotalDebe), true);
                    Celda(tabla, Moneda(cuenta.TotalHaber), true);
                    Celda(tabla, Moneda(cuenta.SaldoDeudor), true);
                    Celda(tabla, Moneda(cuenta.SaldoAcreedor), true);
                }
                CeldaTotal(tabla, "TOTALES", 3);
                CeldaTotal(tabla, Moneda(cuentas.Sum(x => x.TotalDebe)));
                CeldaTotal(tabla, Moneda(cuentas.Sum(x => x.TotalHaber)));
                CeldaTotal(tabla, Moneda(cuentas.Sum(x => x.SaldoDeudor)));
                CeldaTotal(tabla, Moneda(cuentas.Sum(x => x.SaldoAcreedor)));
            });
        });

    private static Action<IContainer> CrearComposicionEstadoResultados(
        EstadoResultadosResumen resumen) => contenido =>
        contenido.Column(columna =>
        {
            if (resumen.TieneSaldosContrarios)
                Advertencia(columna, $"Revisión requerida: {resumen.CantidadSaldosContrarios} cuenta(s) presentan saldo contrario a su naturaleza.");
            SeccionEstadoResultados(columna, "INGRESOS", resumen.GruposIngresos);
            FilaResumen(columna, "TOTAL INGRESOS", resumen.TotalIngresos);
            SeccionEstadoResultados(columna, "GASTOS", resumen.GruposGastos);
            FilaResumen(columna, "TOTAL GASTOS", resumen.TotalGastos);
            FilaResumen(columna, "RESULTADO DEL PERÍODO", resumen.ResultadoPeriodo);
            FilaResumen(columna, "PARTICIPACIÓN TRABAJADORES", -resumen.ParticipacionTrabajadores);
            FilaResumen(columna, "RESULTADO ANTES DE IMPUESTO", resumen.ResultadoAntesImpuestoRenta);
            FilaResumen(columna, "IMPUESTO A LA RENTA", -resumen.ImpuestoRenta);
            FilaResumen(columna, "RESULTADO NETO", resumen.ResultadoNeto, true);
        });

    private static Action<IContainer> CrearComposicionBalanceGeneral(
        BalanceGeneralResumen resumen) => contenido =>
        contenido.Column(columna =>
        {
            if (resumen.TieneSaldosContrarios)
                Advertencia(columna, $"Revisión requerida: {resumen.CantidadSaldosContrarios} cuenta(s) presentan saldo contrario a su naturaleza.");
            if (resumen.TieneCuentasResultadoEjercicioRegistradas)
                Advertencia(columna, "Revisión requerida: existen cuentas de resultado del ejercicio registradas y el resultado neto también se incorpora al balance.");
            SeccionBalance(columna, "ACTIVO CORRIENTE", resumen.ActivoCorriente);
            SeccionBalance(columna, "ACTIVO NO CORRIENTE", resumen.ActivoNoCorriente);
            FilaResumen(columna, "TOTAL ACTIVO", resumen.TotalActivo, true);
            SeccionBalance(columna, "PASIVO CORRIENTE", resumen.PasivoCorriente);
            FilaDetalle(columna, "Participación trabajadores", resumen.ParticipacionTrabajadores);
            FilaDetalle(columna, "Impuesto a la renta", resumen.ImpuestoRenta);
            FilaResumen(columna, "TOTAL PASIVO CORRIENTE", resumen.TotalPasivoCorriente);
            SeccionBalance(columna, "PASIVO NO CORRIENTE", resumen.PasivoNoCorriente);
            FilaResumen(columna, "TOTAL PASIVO", resumen.TotalPasivo, true);
            SeccionBalance(columna, "CAPITAL CONTABLE", resumen.CapitalContable);
            SeccionBalance(columna, "RESULTADOS Y RESERVAS", resumen.ResultadosReservas);
            FilaDetalle(columna, "Resultado neto", resumen.ResultadoNeto);
            FilaResumen(columna, "TOTAL PATRIMONIO", resumen.TotalPatrimonio, true);
            FilaResumen(columna, "TOTAL PASIVO + PATRIMONIO", resumen.TotalPasivoPatrimonio, true);
            FilaResumen(columna, "DIFERENCIA", resumen.Diferencia, true);
            columna.Item().PaddingTop(5).Text(
                resumen.EstaCuadrado ? "IGUALDAD CONTABLE: CUADRADA" : "IGUALDAD CONTABLE: NO CUADRADA")
                .Bold().FontColor(resumen.EstaCuadrado ? Colors.Green.Darken2 : Colors.Red.Darken2);
        });

    private static Action<IContainer> CrearComposicionLibroMayor(
        IReadOnlyList<LibroMayorCuenta> cuentas) => contenido =>
        contenido.Column(columna =>
        {
            if (cuentas.Count == 0)
                columna.Item().Text("No existen movimientos para el período seleccionado.").Italic();
            foreach (LibroMayorCuenta cuenta in cuentas)
            {
                columna.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6)
                    .Text($"{cuenta.CodigoCuenta} — {cuenta.NombreCuenta} · {cuenta.NaturalezaCuenta}").Bold();
                columna.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        columnas.ConstantColumn(48);
                        columnas.ConstantColumn(62);
                        columnas.RelativeColumn(2.5f);
                        columnas.RelativeColumn();
                        columnas.RelativeColumn();
                    });
                    tabla.Header(cabecera =>
                    {
                        CeldaCabecera(cabecera, "Asiento");
                        CeldaCabecera(cabecera, "Fecha");
                        CeldaCabecera(cabecera, "Descripción");
                        CeldaCabecera(cabecera, "Debe", true);
                        CeldaCabecera(cabecera, "Haber", true);
                    });
                    foreach (LibroMayorMovimiento movimiento in cuenta.Movimientos)
                    {
                        Celda(tabla, movimiento.NumeroAsiento.ToString(Cultura));
                        Celda(tabla, movimiento.FechaAsiento.ToString("dd/MM/yyyy", Cultura));
                        Celda(tabla, movimiento.DescripcionAsiento ?? string.Empty);
                        Celda(tabla, Moneda(movimiento.Debe), true);
                        Celda(tabla, Moneda(movimiento.Haber), true);
                    }
                    CeldaTotal(tabla, "TOTALES", 3);
                    CeldaTotal(tabla, Moneda(cuenta.TotalDebe));
                    CeldaTotal(tabla, Moneda(cuenta.TotalHaber));
                });
                string saldo = cuenta.SaldoDeudor > 0
                    ? $"Saldo deudor: {Moneda(cuenta.SaldoDeudor)}"
                    : cuenta.SaldoAcreedor > 0
                        ? $"Saldo acreedor: {Moneda(cuenta.SaldoAcreedor)}"
                        : $"Saldo: {Moneda(0)}";
                columna.Item().AlignRight().PaddingBottom(5).Text(saldo).SemiBold();
            }
        });

    private static Action<IContainer> CrearComposicionLibroDiario(
        IReadOnlyList<AsientoDetalleConsulta> asientos) => contenido =>
        contenido.Column(columna =>
        {
            if (asientos.Count == 0)
                columna.Item().Text("No existen movimientos para el período seleccionado.").Italic();
            foreach (AsientoDetalleConsulta asiento in asientos)
            {
                columna.Item().PaddingTop(8).Background(Colors.Grey.Lighten3).Padding(6).Column(info =>
                {
                    info.Item().Text($"ASIENTO N.º {asiento.NumeroAsiento} · {asiento.FechaAsiento:dd/MM/yyyy} · {asiento.TipoAsiento}").Bold();
                    info.Item().Text(asiento.DescripcionAsiento ?? "Sin descripción").FontSize(8);
                });
                columna.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        columnas.ConstantColumn(62);
                        columnas.RelativeColumn(3);
                        columnas.RelativeColumn();
                        columnas.RelativeColumn();
                    });
                    tabla.Header(cabecera =>
                    {
                        CeldaCabecera(cabecera, "Código");
                        CeldaCabecera(cabecera, "Cuenta");
                        CeldaCabecera(cabecera, "Debe", true);
                        CeldaCabecera(cabecera, "Haber", true);
                    });
                    foreach (DetalleAsientoConsulta detalle in asiento.Movimientos)
                    {
                        Celda(tabla, detalle.CodigoCuenta);
                        Celda(tabla, detalle.NombreCuenta);
                        Celda(tabla, Moneda(detalle.DebeDetalle), true);
                        Celda(tabla, Moneda(detalle.HaberDetalle), true);
                    }
                    CeldaTotal(tabla, "TOTAL ASIENTO", 2);
                    CeldaTotal(tabla, Moneda(asiento.TotalDebe));
                    CeldaTotal(tabla, Moneda(asiento.TotalHaber));
                });
            }
        });

    private static byte[] GenerarDocumento(
        Empresa empresa,
        PeriodoContableListado periodo,
        string nombreReporte,
        Action<IContainer> componerContenido)
    {
        return Document.Create(documento =>
            AgregarPagina(documento, empresa, periodo, nombreReporte, componerContenido))
            .GeneratePdf();
    }

    private static void AgregarPagina(
        IDocumentContainer documento,
        Empresa empresa,
        PeriodoContableListado periodo,
        string nombreReporte,
        Action<IContainer> componerContenido)
    {
        documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(28);
            pagina.DefaultTextStyle(texto => texto.FontSize(8.5f));
            pagina.Header().Column(cabecera =>
            {
                cabecera.Item().Text("ContaNexo").FontSize(17).Bold().FontColor(Colors.Blue.Darken2);
                cabecera.Item().Text(empresa.NombreEmpresa).FontSize(11).SemiBold();
                cabecera.Item().Text(nombreReporte).FontSize(15).Bold();
                cabecera.Item().Text($"{periodo.NombrePeriodo} · {periodo.FechaInicioPeriodo:dd/MM/yyyy} – {periodo.FechaFinPeriodo:dd/MM/yyyy}");
                cabecera.Item().PaddingTop(2).Text(ObtenerEstadoReporte(periodo)).Bold();
                cabecera.Item().PaddingVertical(7).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
            pagina.Content().PaddingVertical(5).Element(componerContenido);
            pagina.Footer().Row(pie =>
            {
                pie.RelativeItem().Text($"Generado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)}").FontSize(7);
                pie.RelativeItem().AlignRight().DefaultTextStyle(estilo => estilo.FontSize(7)).Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        });
    }

    private static void SeccionEstadoResultados(
        ColumnDescriptor columna,
        string titulo,
        IReadOnlyList<EstadoResultadosGrupo> grupos)
    {
        columna.Item().PaddingTop(7).Text(titulo).FontSize(11).Bold();
        foreach (EstadoResultadosGrupo grupo in grupos)
        {
            columna.Item().PaddingTop(4).Text($"{grupo.CodigoGrupo} — {grupo.NombreGrupo}").SemiBold();
            foreach (EstadoResultadosCuenta cuenta in grupo.Cuentas)
            {
                FilaDetalle(columna, $"{cuenta.CodigoCuenta}  {cuenta.NombreCuenta}", cuenta.ImporteConsolidado);
                foreach (EstadoResultadosSubcuenta subcuenta in cuenta.Subcuentas)
                    FilaDetalle(columna, $"    {subcuenta.CodigoCuenta}  {subcuenta.NombreCuenta}", subcuenta.Importe);
            }
            FilaResumen(columna, $"Total {grupo.NombreGrupo}", grupo.TotalGrupo);
        }
    }

    private static void SeccionBalance(ColumnDescriptor columna, string titulo, BalanceGeneralGrupo grupo)
    {
        columna.Item().PaddingTop(7).Text(titulo).FontSize(11).Bold();
        foreach (BalanceGeneralCuenta cuenta in grupo.Cuentas)
        {
            FilaDetalle(columna, $"{cuenta.CodigoCuenta}  {cuenta.NombreCuenta}", cuenta.ImporteConsolidado);
            foreach (BalanceGeneralSubcuenta subcuenta in cuenta.Subcuentas)
                FilaDetalle(columna, $"    {subcuenta.CodigoCuenta}  {subcuenta.NombreCuenta}", subcuenta.Importe);
        }
        FilaResumen(columna, $"TOTAL {titulo}", grupo.TotalGrupo);
    }

    private static void FilaDetalle(ColumnDescriptor columna, string etiqueta, decimal importe) =>
        columna.Item().PaddingVertical(1).Row(fila =>
        {
            fila.RelativeItem().Text(etiqueta);
            fila.ConstantItem(90).AlignRight().Text(Moneda(importe));
        });

    private static void Advertencia(ColumnDescriptor columna, string mensaje) =>
        columna.Item().PaddingBottom(7).Background(Colors.Orange.Lighten4)
            .Border(1).BorderColor(Colors.Orange.Medium).Padding(7)
            .Text(mensaje).SemiBold().FontColor(Colors.Orange.Darken4);

    private static void FilaResumen(ColumnDescriptor columna, string etiqueta, decimal importe, bool destacado = false) =>
        columna.Item().PaddingTop(3).BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(3).Row(fila =>
        {
            IContainer izquierda = fila.RelativeItem();
            IContainer derecha = fila.ConstantItem(100).AlignRight();
            if (destacado)
            {
                izquierda.Text(etiqueta).Bold().FontSize(10);
                derecha.Text(Moneda(importe)).Bold().FontSize(10);
            }
            else
            {
                izquierda.Text(etiqueta).SemiBold();
                derecha.Text(Moneda(importe)).SemiBold();
            }
        });

    private static void CeldaCabecera(TableCellDescriptor cabecera, string texto, bool derecha = false)
    {
        IContainer contenedor = cabecera.Cell().Element(EstiloCeldaCabecera);
        if (derecha) contenedor.AlignRight().Text(texto).Bold();
        else contenedor.Text(texto).Bold();
    }

    private static void Celda(TableDescriptor tabla, string texto, bool derecha = false)
    {
        IContainer contenedor = tabla.Cell().Element(EstiloCelda);
        if (derecha) contenedor.AlignRight().Text(texto);
        else contenedor.Text(texto);
    }

    private static void CeldaTotal(TableDescriptor tabla, string texto, uint columnas = 1)
    {
        IContainer contenedor = tabla.Cell().ColumnSpan(columnas).Element(EstiloCeldaTotal);
        contenedor.AlignRight().Text(texto).Bold();
    }

    private static IContainer EstiloCeldaCabecera(IContainer contenedor) =>
        contenedor.Background(Colors.Grey.Lighten2).Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Medium);

    private static IContainer EstiloCelda(IContainer contenedor) =>
        contenedor.Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

    private static IContainer EstiloCeldaTotal(IContainer contenedor) =>
        contenedor.Background(Colors.Grey.Lighten3).Padding(4).BorderTop(1).BorderColor(Colors.Grey.Medium);

    private static string Moneda(decimal valor) => valor.ToString("N2", Cultura);
}
