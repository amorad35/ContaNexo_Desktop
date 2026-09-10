using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ContaNexo.Core.Models;
using ContaNexo.Data.Connections;
using ContaNexo.Data.Repositories;
using ContaNexo.Desktop.ViewModels;
using ContaNexo.Desktop.Views;

namespace ContaNexo.Desktop.Tests;

internal static class Program
{
    private static readonly List<(string Nombre, Action Prueba)> Pruebas =
    [
        (nameof(Filtra_cuentas_por_fragmento_de_codigo), Filtra_cuentas_por_fragmento_de_codigo),
        (nameof(Filtra_cuentas_por_fragmento_de_nombre_sin_distinguir_mayusculas), Filtra_cuentas_por_fragmento_de_nombre_sin_distinguir_mayusculas),
        (nameof(Incorpora_cuentas_cargadas_despues_de_crear_la_linea), Incorpora_cuentas_cargadas_despues_de_crear_la_linea),
        (nameof(Seleccionar_cuenta_conserva_el_objeto_y_su_id), Seleccionar_cuenta_conserva_el_objeto_y_su_id),
        (nameof(Seleccionar_desde_el_combo_editable_no_se_invalida), Seleccionar_desde_el_combo_editable_no_se_invalida),
        (nameof(Flechas_navegan_sugerencias_sin_modificar_la_cuenta_confirmada), Flechas_navegan_sugerencias_sin_modificar_la_cuenta_confirmada),
        (nameof(Editar_texto_de_una_seleccion_invalida_la_cuenta), Editar_texto_de_una_seleccion_invalida_la_cuenta),
        (nameof(Debe_y_haber_inician_vacios), Debe_y_haber_inician_vacios),
        (nameof(El_editor_de_importe_conserva_el_texto_mientras_se_escribe), El_editor_de_importe_conserva_el_texto_mientras_se_escribe),
        (nameof(Backspace_puede_vaciar_un_importe_sin_reconstruir_ceros), Backspace_puede_vaciar_un_importe_sin_reconstruir_ceros),
        (nameof(Un_segundo_separador_decimal_se_rechaza_sin_corromper_el_texto), Un_segundo_separador_decimal_se_rechaza_sin_corromper_el_texto),
        (nameof(El_importe_respeta_el_separador_de_la_cultura_actual), El_importe_respeta_el_separador_de_la_cultura_actual),
        (nameof(Los_importes_editados_actualizan_los_totales), Los_importes_editados_actualizan_los_totales),
        (nameof(La_fecha_inicial_se_ajusta_al_limite_correspondiente), La_fecha_inicial_se_ajusta_al_limite_correspondiente),
        (nameof(La_validacion_de_fecha_usa_limites_inclusivos), La_validacion_de_fecha_usa_limites_inclusivos),
        (nameof(El_datepicker_actualiza_sus_limites_al_cambiar_periodo), El_datepicker_actualiza_sus_limites_al_cambiar_periodo),
        (nameof(Una_captura_nueva_sin_editar_permanece_limpia), Una_captura_nueva_sin_editar_permanece_limpia),
        (nameof(La_descripcion_marca_la_captura_como_modificada), La_descripcion_marca_la_captura_como_modificada),
        (nameof(La_cuenta_o_su_busqueda_marcan_la_captura_como_modificada), La_cuenta_o_su_busqueda_marcan_la_captura_como_modificada),
        (nameof(Un_importe_no_cero_marca_la_captura_como_modificada), Un_importe_no_cero_marca_la_captura_como_modificada),
        (nameof(Agregar_una_linea_marca_la_captura_como_modificada), Agregar_una_linea_marca_la_captura_como_modificada),
        (nameof(Cambiar_fecha_o_tipo_marca_la_captura_como_modificada), Cambiar_fecha_o_tipo_marca_la_captura_como_modificada),
        (nameof(Cancelar_el_descarte_conserva_la_captura), Cancelar_el_descarte_conserva_la_captura),
        (nameof(Confirmar_el_descarte_limpia_y_cierra_la_captura), Confirmar_el_descarte_limpia_y_cierra_la_captura),
        (nameof(Un_guardado_exitoso_limpia_la_captura_modificada), Un_guardado_exitoso_limpia_la_captura_modificada),
        (nameof(Un_guardado_fallido_conserva_la_captura_modificada), Un_guardado_fallido_conserva_la_captura_modificada),
        (nameof(Volver_con_captura_limpia_no_solicita_confirmacion), Volver_con_captura_limpia_no_solicita_confirmacion),
        (nameof(La_navegacion_cancelada_permanece_en_el_libro_diario), La_navegacion_cancelada_permanece_en_el_libro_diario),
        (nameof(La_navegacion_confirmada_sale_del_libro_diario), La_navegacion_confirmada_sale_del_libro_diario),
        (nameof(El_cierre_cancelado_es_rechazado), El_cierre_cancelado_es_rechazado)
    ];

