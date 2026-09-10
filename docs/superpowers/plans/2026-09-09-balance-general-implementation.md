# Balance General Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar un Balance General calculado, consultable por período abierto o cerrado, con consolidación contable, diagnósticos y una vista WPF integrada al flujo existente.

**Architecture:** `BalanceGeneralCalculador` transformará las cuentas devueltas por `RepositorioLibroMayor` en seis secciones del Balance General y reutilizará `EstadoResultadosCalculador` para las partidas calculadas. `BalanceGeneralViewModel` mantendrá una selección de período local obtenida con `RepositorioPeriodoContable`; la vista solo presentará el resumen y la navegación existente construirá el módulo sin introducir servicios, DI ni persistencia adicional.

**Tech Stack:** C# 14, .NET 10, WPF, MVVM simple, Dapper y SQL Server Express.

**Spec:** `docs/superpowers/specs/2026-09-09-balance-general-design.md`

## Global Constraints

- No hacer commit, push, reset ni restore de Git.
- No abrir automáticamente la aplicación WPF.
- No crear tablas ni Stored Procedures de Balance General.
- No agregar paquetes NuGet.
- No escribir ni modificar movimientos contables, incluidos `3203` y `3204`.
- No modificar el comportamiento de Períodos Contables, Estado de Resultados ni otros módulos fuera de la integración mínima.
- Clasificar secciones únicamente con `CodigoElemento` y `CodigoGrupo`.
- Usar `NaturalezaCuenta` únicamente para diagnosticar la orientación real Debe/Haber.
- Ejecutar cada ciclo RED y GREEN de la lógica contable de Core y conservar su salida para el informe final; la coordinación y presentación WPF se validarán mediante compilación y las pruebas manuales acordadas.
- Sustituir cada paso de commit habitual por una revisión de `git diff --check` y `git status --short`.

---

## File map

### Nuevos archivos de Core

- `src/ContaNexo.Core/Models/BalanceGeneralSubcuenta.cs`: detalle interno de subcuentas consolidadas.
- `src/ContaNexo.Core/Models/BalanceGeneralCuenta.cs`: cuenta principal visible, signo presentado, impacto y diagnóstico.
- `src/ContaNexo.Core/Models/BalanceGeneralGrupo.cs`: sección estructural con cuentas y subtotal.
- `src/ContaNexo.Core/Models/BalanceGeneralResumen.cs`: resultado completo, partidas calculadas, totales y diagnósticos.
- `src/ContaNexo.Core/Calculations/BalanceGeneralCalculador.cs`: única fuente de reglas del Balance General.

### Nuevos archivos de prueba

- `tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj`: ejecutable de pruebas sin dependencias NuGet.
- `tests/ContaNexo.Core.Tests/Program.cs`: runner y casos con expectativas literales.

### Nuevos archivos de Desktop

- `src/ContaNexo.Desktop/ViewModels/BalanceGeneralViewModel.cs`: selector local, carga por ID y bindings.
- `src/ContaNexo.Desktop/Views/BalanceGeneralView.xaml`: composición visual del reporte.
- `src/ContaNexo.Desktop/Views/BalanceGeneralView.xaml.cs`: adaptación presentacional de dos columnas a una.

### Archivos existentes que se modificarán

- `ContaNexo_Desktop.slnx`: incorporar el proyecto de pruebas.
- `src/ContaNexo.Desktop/App.xaml`: registrar el DataTemplate del módulo.
- `src/ContaNexo.Desktop/ViewModels/MainWindowViewModel.cs`: construir y navegar al Balance General.
- `src/ContaNexo.Desktop/MainWindow.xaml`: añadir la opción al menú Operaciones.
- `src/ContaNexo.Desktop/ViewModels/InicioViewModel.cs`: recibir y exponer el comando.
- `src/ContaNexo.Desktop/Views/InicioView.xaml`: añadir la tarjeta del sexto paso.

No se modificarán archivos de `ContaNexo.Data`, `Database`, `EstadoResultadosCalculador` ni recursos WPF globales.

---

### Task 1: Crear el contrato del cálculo con una primera prueba RED

**Files:**

- Create: `tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj`
- Create: `tests/ContaNexo.Core.Tests/Program.cs`
- Modify: `ContaNexo_Desktop.slnx`
- Create: `src/ContaNexo.Core/Models/BalanceGeneralSubcuenta.cs`
- Create: `src/ContaNexo.Core/Models/BalanceGeneralCuenta.cs`
- Create: `src/ContaNexo.Core/Models/BalanceGeneralGrupo.cs`
- Create: `src/ContaNexo.Core/Models/BalanceGeneralResumen.cs`
- Create: `src/ContaNexo.Core/Calculations/BalanceGeneralCalculador.cs`