    [STAThread]
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

        Console.WriteLine($"Resultado: {aprobadas}/{Pruebas.Count} pruebas aprobadas.");
        return fallos.Count == 0 ? 0 : 1;
    }

    private static void Filtra_cuentas_por_fragmento_de_codigo()
    {
        var linea = new LineaAsientoViewModel(CrearCuentas());

        linea.TextoBusquedaCuenta = "1101";

        AssertCodigos(["1101"], linea.CuentasFiltradas.Cast<CuentaMovimiento>());
    }

    private static void Filtra_cuentas_por_fragmento_de_nombre_sin_distinguir_mayusculas()
    {
        var linea = new LineaAsientoViewModel(CrearCuentas());

        linea.TextoBusquedaCuenta = "INGRESOS finan";

        AssertCodigos(["4201"], linea.CuentasFiltradas.Cast<CuentaMovimiento>());
    }

    private static void Incorpora_cuentas_cargadas_despues_de_crear_la_linea()
    {
        var cuentas = new ObservableCollection<CuentaMovimiento>();
        var linea = new LineaAsientoViewModel(cuentas)
        {
            TextoBusquedaCuenta = "Caja"
        };

        cuentas.Add(CrearCuenta(1, "1101", "Caja"));

        AssertCodigos(["1101"], linea.CuentasFiltradas.Cast<CuentaMovimiento>());
    }

    private static void Seleccionar_cuenta_conserva_el_objeto_y_su_id()
    {
        ObservableCollection<CuentaMovimiento> cuentas = CrearCuentas();
        var linea = new LineaAsientoViewModel(cuentas);

        linea.CuentaSeleccionada = cuentas[0];

        AssertMismo(cuentas[0], linea.CuentaSeleccionada);
        AssertIgual(1, linea.CuentaSeleccionada?.IdCuentaContable);
        AssertIgual("1101 — Caja", linea.TextoBusquedaCuenta);
    }

    private static void Editar_texto_de_una_seleccion_invalida_la_cuenta()
    {
        ObservableCollection<CuentaMovimiento> cuentas = CrearCuentas();
        var linea = new LineaAsientoViewModel(cuentas)
        {
            CuentaSeleccionada = cuentas[0]
        };

        linea.TextoBusquedaCuenta = "texto sin seleccionar";

        AssertIgual<CuentaMovimiento?>(null, linea.CuentaSeleccionada);
    }

    private static void Seleccionar_desde_el_combo_editable_no_se_invalida()
    {
        ObservableCollection<CuentaMovimiento> cuentas = CrearCuentas();
        var linea = new LineaAsientoViewModel(cuentas)
        {
            TextoBusquedaCuenta = "Caj"
        };
        var selector = new ComboBox
        {
            DataContext = linea,
            IsEditable = true,
            IsTextSearchEnabled = false,
            ItemsSource = linea.CuentasFiltradas
        };
        selector.SetBinding(
            ComboBox.TextProperty,
            new Binding(nameof(LineaAsientoViewModel.TextoBusquedaCuenta))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            });
        selector.SetBinding(
            ComboBox.SelectedItemProperty,
            new Binding(nameof(LineaAsientoViewModel.CuentaSeleccionada))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            });

        LibroDiarioView vista =
            (LibroDiarioView)RuntimeHelpers.GetUninitializedObject(
                typeof(LibroDiarioView));
        MethodInfo dropDownClosed = typeof(LibroDiarioView).GetMethod(
            "CuentaSelector_DropDownClosed",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró el manejador de consolidación del autocomplete.");

        selector.SelectedItem = cuentas[0];
        dropDownClosed.Invoke(vista, [selector, EventArgs.Empty]);

        AssertMismo(cuentas[0], linea.CuentaSeleccionada);
        AssertIgual(1, linea.CuentaSeleccionada?.IdCuentaContable);
        AssertIgual("1101 — Caja", selector.Text);
    }

    private static void Flechas_navegan_sugerencias_sin_modificar_la_cuenta_confirmada()
    {
        ObservableCollection<CuentaMovimiento> cuentas = CrearCuentas();
        var linea = new LineaAsientoViewModel(cuentas)
        {
            TextoBusquedaCuenta = "Caj"
        };
        var selector = new ComboBox
        {
            DataContext = linea,
            IsEditable = true,
            IsTextSearchEnabled = false,
            ItemsSource = linea.CuentasFiltradas
        };
        selector.SetBinding(
            ComboBox.TextProperty,
            new Binding(nameof(LineaAsientoViewModel.TextoBusquedaCuenta))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            });
        selector.SetBinding(
            ComboBox.SelectedItemProperty,
            new Binding(nameof(LineaAsientoViewModel.CuentaSeleccionada))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            });

        MethodInfo procesarTecla = typeof(LibroDiarioView).GetMethod(
            "ProcesarTeclaNavegacionCuenta",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró el procesamiento de teclado del autocomplete.");

        bool abajo1 = (bool)(procesarTecla.Invoke(null, [selector, System.Windows.Input.Key.Down]) ?? false);
        AssertIgual(true, abajo1);
        AssertIgual(0, selector.SelectedIndex);
        AssertIgual<CuentaMovimiento?>(null, linea.CuentaSeleccionada);
        AssertIgual("Caj", linea.TextoBusquedaCuenta);

        bool abajo2 = (bool)(procesarTecla.Invoke(null, [selector, System.Windows.Input.Key.Down]) ?? false);
        AssertIgual(true, abajo2);
        AssertIgual(1, selector.SelectedIndex);
        AssertIgual<CuentaMovimiento?>(null, linea.CuentaSeleccionada);
        AssertIgual("Caj", linea.TextoBusquedaCuenta);

        bool arriba = (bool)(procesarTecla.Invoke(null, [selector, System.Windows.Input.Key.Up]) ?? false);
        AssertIgual(true, arriba);
        AssertIgual(0, selector.SelectedIndex);
        AssertIgual<CuentaMovimiento?>(null, linea.CuentaSeleccionada);
        AssertIgual("Caj", linea.TextoBusquedaCuenta);

        bool enter = (bool)(procesarTecla.Invoke(null, [selector, System.Windows.Input.Key.Enter]) ?? false);
        AssertIgual(true, enter);
        AssertMismo(cuentas[0], linea.CuentaSeleccionada);
        AssertIgual(1, linea.CuentaSeleccionada?.IdCuentaContable);
        AssertIgual("1101 — Caja", selector.Text);
    }

    private static void Debe_y_haber_inician_vacios()
    {
        var linea = new LineaAsientoViewModel(CrearCuentas());

        AssertIgual<decimal?>(null, linea.Debe);
        AssertIgual<decimal?>(null, linea.Haber);
        AssertIgual(0m, linea.Debe.GetValueOrDefault());
        AssertIgual(0m, linea.Haber.GetValueOrDefault());
    }

    private static void El_editor_de_importe_conserva_el_texto_mientras_se_escribe()
    {
        EjecutarConCultura(CultureInfo.InvariantCulture, () =>
        {
            var linea = new LineaAsientoViewModel(CrearCuentas());
            TextBox editor = CrearEditorImporte(
                linea,
                nameof(LineaAsientoViewModel.TextoDebe));

            editor.Text = "410";
            AssertIgual("410", editor.Text);
            AssertIgual(410m, linea.Debe);

            editor.Text = "410.";
            AssertIgual("410.", editor.Text);
            AssertIgual(410m, linea.Debe);

            editor.Text = "410.5";
            AssertIgual("410.5", editor.Text);
            AssertIgual(410.5m, linea.Debe);

            editor.Text = "410.50";
            AssertIgual("410.50", editor.Text);
            AssertIgual(410.50m, linea.Debe);
        });
    }

    private static void Backspace_puede_vaciar_un_importe_sin_reconstruir_ceros()
    {
        EjecutarConCultura(CultureInfo.InvariantCulture, () =>
        {
            var linea = new LineaAsientoViewModel(CrearCuentas());
            TextBox editor = CrearEditorImporte(
                linea,
                nameof(LineaAsientoViewModel.TextoDebe));

            foreach (string texto in new[] { "410.50", "410.5", "410.", "410", "41", "4", "" })
            {
                editor.Text = texto;
                AssertIgual(texto, editor.Text);
            }

            AssertIgual(string.Empty, linea.TextoDebe);
            AssertIgual<decimal?>(null, linea.Debe);
            AssertIgual(0m, linea.Debe.GetValueOrDefault());
        });
    }

    private static void Un_segundo_separador_decimal_se_rechaza_sin_corromper_el_texto()
    {
        EjecutarConCultura(CultureInfo.InvariantCulture, () =>
        {
            var linea = new LineaAsientoViewModel(CrearCuentas());
            TextBox editor = CrearEditorImporte(
                linea,
                nameof(LineaAsientoViewModel.TextoDebe));

            editor.Text = "410.1";
            editor.Text = "410.1.";

            AssertIgual("410.1", editor.Text);
            AssertIgual("410.1", linea.TextoDebe);
            AssertIgual(410.1m, linea.Debe);
        });
    }

    private static void El_importe_respeta_el_separador_de_la_cultura_actual()
    {
        EjecutarConCultura(CultureInfo.GetCultureInfo("es-EC"), () =>
        {
            var linea = new LineaAsientoViewModel(CrearCuentas());

            linea.TextoHaber = "410,5";

            AssertIgual("410,5", linea.TextoHaber);
            AssertIgual(410.5m, linea.Haber);
        });
    }

    private static void Los_importes_editados_actualizan_los_totales()
    {
        EjecutarConCultura(CultureInfo.InvariantCulture, () =>
        {
            var conexion = new ConexionBD("Server=.;Database=ContaNexoTests;Integrated Security=true;TrustServerCertificate=true");
            var libro = new LibroDiarioViewModel(
                new RepositorioCuentaContable(conexion),
                new RepositorioAsiento(conexion),
                () => null);

            libro.Lineas[0].TextoDebe = "410";
            libro.Lineas[1].TextoHaber = "410";

            AssertIgual(410m, libro.TotalDebe);
            AssertIgual(410m, libro.TotalHaber);
            AssertIgual(0m, libro.Diferencia);
            AssertIgual(true, libro.EstaCuadrado);
        });
    }

    private static void La_fecha_inicial_se_ajusta_al_limite_correspondiente()
    {
        DateTime hoy = DateTime.Today;
        LibroDiarioViewModel periodoFuturo = CrearLibro(
            CrearPeriodo(hoy.AddDays(10), hoy.AddDays(20)));
        LibroDiarioViewModel periodoActual = CrearLibro(
            CrearPeriodo(hoy.AddDays(-10), hoy.AddDays(10)));
        LibroDiarioViewModel periodoPasado = CrearLibro(
            CrearPeriodo(hoy.AddDays(-20), hoy.AddDays(-10)));

        AssertIgual(hoy.AddDays(10), periodoFuturo.FechaAsiento);
        AssertIgual(hoy, periodoActual.FechaAsiento);
        AssertIgual(hoy.AddDays(-10), periodoPasado.FechaAsiento);
    }

    private static void La_validacion_de_fecha_usa_limites_inclusivos()
    {
        DateTime inicio = new(2026, 10, 1);
        DateTime fin = new(2026, 10, 31);
        LibroDiarioViewModel libro = CrearLibro(CrearPeriodo(inicio, fin));
        PrepararCapturaCuadrada(libro);

        libro.FechaAsiento = inicio;
        AssertIgual(true, ValidarCaptura(libro));

        libro.FechaAsiento = fin;
        AssertIgual(true, ValidarCaptura(libro));

        libro.FechaAsiento = inicio.AddDays(-1);
        AssertIgual(false, ValidarCaptura(libro));
        AssertIgual(
            "La fecha del asiento debe estar entre el 01/10/2026 y el 31/10/2026.",
            libro.MensajeError);

        libro.FechaAsiento = fin.AddDays(1);
        AssertIgual(false, ValidarCaptura(libro));
        AssertIgual(
            "La fecha del asiento debe estar entre el 01/10/2026 y el 31/10/2026.",
            libro.MensajeError);
    }

    private static void El_datepicker_actualiza_sus_limites_al_cambiar_periodo()
    {
        if (Application.Current is null)
        {
            var aplicacion = new ContaNexo.Desktop.App();
            aplicacion.InitializeComponent();
        }

        PeriodoContableListado periodo = CrearPeriodo(
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));
        LibroDiarioViewModel libro = CrearLibro(() => periodo);
        var vista = new LibroDiarioView
        {
            DataContext = libro
        };
        var editor = vista.FindName("FechaAsientoEditor") as DatePicker
            ?? throw new InvalidOperationException(
                "No se encontró el DatePicker de FechaAsiento.");
        BindingExpression inicioBinding = editor.GetBindingExpression(
            DatePicker.DisplayDateStartProperty)
            ?? throw new InvalidOperationException(
                "El DatePicker no tiene binding para el límite inicial.");
        BindingExpression finBinding = editor.GetBindingExpression(
            DatePicker.DisplayDateEndProperty)
            ?? throw new InvalidOperationException(
                "El DatePicker no tiene binding para el límite final.");
        var editorEnPrueba = new DatePicker
        {
            DataContext = libro
        };
        editorEnPrueba.SetBinding(
            DatePicker.DisplayDateStartProperty,
            inicioBinding.ParentBinding);
        editorEnPrueba.SetBinding(
            DatePicker.DisplayDateEndProperty,
            finBinding.ParentBinding);

        AssertIgual<DateTime?>(new DateTime(2026, 10, 1), editorEnPrueba.DisplayDateStart);
        AssertIgual<DateTime?>(new DateTime(2026, 10, 31), editorEnPrueba.DisplayDateEnd);

        periodo = CrearPeriodo(
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30),
            idPeriodo: 2);
        libro.ActualizarPeriodoActivo();
        editorEnPrueba.GetBindingExpression(DatePicker.DisplayDateStartProperty)
            ?.UpdateTarget();
        editorEnPrueba.GetBindingExpression(DatePicker.DisplayDateEndProperty)
            ?.UpdateTarget();

        AssertIgual<DateTime?>(new DateTime(2026, 11, 1), editorEnPrueba.DisplayDateStart);
        AssertIgual<DateTime?>(new DateTime(2026, 11, 30), editorEnPrueba.DisplayDateEnd);
    }

    private static void Una_captura_nueva_sin_editar_permanece_limpia()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia();

        AssertIgual(false, libro.TieneCambiosSinGuardar);
    }

    private static void La_descripcion_marca_la_captura_como_modificada()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia();

        libro.DescripcionAsiento = "Compra de suministros";

        AssertIgual(true, libro.TieneCambiosSinGuardar);
    }

    private static void La_cuenta_o_su_busqueda_marcan_la_captura_como_modificada()
    {
        LibroDiarioViewModel conCuenta = CrearCapturaLimpia();
        LibroDiarioViewModel conBusqueda = CrearCapturaLimpia();

        conCuenta.Lineas[0].CuentaSeleccionada = CrearCuenta(1, "1101", "Caja");
        conBusqueda.Lineas[0].TextoBusquedaCuenta = "Caja";

        AssertIgual(true, conCuenta.TieneCambiosSinGuardar);
        AssertIgual(true, conBusqueda.TieneCambiosSinGuardar);
    }

    private static void Un_importe_no_cero_marca_la_captura_como_modificada()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia();

        libro.Lineas[0].TextoDebe = "0";
        AssertIgual(false, libro.TieneCambiosSinGuardar);

        libro.Lineas[0].TextoDebe = "10";
        AssertIgual(true, libro.TieneCambiosSinGuardar);
    }

    private static void Agregar_una_linea_marca_la_captura_como_modificada()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia();

        libro.AgregarLineaCommand.Execute(null);

        AssertIgual(true, libro.TieneCambiosSinGuardar);
    }

    private static void Cambiar_fecha_o_tipo_marca_la_captura_como_modificada()
    {
        LibroDiarioViewModel conFecha = CrearCapturaLimpia();
        LibroDiarioViewModel conTipo = CrearCapturaLimpia();

        conFecha.FechaAsiento = conFecha.FechaAsiento.AddDays(1);
        conTipo.TipoAsiento = "Ajuste";

        AssertIgual(true, conFecha.TieneCambiosSinGuardar);
        AssertIgual(true, conTipo.TieneCambiosSinGuardar);
    }

    private static void Cancelar_el_descarte_conserva_la_captura()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia(() => false);
        libro.DescripcionAsiento = "No descartar";

        bool puedeSalir = libro.IntentarDescartarCaptura();

        AssertIgual(false, puedeSalir);
        AssertIgual(true, libro.EstaCreandoAsiento);
        AssertIgual("No descartar", libro.DescripcionAsiento);
        AssertIgual(true, libro.TieneCambiosSinGuardar);
    }

    private static void Confirmar_el_descarte_limpia_y_cierra_la_captura()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia(() => true);
        libro.DescripcionAsiento = "Descartar";

        bool puedeSalir = libro.IntentarDescartarCaptura();

        AssertIgual(true, puedeSalir);
        AssertIgual(false, libro.EstaCreandoAsiento);
        AssertIgual<string?>(null, libro.DescripcionAsiento);
        AssertIgual(false, libro.TieneCambiosSinGuardar);
    }

    private static void Un_guardado_fallido_conserva_la_captura_modificada()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia();
        libro.DescripcionAsiento = "Falla validación por líneas vacías";

        libro.GuardarCommand.Execute(null);

        AssertIgual(true, libro.TieneCambiosSinGuardar);
        AssertIgual(true, libro.EstaCreandoAsiento);
    }

    private static void Un_guardado_exitoso_limpia_la_captura_modificada()
    {
        LibroDiarioViewModel libro = CrearCapturaLimpia();
        libro.DescripcionAsiento = "Asiento guardado";
        MethodInfo metodo = typeof(LibroDiarioViewModel).GetMethod(
            "CompletarGuardadoExitoso",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró la transición posterior al guardado exitoso.");

        metodo.Invoke(libro, [new AsientoCreacionResultado
        {
            IdAsiento = 10,
            NumeroAsiento = 25
        }]);

        AssertIgual(false, libro.TieneCambiosSinGuardar);
        AssertIgual(false, libro.EstaCreandoAsiento);
    }

    private static void La_navegacion_cancelada_permanece_en_el_libro_diario()
    {
        MainWindowViewModel principal = CrearVentanaPrincipal(_ => false);
        LibroDiarioViewModel libro = ObtenerLibroDiario(principal);
        PrepararCapturaLimpia(libro);
        libro.DescripcionAsiento = "No salir";
        EstablecerVistaActual(principal, libro);

        principal.NavegarInicioCommand.Execute(null);

        AssertMismo(libro, principal.VistaActual);
        AssertIgual(true, libro.TieneCambiosSinGuardar);
    }

    private static void Volver_con_captura_limpia_no_solicita_confirmacion()
    {
        int confirmaciones = 0;
        LibroDiarioViewModel libro = CrearCapturaLimpia(() =>
        {
            confirmaciones++;
            return false;
        });

        bool puedeSalir = libro.IntentarDescartarCaptura();

        AssertIgual(true, puedeSalir);
        AssertIgual(0, confirmaciones);
        AssertIgual(false, libro.EstaCreandoAsiento);
    }

    private static void La_navegacion_confirmada_sale_del_libro_diario()
    {
        MainWindowViewModel principal = CrearVentanaPrincipal(_ => true);
        LibroDiarioViewModel libro = ObtenerLibroDiario(principal);
        PrepararCapturaLimpia(libro);
        libro.DescripcionAsiento = "Salir";
        EstablecerVistaActual(principal, libro);

        principal.NavegarInicioCommand.Execute(null);

        AssertIgual(false, ReferenceEquals(libro, principal.VistaActual));
        AssertIgual(false, libro.TieneCambiosSinGuardar);
    }

    private static void El_cierre_cancelado_es_rechazado()
    {
        MainWindowViewModel principal = CrearVentanaPrincipal(_ => false);
        LibroDiarioViewModel libro = ObtenerLibroDiario(principal);
        PrepararCapturaLimpia(libro);
        libro.DescripcionAsiento = "No cerrar";

        AssertIgual(false, principal.PuedeCerrarAplicacion());
        AssertIgual(true, libro.TieneCambiosSinGuardar);
    }

    private static LibroDiarioViewModel CrearLibro(
        PeriodoContableListado periodo,
        Func<bool>? confirmarSalidaSinGuardar = null)
    {
        return CrearLibro(() => periodo, confirmarSalidaSinGuardar);
    }

    private static LibroDiarioViewModel CrearLibro(
        Func<PeriodoContableListado> obtenerPeriodo,
        Func<bool>? confirmarSalidaSinGuardar = null)
    {
        var conexion = new ConexionBD("Server=.;Database=ContaNexoTests;Integrated Security=true;TrustServerCertificate=true");
        return new LibroDiarioViewModel(
            new RepositorioCuentaContable(conexion),
            new RepositorioAsiento(conexion),
            obtenerPeriodo,
            confirmarSalidaSinGuardar);
    }

    private static LibroDiarioViewModel CrearCapturaLimpia(
        Func<bool>? confirmarSalidaSinGuardar = null)
    {
        LibroDiarioViewModel libro = CrearLibro(
            CrearPeriodo(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(10)),
            confirmarSalidaSinGuardar);
        PrepararCapturaLimpia(libro);
        return libro;
    }

    private static void PrepararCapturaLimpia(LibroDiarioViewModel libro)
    {
        FieldInfo campo = typeof(LibroDiarioViewModel).GetField(
            "_estaCreandoAsiento",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró el estado interno de creación.");
        campo.SetValue(libro, true);

        MethodInfo metodo = typeof(LibroDiarioViewModel).GetMethod(
            "MarcarCapturaLimpia",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró la captura del estado limpio.");
        metodo.Invoke(libro, null);
    }

    private static MainWindowViewModel CrearVentanaPrincipal(
        Func<string, bool> confirmarSalidaSinGuardar)
    {
        var conexion = new ConexionBD("Server=.;Database=ContaNexoTests;Integrated Security=true;TrustServerCertificate=true");
        return new MainWindowViewModel(
            new RepositorioEmpresa(conexion),
            new RepositorioPeriodoContable(conexion),
            new RepositorioCuentaContable(conexion),
            new RepositorioAsiento(conexion),
            new RepositorioLibroMayor(conexion),
            confirmarSalidaSinGuardar);
    }

    private static LibroDiarioViewModel ObtenerLibroDiario(
        MainWindowViewModel principal)
    {
        FieldInfo campo = typeof(MainWindowViewModel).GetField(
            "_libroDiarioViewModel",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró el Libro Diario de la ventana principal.");
        return (LibroDiarioViewModel)(campo.GetValue(principal)
            ?? throw new InvalidOperationException("El Libro Diario no está inicializado."));
    }

    private static void EstablecerVistaActual(
        MainWindowViewModel principal,
        ViewModelBase vista)
    {
        FieldInfo campo = typeof(MainWindowViewModel).GetField(
            "_vistaActual",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró la vista actual de la ventana principal.");
        campo.SetValue(principal, vista);
    }

    private static PeriodoContableListado CrearPeriodo(
        DateTime inicio,
        DateTime fin,
        int idPeriodo = 1)
    {
        return new PeriodoContableListado
        {
            IdPeriodoContable = idPeriodo,
            IdEmpresa = 1,
            NombreEmpresa = "Empresa de prueba",
            NombrePeriodo = "Período de prueba",
            FechaInicioPeriodo = inicio,
            FechaFinPeriodo = fin,
            EstadoPeriodo = "Abierto"
        };
    }

    private static void PrepararCapturaCuadrada(LibroDiarioViewModel libro)
    {
        libro.Lineas[0].CuentaSeleccionada = CrearCuenta(1, "1101", "Caja");
        libro.Lineas[0].TextoDebe = "40";
        libro.Lineas[1].CuentaSeleccionada = CrearCuenta(2, "2101", "Proveedores");
        libro.Lineas[1].TextoHaber = "40";
    }

    private static bool ValidarCaptura(LibroDiarioViewModel libro)
    {
        MethodInfo metodo = typeof(LibroDiarioViewModel).GetMethod(
            "ValidarCaptura",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "No se encontró la validación de captura.");
        object?[] argumentos = [null];

        return (bool)(metodo.Invoke(libro, argumentos) ?? false);
    }

    private static TextBox CrearEditorImporte(
        LineaAsientoViewModel linea,
        string propiedadTexto)
    {
        var editor = new TextBox
        {
            DataContext = linea
        };
        editor.SetBinding(
            TextBox.TextProperty,
            new Binding(propiedadTexto)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

        return editor;
    }

    private static void EjecutarConCultura(CultureInfo cultura, Action accion)
    {
        CultureInfo culturaAnterior = CultureInfo.CurrentCulture;
        CultureInfo culturaUiAnterior = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = cultura;
            CultureInfo.CurrentUICulture = cultura;
            accion();
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
            CultureInfo.CurrentUICulture = culturaUiAnterior;
        }
    }

    private static ObservableCollection<CuentaMovimiento> CrearCuentas()
    {
        return
        [
            CrearCuenta(1, "1101", "Caja"),
            CrearCuenta(2, "1102", "Caja Chica"),
            CrearCuenta(3, "1103", "Bancos"),
            CrearCuenta(4, "4201", "Ingresos Financieros")
        ];
    }

    private static CuentaMovimiento CrearCuenta(int id, string codigo, string nombre)
    {
        return new CuentaMovimiento
        {
            IdCuentaContable = id,
            CodigoCuenta = codigo,
            NombreCuenta = nombre
        };
    }

    private static void AssertCodigos(
        IReadOnlyList<string> esperados,
        IEnumerable<CuentaMovimiento> cuentas)
    {
        string[] actuales = cuentas.Select(cuenta => cuenta.CodigoCuenta).ToArray();

        if (!esperados.SequenceEqual(actuales))
        {
            throw new InvalidOperationException(
                $"Se esperaban [{string.Join(", ", esperados)}] pero se obtuvieron [{string.Join(", ", actuales)}].");
        }
    }

    private static void AssertMismo(object esperado, object? actual)
    {
        if (!ReferenceEquals(esperado, actual))
        {
            throw new InvalidOperationException("La selección no conserva la instancia de cuenta esperada.");
        }
    }

    private static void AssertIgual<T>(T esperado, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, actual))
        {
            throw new InvalidOperationException(
                $"Se esperaba <{esperado}> pero se obtuvo <{actual}>.");
        }
    }
}