**Interfaces:**

- Consumes: `IEnumerable<LibroMayorCuenta>` y `EstadoResultadosCalculador.Calcular(IEnumerable<LibroMayorCuenta>)`.
- Produces: `public static BalanceGeneralResumen Calcular(IEnumerable<LibroMayorCuenta> cuentas)`.
- Produces: propiedades `ActivoCorriente`, `ActivoNoCorriente`, `PasivoCorriente`, `PasivoNoCorriente`, `CapitalContable` y `ResultadosReservas` de tipo `BalanceGeneralGrupo`.

- [ ] **Step 1: Crear el proyecto de prueba sin paquetes**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ContaNexo.Core\ContaNexo.Core.csproj" />
  </ItemGroup>
</Project>
```

Añadir a `ContaNexo_Desktop.slnx`:

```xml
<Project Path="tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj" />
```

- [ ] **Step 2: Escribir la primera prueba que falla**

En `Program.cs`, crear un runner que ejecute métodos `Action`, informe `PASS/FAIL` y termine con `Environment.ExitCode = 1` si existe algún fallo. La primera prueba debe construir literalmente:

```csharp
new LibroMayorCuenta
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
}
```

La prueba `Cuenta_principal_sin_hijas_se_clasifica_por_codigos` llamará `BalanceGeneralCalculador.Calcular` y comprobará mediante expectativas literales:

```csharp
AssertEqual(1, resumen.ActivoCorriente.Cuentas.Count);
AssertEqual("1101", resumen.ActivoCorriente.Cuentas[0].CodigoCuenta);
AssertEqual(1000m, resumen.ActivoCorriente.Cuentas[0].ImporteConsolidado);
AssertEqual(1000m, resumen.ActivoCorriente.TotalGrupo);
AssertEqual(0, resumen.PasivoCorriente.Cuentas.Count);
AssertEqual("Activo Corriente", resumen.ActivoCorriente.NombreGrupo);
```

- [ ] **Step 3: Ejecutar RED y comprobar la causa**

Run:

```powershell
dotnet run --project tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj
```

Expected: compilación fallida porque `BalanceGeneralCalculador` y los modelos aún no existen. No aceptar como RED un error en la configuración del proyecto.

- [ ] **Step 4: Crear los cuatro modelos con propiedades explícitas**

Usar estas formas públicas:

```csharp
public sealed class BalanceGeneralSubcuenta
{
    public int IdCuentaContable { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string NaturalezaCuenta { get; set; } = string.Empty;
    public int OrdenCuenta { get; set; }
    public decimal Importe { get; set; }
    public decimal ImpactoEnTotal { get; set; }
    public bool TieneSaldoContrario { get; set; }
}
```

```csharp
public sealed class BalanceGeneralCuenta
{
    public int IdCuentaContable { get; set; }
    public string CodigoCuenta { get; set; } = string.Empty;
    public string NombreCuenta { get; set; } = string.Empty;
    public string NaturalezaCuenta { get; set; } = string.Empty;
    public int OrdenCuenta { get; set; }
    public string CodigoGrupo { get; set; } = string.Empty;
    public string CodigoElemento { get; set; } = string.Empty;
    public decimal ImporteDirecto { get; set; }
    public decimal ImporteConsolidado { get; set; }
    public decimal ImpactoEnTotal { get; set; }
    public bool EsReductora { get; set; }
    public bool TieneSaldoContrarioDirecto { get; set; }
    public IReadOnlyList<BalanceGeneralSubcuenta> Subcuentas { get; set; } = [];
    public string NombrePresentacion => EsReductora ? $"(-) {NombreCuenta}" : NombreCuenta;
    public bool TieneSaldoContrario => TieneSaldoContrarioDirecto
        || Subcuentas.Any(subcuenta => subcuenta.TieneSaldoContrario);
}
```

```csharp
public sealed class BalanceGeneralGrupo
{
    public string CodigoElemento { get; set; } = string.Empty;
    public string CodigoGrupo { get; set; } = string.Empty;
    public string NombreGrupo { get; set; } = string.Empty;
    public IReadOnlyList<BalanceGeneralCuenta> Cuentas { get; set; } = [];
    public decimal TotalGrupo { get; set; }
}
```

`BalanceGeneralResumen` declarará las seis secciones no anulables, las propiedades monetarias indicadas en Task 4 y los diagnósticos indicados en Task 3.

- [ ] **Step 5: Implementar el mínimo para clasificar y totalizar una cuenta ordinaria**

Crear constantes para los pares `1/11`, `1/12`, `2/21`, `2/22`, `3/31` y `3/32`. `Calcular` materializará el enumerable una sola vez, rechazará `null` y construirá siempre los seis grupos. Para esta primera GREEN basta con cuentas principales y:

```csharp
private static decimal CalcularImporteEstado(LibroMayorCuenta cuenta)
{
    return cuenta.CodigoElemento == "1"
        ? cuenta.TotalDebe - cuenta.TotalHaber
        : cuenta.TotalHaber - cuenta.TotalDebe;
}
```

Los nombres estructurales se tomarán de las constantes del calculador, no de `NombreGrupo`.

- [ ] **Step 6: Ejecutar GREEN**

Run:

```powershell
dotnet run --project tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj
```

Expected: `1 passed, 0 failed` y exit code `0`.

- [ ] **Step 7: Revisar el checkpoint**

Run:

```powershell
git diff --check
git status --short
```

Expected: sin errores de whitespace; únicamente los archivos de esta tarea aparecen modificados o nuevos.

---

### Task 2: Consolidar principales y subcuentas sin doble conteo

**Files:**

- Modify: `tests/ContaNexo.Core.Tests/Program.cs`
- Modify: `src/ContaNexo.Core/Calculations/BalanceGeneralCalculador.cs`

**Interfaces:**

- Consumes: modelos creados en Task 1.
- Produces: `BalanceGeneralCuenta.Subcuentas`, `ImporteDirecto`, `ImporteConsolidado` e `ImpactoEnTotal` completos.

- [ ] **Step 1: Añadir pruebas RED de consolidación**

Añadir `Principal_suma_movimiento_directo_y_subcuentas_una_sola_vez` con una principal `1201` Debe `500`, hija `120101` Debe `5000` e hija `120102` Debe `10000`. Las hijas tendrán `IdCuentaPadre` igual al ID de `1201` y metadatos completos del padre. Comprobar:

```csharp
AssertEqual(500m, cuenta.ImporteDirecto);
AssertEqual(15500m, cuenta.ImporteConsolidado);
AssertEqual(15500m, cuenta.ImpactoEnTotal);
AssertEqual(2, cuenta.Subcuentas.Count);
AssertEqual(1, resumen.ActivoNoCorriente.Cuentas.Count);
```

Añadir `Principal_se_reconstruye_si_solo_la_hija_tiene_movimiento` usando solo `120101` con `CodigoCuentaPadre = "1201"`, `NombreCuentaPadre = "Propiedad, Planta y Equipo"` y `OrdenCuentaPadre = 18`. Comprobar código, nombre, orden y total `5000m`.

Añadir `Cuenta_con_importe_consolidado_cero_no_se_muestra` con Debe/Haber iguales y comprobar lista vacía y subtotal cero.

- [ ] **Step 2: Ejecutar RED**

Run el proyecto de pruebas. Expected: fallos literales porque las hijas aún aparecen separadas o no se reconstruye el padre.

- [ ] **Step 3: Implementar la consolidación**

Crear un record privado que conserve cuenta, importe, impacto y diagnóstico. Agrupar por:

```csharp
cuenta.IdCuentaPadre ?? cuenta.IdCuentaContable
```

Separar filas principales de hijas, reconstruir la referencia desde los metadatos del padre cuando sea necesario, ordenar por `OrdenCuenta` y luego `CodigoCuenta`, y lanzar:

```csharp
new InvalidOperationException(
    $"La subcuenta {cuenta.CodigoCuenta} no contiene los metadatos de su cuenta principal.")
```

Filtrar del `BalanceGeneralGrupo.Cuentas` únicamente las principales cuyo `ImporteConsolidado != 0m`. Calcular `TotalGrupo` mediante `ImpactoEnTotal`, nunca sumando otra vez las subcuentas.

- [ ] **Step 4: Ejecutar GREEN y el conjunto completo**

Run el proyecto de pruebas. Expected: `4 passed, 0 failed`.

- [ ] **Step 5: Revisar diff sin crear commit**

Ejecutar `git diff --check` y revisar que no cambió `EstadoResultadosCalculador.cs`.

---

### Task 3: Separar impacto, naturaleza y saldos contrarios

**Files:**

- Modify: `tests/ContaNexo.Core.Tests/Program.cs`
- Modify: `src/ContaNexo.Core/Calculations/BalanceGeneralCalculador.cs`
- Modify: `src/ContaNexo.Core/Models/BalanceGeneralResumen.cs`

**Interfaces:**

- Produces: `CantidadSaldosContrarios`, `TieneSaldosContrarios`, `CantidadCuentasResultadoEjercicioRegistradas` y `TieneCuentasResultadoEjercicioRegistradas`.

- [ ] **Step 1: Añadir pruebas RED para reductoras**

Añadir un caso con `1201` Debe `2400`, `1203` Debe `1500`, `1202` Haber `533` y `1204` Haber `46`, todas en `1/12`. Comprobar:

```csharp
AssertEqual(533m, depreciacion.ImporteConsolidado);
AssertEqual(-533m, depreciacion.ImpactoEnTotal);
AssertEqual("(-) Depreciación Acumulada", depreciacion.NombrePresentacion);
AssertFalse(depreciacion.TieneSaldoContrario);
AssertEqual(3321m, resumen.ActivoNoCorriente.TotalGrupo);
```

Añadir una hija `120201` con padre `1202` para demostrar que la familia se reconoce mediante `CodigoCuentaPadre`.

- [ ] **Step 2: Añadir la prueba RED crítica de 3204**

Construir `3204`, naturaleza `Deudora`, elemento `3`, grupo `32`, Debe `1000`, Haber `0`. Añadir al menos un `LibroMayorMovimiento` para representar registro real. Comprobar:

```csharp
AssertEqual(-1000m, cuenta3204.ImporteConsolidado);
AssertEqual(-1000m, cuenta3204.ImpactoEnTotal);
AssertFalse(cuenta3204.TieneSaldoContrario);
AssertEqual(0, resumen.CantidadSaldosContrarios);
AssertTrue(resumen.TieneCuentasResultadoEjercicioRegistradas);
AssertEqual(1, resumen.CantidadCuentasResultadoEjercicioRegistradas);
```

Añadir casos separados donde una cuenta `Deudora` tiene Haber mayor y una `Acreedora` tiene Debe mayor; ambos deben incrementar el diagnóstico.

- [ ] **Step 3: Ejecutar RED y comprobar que detecta la confusión anterior**

Run el proyecto de pruebas. Expected: el caso `3204` falla si el diagnóstico usa `importe < 0`, y las reductoras fallan hasta implementar su impacto especial.

- [ ] **Step 4: Implementar reglas independientes**

Usar códigos solo para el estado y las familias reductoras:

```csharp
private static bool EsReductoraActivo(LibroMayorCuenta cuenta)
{
    return cuenta.CodigoGrupo == "12"
        && (cuenta.CodigoCuenta is "1202" or "1204"
            || cuenta.CodigoCuentaPadre is "1202" or "1204");
}
```

Usar naturaleza solo para diagnóstico:

```csharp
private static bool TieneSaldoContrario(LibroMayorCuenta cuenta)
{
    return cuenta.NaturalezaCuenta switch
    {
        "Deudora" => cuenta.TotalHaber > cuenta.TotalDebe,
        "Acreedora" => cuenta.TotalDebe > cuenta.TotalHaber,
        _ => false
    };
}
```

Para reductoras, `Importe = Haber - Debe` e `Impacto = -Importe`. Para `3204`, como cuenta ordinaria patrimonial, `Importe` e `Impacto` serán `Haber - Debe`; el diagnóstico seguirá evaluando su naturaleza deudora.

Detectar registros en `3203/3204` mediante código y:

```csharp
cuenta.Movimientos.Count > 0
    || cuenta.TotalDebe != 0m
    || cuenta.TotalHaber != 0m
```

- [ ] **Step 5: Ejecutar GREEN**

Run el proyecto de pruebas. Expected: todos los casos pasan; específicamente `3204` reduce Patrimonio, no genera falso saldo contrario y sí genera la advertencia independiente.

- [ ] **Step 6: Revisar checkpoint**

Ejecutar `git diff --check` y leer el diff del calculador completo.

---

### Task 4: Integrar el resultado calculado y comprobar la ecuación

**Files:**

- Modify: `tests/ContaNexo.Core.Tests/Program.cs`
- Modify: `src/ContaNexo.Core/Calculations/BalanceGeneralCalculador.cs`
- Modify: `src/ContaNexo.Core/Models/BalanceGeneralResumen.cs`

**Interfaces:**

- Consumes: `EstadoResultadosResumen` sin copiar sus fórmulas.
- Produces: `ParticipacionTrabajadores`, `ImpuestoRenta`, `ResultadoNeto`, `TotalPasivoCorriente`, `TotalActivo`, `TotalPasivo`, `TotalPatrimonio`, `TotalPasivoPatrimonio`, `Diferencia` y `EstaCuadrado`.

- [ ] **Step 1: Añadir prueba RED de utilidad y balance cuadrado**

Usar una cuenta `1101` con Debe `1000` y una cuenta de ingreso `4101` con Haber `1000`. Las expectativas independientes son:

```csharp
AssertEqual(150m, resumen.ParticipacionTrabajadores);
AssertEqual(212.50m, resumen.ImpuestoRenta);
AssertEqual(637.50m, resumen.ResultadoNeto);
AssertEqual(362.50m, resumen.TotalPasivoCorriente);
AssertEqual(1000m, resumen.TotalActivo);
AssertEqual(637.50m, resumen.TotalPatrimonio);
AssertEqual(1000m, resumen.TotalPasivoPatrimonio);
AssertEqual(0m, resumen.Diferencia);
AssertTrue(resumen.EstaCuadrado);
```

Este caso falla si el Balance General duplica las fórmulas con un redondeo distinto o no incorpora los valores devueltos por Estado de Resultados.

- [ ] **Step 2: Añadir pruebas RED de pérdida y descuadre**

Crear ingreso `400` y gasto `1000`: resultado neto esperado `-600`, participación e IR `0`. Comprobar que el Resultado Neto reduce Patrimonio. Crear además un caso con Activo `100` y sin contrapartida; comprobar `Diferencia = 100m` y `EstaCuadrado == false`.

- [ ] **Step 3: Ejecutar RED**

Run el proyecto de pruebas. Expected: faltan las propiedades o permanecen en cero antes de integrar el resultado.

- [ ] **Step 4: Implementar reutilización y totales**

Materializar primero las cuentas y llamar exactamente una vez:

```csharp
EstadoResultadosResumen resultado = EstadoResultadosCalculador.Calcular(listaCuentas);
```

Asignar directamente sus tres importes. Calcular:

```csharp
totalPasivoCorriente = pasivoCorriente.TotalGrupo
    + resultado.ParticipacionTrabajadores
    + resultado.ImpuestoRenta;
totalActivo = activoCorriente.TotalGrupo + activoNoCorriente.TotalGrupo;
totalPasivo = totalPasivoCorriente + pasivoNoCorriente.TotalGrupo;
totalPatrimonio = capitalContable.TotalGrupo
    + resultadosReservas.TotalGrupo
    + resultado.ResultadoNeto;
totalPasivoPatrimonio = totalPasivo + totalPatrimonio;
diferencia = totalActivo - totalPasivoPatrimonio;
```

No redondear ni compensar `diferencia`. `EstaCuadrado` será una propiedad derivada de `Diferencia == 0m`.

- [ ] **Step 5: Añadir casos de validación**

Comprobar que `Calcular(null!)` lanza `ArgumentNullException` y que una hija sin metadatos de padre lanza `InvalidOperationException` con el código de la hija en el mensaje.

- [ ] **Step 6: Ejecutar GREEN completo**

Run:

```powershell
dotnet run --project tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj
```

Expected: todos los casos del runner pasan y el proceso termina con exit code `0`.

- [ ] **Step 7: Realizar mutation check mental y revisión**

Confirmar que las pruebas fallarían ante: invertir Debe/Haber, usar nombres para clasificar, sumar hijas dos veces, quitar el factor reductor, diagnosticar por signo, omitir participación/IR, omitir Resultado Neto o cambiar el signo de Diferencia. Ejecutar `git diff --check`.

---

### Task 5: Implementar el selector local y la coordinación del ViewModel

**Files:**

- Create: `src/ContaNexo.Desktop/ViewModels/BalanceGeneralViewModel.cs`

**Interfaces:**

- Consumes: `RepositorioPeriodoContable.ListarAsync(int? idEmpresa, string? estadoPeriodo)`, `RepositorioLibroMayor.ObtenerPorPeriodoAsync(int idPeriodoContable)`, `Func<Empresa?>` y `Func<PeriodoContableListado?>`.
- Produces: `Task CargarAsync()` y `Task CargarPeriodoAsync(int idPeriodoContable)`.
- Produces: `ObservableCollection<BalanceGeneralPeriodoItemViewModel> Periodos`, `PeriodoSeleccionado`, `PeriodoConsultado`, `ConsultarPeriodoCommand` y propiedades de resumen/estado.

- [ ] **Step 1: Confirmar la compilación base antes del cambio Desktop**

Ejecutar antes de crear el ViewModel:

```powershell
dotnet build ContaNexo_Desktop.slnx
```

Expected: exit code `0`. Esta evidencia separa cualquier fallo nuevo de los problemas preexistentes y no abre la aplicación.

- [ ] **Step 2: Crear el item de selector local**

Dentro del mismo archivo, siguiendo `PeriodoContableItemViewModel`, exponer:

```csharp
public sealed class BalanceGeneralPeriodoItemViewModel
{
    public PeriodoContableListado Periodo { get; }
    public int IdPeriodoContable => Periodo.IdPeriodoContable;
    public string NombrePeriodo => Periodo.NombrePeriodo;
    public DateTime FechaInicioPeriodo => Periodo.FechaInicioPeriodo;
    public DateTime FechaFinPeriodo => Periodo.FechaFinPeriodo;
    public string EstadoPeriodo => Periodo.EstadoPeriodo;
    public bool EsCerrado => EstadoPeriodo == "Cerrado";
}
```

- [ ] **Step 3: Crear dependencias y selección**

El constructor será:

```csharp
public BalanceGeneralViewModel(
    RepositorioPeriodoContable repositorioPeriodoContable,
    RepositorioLibroMayor repositorioLibroMayor,
    Func<Empresa?> obtenerEmpresaActiva,
    Func<PeriodoContableListado?> obtenerPeriodoActivo)
```

`PeriodoSeleccionado` notificará `TienePeriodoSeleccionado`, limpiará `Resumen`, `PeriodoConsultado` y errores para no dejar datos de otro período bajo una selección nueva, y llamará `ConsultarPeriodoCommand.NotificarPuedeEjecutar()`.

- [ ] **Step 4: Implementar carga inicial sin cambiar estado global**

`CargarAsync` limpiará lista, selección y resumen; obtendrá la empresa; llamará:

```csharp
await _repositorioPeriodoContable.ListarAsync(empresa.IdEmpresa)
```

No pasará filtro de estado. Si el período activo existe, pertenece a la empresa y aparece en la lista, asignará el item correspondiente y ejecutará `CargarPeriodoAsync(periodoActivo.IdPeriodoContable)`. Sin período activo dejará el selector pendiente.

No almacenar ni recibir `Action<PeriodoContableListado?>`; así el módulo no podrá cambiar accidentalmente el período global.

- [ ] **Step 5: Implementar consulta explícita por ID**

`ConsultarPeriodoCommand` se construirá con `ComandoAsync(ConsultarPeriodoSeleccionadoAsync, PuedeConsultarPeriodo)`. `CargarPeriodoAsync(int idPeriodoContable)` validará que el ID pertenezca a la lista local, consultará el Libro Mayor usando exactamente ese argumento, calculará el resumen y solo entonces asignará `PeriodoConsultado`.

Exponer las seis secciones, partidas calculadas, totales y diagnósticos mediante propiedades que devuelvan cero/listas vacías cuando `Resumen` sea nulo. Mantener estados `EstaCargando`, `MensajeError`, `TieneError`, `TieneEmpresa`, `TienePeriodos`, `TienePeriodoConsultado`.

- [ ] **Step 6: Compilar GREEN del proyecto Desktop**

Run:

```powershell
dotnet build src/ContaNexo.Desktop/ContaNexo.Desktop.csproj
```

Expected: exit code `0`. El ViewModel todavía no estará registrado en navegación, por lo que esta compilación valida el nuevo tipo de manera aislada.

---

### Task 6: Crear la vista responsive del Balance General

**Files:**

- Create: `src/ContaNexo.Desktop/Views/BalanceGeneralView.xaml`
- Create: `src/ContaNexo.Desktop/Views/BalanceGeneralView.xaml.cs`

**Interfaces:**

- Consumes: todas las propiedades de `BalanceGeneralViewModel`, `BalanceGeneralGrupo` y `BalanceGeneralCuenta`.
- Produces: reporte visual sin reglas contables.

- [ ] **Step 1: Crear recursos locales de presentación**

Definir estilos locales para:

- paneles de estado sin empresa, sin período, carga y error;
- tarjeta de sección;
- fila de cuenta, colapsada cuando `ImporteConsolidado == 0` como defensa visual;
- etiqueta `Saldo contrario` controlada por `TieneSaldoContrario`;
- advertencia separada controlada por `TieneCuentasResultadoEjercicioRegistradas`;
- estado cuadrado y descuadrado.

Todos deben reutilizar brushes y estilos existentes; no añadir recursos globales.

- [ ] **Step 2: Construir el encabezado y selector**

Usar `REPORTE FINANCIERO`, `Balance General` y una descripción a la izquierda. A la derecha, un `ComboBox` enlazado a `Periodos` y `PeriodoSeleccionado`, con `ItemTemplate` que muestre `NombrePeriodo`, fechas y `EstadoPeriodo` textualmente; usar trigger verde para `Abierto` y estilo neutro/violeta para `Cerrado`. Añadir botón:

```xml
<Button Command="{Binding ConsultarPeriodoCommand}"
        Content="Consultar"
        Style="{StaticResource PrimaryActionButtonStyle}" />
```

- [ ] **Step 3: Construir las secciones**

Crear un `DataTemplate` de grupo que muestre siempre el título y subtotal, y un `ItemsControl` para las cuentas. Cada fila enlazará `CodigoCuenta`, `NombrePresentacion`, `ImporteConsolidado` y `TieneSaldoContrario`.

En Pasivo Corriente, después de cuentas reales, añadir filas calculadas para participación e IR y enlazar el subtotal a `TotalPasivoCorriente`. En Patrimonio, añadir la fila separada `Resultado neto del período` y enlazar Total Patrimonio al resumen completo.

- [ ] **Step 4: Construir la comprobación**

Mostrar `TotalActivo`, `TotalPasivoPatrimonio` y `Diferencia`. Usar triggers sobre `EstaCuadrado` para alternar exactamente:

```text
✓ Balance cuadrado
⚠ El balance no está cuadrado
```

No ocultar la diferencia cuando sea distinta de cero.

- [ ] **Step 5: Implementar responsividad exclusivamente visual**

Nombrar el grid central y sus dos paneles. En `BalanceGeneralView.xaml.cs`, manejar `SizeChanged` con umbral `980d`: por encima, dos columnas `* / 24 / *`; por debajo, una columna, Activo en fila 0 y Pasivo/Patrimonio en fila 1. El code-behind solo llamará `Grid.SetRow`, `Grid.SetColumn` y ajustará spans/anchos; no accederá al ViewModel ni calculará importes.

El `ScrollViewer` raíz tendrá `HorizontalScrollBarVisibility="Disabled"` y `VerticalScrollBarVisibility="Auto"`.

- [ ] **Step 6: Compilar XAML**

Run:

```powershell
dotnet build src/ContaNexo.Desktop/ContaNexo.Desktop.csproj
```

Expected: exit code `0`, sin abrir WPF.

- [ ] **Step 7: Revisar bindings y diff**

Comparar cada binding con una propiedad pública real del ViewModel/modelo. Ejecutar `git diff --check`.

---

### Task 7: Integrar navegación e Inicio

**Files:**

- Modify: `src/ContaNexo.Desktop/App.xaml`
- Modify: `src/ContaNexo.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ContaNexo.Desktop/MainWindow.xaml`
- Modify: `src/ContaNexo.Desktop/ViewModels/InicioViewModel.cs`
- Modify: `src/ContaNexo.Desktop/Views/InicioView.xaml`

**Interfaces:**

- Consumes: constructor y `CargarAsync()` de `BalanceGeneralViewModel`.
- Produces: `NavegarBalanceGeneralCommand` disponible desde menú e Inicio.

- [ ] **Step 1: Registrar el DataTemplate**

Añadir en `App.xaml` junto a Estado de Resultados:

```xml
<DataTemplate DataType="{x:Type viewModels:BalanceGeneralViewModel}">
    <views:BalanceGeneralView />
</DataTemplate>
```

- [ ] **Step 2: Construir y navegar desde MainWindowViewModel**

Añadir campo, propiedad de comando y construcción:

```csharp
_balanceGeneralViewModel = new BalanceGeneralViewModel(
    repositorioPeriodoContable,
    repositorioLibroMayor,
    () => _empresaActiva,
    () => PeriodoActivo);
NavegarBalanceGeneralCommand = new ComandoAsync(NavegarABalanceGeneralAsync);
```

El método será:

```csharp
private async Task NavegarABalanceGeneralAsync()
{
    VistaActual = _balanceGeneralViewModel;
    await _balanceGeneralViewModel.CargarAsync();
}
```

- [ ] **Step 3: Añadir la opción de Operaciones**

Después de Estado de Resultados, añadir un `MenuItem` con `Header="Balance General"`, `OperationsMenuItemStyle` y binding al nuevo comando mediante `PlacementTarget.DataContext`, igual que los ítems existentes.

- [ ] **Step 4: Extender InicioViewModel sin crear comandos duplicados**

Agregar `ComandoAsync navegarBalanceGeneralCommand` al constructor, asignarlo directamente y exponer `NavegarBalanceGeneralCommand`. Actualizar únicamente su llamada desde `MainWindowViewModel`.

- [ ] **Step 5: Añadir la tarjeta del sexto paso**

Copiar la estructura visual de las tarjetas existentes, enlazar el nuevo comando y usar:

```text
BG
6
Balance General
Presenta la situación financiera del período mediante Activo, Pasivo y Patrimonio.
SITUACIÓN FINANCIERA  →
```

Mantener `ModuleCardStyle` y la paleta existente.

- [ ] **Step 6: Compilar la solución completa**

Run:

```powershell
dotnet build ContaNexo_Desktop.slnx
```

Expected: exit code `0`; no iniciar el ejecutable.

- [ ] **Step 7: Revisar alcance**

Run:

```powershell
git diff --name-status
git diff --check
```

Expected: ningún archivo de `Database`, `ContaNexo.Data`, Períodos Contables o Estado de Resultados aparece modificado.

---

### Task 8: Verificación final y entrega

**Files:**

- Verify only: todos los archivos anteriores.

**Interfaces:**

- Produces: evidencia fresca de pruebas, compilación y alcance.

- [ ] **Step 1: Ejecutar pruebas Core desde cero**

Run:

```powershell
dotnet run --project tests/ContaNexo.Core.Tests/ContaNexo.Core.Tests.csproj
```

Registrar cantidad exacta de casos, fallos y exit code.

- [ ] **Step 2: Compilar la solución completa sin abrir WPF**

Run:

```powershell
dotnet build ContaNexo_Desktop.slnx
```

Registrar warnings, errores y exit code. No ejecutar `dotnet run` sobre el proyecto Desktop.

- [ ] **Step 3: Auditar que no hubo cambios SQL ni escrituras contables**

Run:

```powershell
git diff --name-status
git diff -- Database src/ContaNexo.Data
rg -n "3203|3204|INSERT|UPDATE|DELETE" src/ContaNexo.Core/Calculations/BalanceGeneralCalculador.cs src/ContaNexo.Desktop/ViewModels/BalanceGeneralViewModel.cs
```

Confirmar por lectura que las coincidencias de `3203/3204` solo diagnostican y que no existen comandos de escritura.

- [ ] **Step 4: Auditar la selección local**

Buscar referencias a `EstablecerPeriodoActivo` y confirmar que el nuevo ViewModel no recibe ni invoca ese callback. Confirmar que `CargarPeriodoAsync` pasa su argumento directamente a `ObtenerPorPeriodoAsync`.

- [ ] **Step 5: Preparar inventario exacto**

Usar `git status --short` y `git diff --name-status` para separar archivos creados y modificados. Explicar una línea por cada archivo nuevo.

- [ ] **Step 6: Entregar checklist manual WPF**

Solicitar al usuario verificar manualmente:

1. navegación desde Inicio y Operaciones;
2. selección inicial del período activo;
3. selector vacío cuando no existe período activo;
4. consulta de un período cerrado sin cambiar el período global;
5. recarga de un período abierto después de nuevos asientos;
6. consolidación visible de principales con hijas;
7. ocultamiento de filas en cero;
8. presentación `(-)` y resta de `1202/1204`;
9. advertencia independiente al registrar datos en `3203/3204`;
10. mensajes cuadrado/descuadrado y diferencia;
11. diseño maximizado y apilado en ventana reducida;
12. ausencia de scroll horizontal.

- [ ] **Step 7: Confirmar restricciones**

Informar explícitamente: sin tabla/SP propio, sin movimientos generados, sin cambios a Estado de Resultados, sin commit/push y aplicación WPF no abierta.
