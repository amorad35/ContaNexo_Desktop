# Balance General de ContaNexo Desktop

## Objetivo

Implementar el Balance General como un estado financiero calculado a partir del Libro Mayor existente, con consulta explícita por `idPeriodoContable`, selector local de períodos abiertos y cerrados, presentación responsive en WPF y comprobación visible de la ecuación contable.

El módulo será exclusivamente de lectura. No creará tablas, Stored Procedures, asientos ni persistencia propia.

## Alcance

La implementación comprende:

- cálculo de Activo, Pasivo y Patrimonio en `ContaNexo.Core`;
- consolidación de cuentas principales y subcuentas;
- reutilización de `EstadoResultadosCalculador` para participación, Impuesto a la Renta y resultado neto;
- tratamiento contable y visual de las reductoras `1202` y `1204`;
- diagnóstico de saldos contrarios y de movimientos existentes en `3203/3204`;
- selector local de períodos abiertos y cerrados;
- ViewModel, View e integración mínima con la navegación y la pantalla de Inicio;
- pruebas automatizadas de la lógica de Core;
- compilación de la solución sin abrir automáticamente la aplicación.

Quedan fuera del alcance el cierre contable, la creación o reapertura de períodos, los asientos de cierre, las exportaciones, los PDF, las tasas configurables y cualquier modificación funcional de otros módulos.

## Estado real del repositorio

`RepositorioLibroMayor.ObtenerPorPeriodoAsync(int idPeriodoContable)` ejecuta `dbo.SP_LibroMayor_ObtenerPorPeriodo` y devuelve `LibroMayorCuenta` con clasificación por elemento y grupo, metadatos de la cuenta padre, totales Debe/Haber y movimientos.

El procedimiento solo consulta asientos `Registrado` y no restringe el estado del período. Por ello ya admite períodos abiertos y cerrados sin requerir un SP de Balance General.

`EstadoResultadosCalculador.Calcular(IEnumerable<LibroMayorCuenta>)` ya contiene las tasas estáticas, el redondeo `MidpointRounding.AwayFromZero` y las reglas de utilidad o pérdida. El Balance General lo invocará y no duplicará esas fórmulas.

`RepositorioPeriodoContable.ListarAsync(int? idEmpresa, string? estadoPeriodo)` permite listar todos los períodos de una empresa cuando el filtro de estado es nulo. La pantalla actual de Períodos Contables solo permite activar períodos abiertos, por lo que el Balance General tendrá una selección independiente.

El catálogo inicial confirma:

- `1202 Depreciación Acumulada`, naturaleza acreedora, con subcuentas;
- `1204 Amortización Acumulada`, naturaleza acreedora;
- `3203 Utilidad del Ejercicio`, naturaleza acreedora;
- `3204 Pérdida del Ejercicio`, naturaleza deudora.

## Arquitectura

El flujo será:

```text
RepositorioPeriodoContable ──> BalanceGeneralViewModel
                                      │
RepositorioLibroMayor ────────────────┤
                                      v
                           BalanceGeneralCalculador
                                      │
                                      ├──> EstadoResultadosCalculador
                                      v
                              BalanceGeneralView
```

No se modificará `EstadoResultadosCalculador`: el nuevo calculador lo consumirá como API existente. La consolidación del Balance General permanecerá aislada porque añade reglas específicas de elementos reales, partidas reductoras e impacto en totales que no pertenecen al Estado de Resultados.

## Modelo de Core

Se crearán modelos enfocados en el Balance General:

- `BalanceGeneralResumen`: secciones, totales, diferencia, resultado calculado y diagnósticos;
- `BalanceGeneralGrupo`: código, nombre, cuentas y subtotal;
- `BalanceGeneralCuenta`: cuenta principal, importe directo, importe consolidado, impacto en el subtotal, condición reductora, naturaleza, diagnóstico y subcuentas;
- `BalanceGeneralSubcuenta`: detalle interno de cada subcuenta, naturaleza, importe e impacto.

Las seis secciones siempre existirán en el resumen, incluso cuando no tengan cuentas visibles:

| Elemento | Grupo | Sección |
|---|---|---|
| `1` | `11` | Activo Corriente |
| `1` | `12` | Activo No Corriente |
| `2` | `21` | Pasivo Corriente |
| `2` | `22` | Pasivo No Corriente |
| `3` | `31` | Capital Contable |
| `3` | `32` | Resultados y Reservas |

La clasificación utilizará únicamente `CodigoElemento` y `CodigoGrupo`. Los IDs se usarán solo para reconstruir la relación padre-hija que ya entrega el Libro Mayor.

## Consolidación de cuentas

Las filas se agruparán mediante:

```text
IdCuentaPadre ?? IdCuentaContable
```

Para cada cuenta principal:

```text
importe consolidado = importe directo de la principal
                    + suma de importes de las subcuentas
```

Cuando el SP no devuelva la principal porque solo sus hijas tuvieron movimientos, se reconstruirán código, nombre y orden mediante `CodigoCuentaPadre`, `NombreCuentaPadre` y `OrdenCuentaPadre`, igual que en Estado de Resultados. La ausencia de esos metadatos producirá una excepción explícita.

Las subcuentas permanecerán en el modelo, pero la primera versión de la vista mostrará únicamente cuentas principales. Una cuenta principal sin hijas se mostrará normalmente. Las cuentas cuyo importe consolidado sea exactamente cero no aparecerán como filas y no afectarán la existencia de la sección o su subtotal.

## Impacto, naturaleza y saldos contrarios

El cálculo distinguirá tres conceptos independientes:

1. el signo con el que una cuenta impacta el estado financiero;
2. la naturaleza contable normal de la cuenta;
3. la existencia de un verdadero saldo contrario.

El impacto en el Balance General se calculará por elemento:

```text
Activo:              TotalDebe - TotalHaber
Pasivo y Patrimonio: TotalHaber - TotalDebe
```

No se aplicará valor absoluto. Un impacto negativo se conservará, pero no constituirá por sí mismo un saldo contrario.

El diagnóstico evaluará las cuentas del Libro Mayor antes de consolidarlas, incluidas las subcuentas, y utilizará exclusivamente `NaturalezaCuenta` para decidir si la orientación observada es contraria:

```text
Naturaleza Deudora:   saldo contrario si TotalHaber > TotalDebe
Naturaleza Acreedora: saldo contrario si TotalDebe > TotalHaber
```

La naturaleza no se utilizará para clasificar cuentas dentro del estado financiero ni para cambiar el signo de su impacto. Esa clasificación seguirá dependiendo exclusivamente de `CodigoElemento` y `CodigoGrupo`.

Por ejemplo, `3204 Pérdida del Ejercicio` tiene naturaleza deudora. Con Debe `1.000` y Haber `0`, su impacto patrimonial será `-1.000` conforme a la fórmula de Patrimonio, reducirá el total y no generará un diagnóstico de saldo contrario.

## Cuentas reductoras del Activo

Se reconocerán por código las familias `1202` y `1204`. Una fila pertenece a una de estas familias si su propio código es `1202`/`1204` o si `CodigoCuentaPadre` tiene uno de esos valores.

Para ellas:

```text
importe presentado = TotalHaber - TotalDebe
impacto en Activo   = -importe presentado
```

Con un saldo acreedor normal, la vista mostrará `(-)` antes del nombre y un importe positivo, mientras el subtotal de Activo No Corriente lo restará. Como `1202` y `1204` tienen naturaleza acreedora, este caso no generará diagnóstico. Si una reductora presenta saldo deudor, su importe presentado será negativo, conservará ese signo y sí generará diagnóstico de saldo contrario por la comparación con `NaturalezaCuenta`; su impacto matemático no será corregido artificialmente.

## Resultado del período

`BalanceGeneralCalculador` invocará una vez:

```csharp
EstadoResultadosResumen resultado = EstadoResultadosCalculador.Calcular(cuentas);
```

Del resultado reutilizará:

- `ParticipacionTrabajadores`;
- `ImpuestoRenta`;
- `ResultadoNeto`;
- las tasas solo para etiquetas si la vista las necesita.

Cuando exista utilidad, participación e IR se agregarán como partidas calculadas al Pasivo Corriente. En pérdida o resultado cero ambos valores serán cero por las reglas ya implementadas en Estado de Resultados.

El Resultado Neto se presentará como partida calculada separada dentro de Patrimonio. Una pérdida conservará su signo negativo y reducirá el total patrimonial.

## Saldos reales en 3203 y 3204

Las cuentas `3203` y `3204` no se excluirán del Libro Mayor: cualquier saldo real seguirá formando parte del grupo `32` y de sus totales. El módulo no eliminará, modificará ni compensará esos valores.

Se considerará que existen registros reales cuando una cuenta `3203` o `3204` tenga movimientos, `TotalDebe` distinto de cero o `TotalHaber` distinto de cero. El resumen expondrá un diagnóstico específico con la cantidad de cuentas detectadas.

La vista mostrará una advertencia clara indicando que existen saldos registrados en cuentas de resultado del ejercicio y que la proyección de Resultado Neto podría superponerse con ellos. No se intentará cuadrar el Balance ni introducir lógica de Cierre para resolverlo.

Este diagnóstico será independiente del diagnóstico de saldo contrario. En particular, un saldo deudor normal de `3204` activará la advertencia de posible superposición, pero no la advertencia de saldo contrario.

## Totales y ecuación contable

```text
Total Activo     = Activo Corriente + Activo No Corriente
Total Pasivo     = Pasivo Corriente + Pasivo No Corriente
Total Patrimonio = Capital Contable
                 + Resultados y Reservas
                 + Resultado Neto
Total Pasivo y Patrimonio = Total Pasivo + Total Patrimonio
Diferencia = Total Activo - Total Pasivo y Patrimonio
```

El subtotal de Pasivo Corriente incluye las partidas calculadas de participación e IR.

`EstaCuadrado` será verdadero únicamente cuando `Diferencia == 0m`. No se modificarán importes ni se introducirán redondeos compensatorios para forzar la igualdad.

## Selector local de período

`BalanceGeneralViewModel` dependerá de:

- `RepositorioPeriodoContable`;
- `RepositorioLibroMayor`;
- `Func<Empresa?>` para conocer la empresa actual;
- `Func<PeriodoContableListado?>` para leer, sin modificar, el período activo global.

Al navegar al módulo:

1. Obtendrá la empresa actual.
2. Listará sus períodos con `ListarAsync(idEmpresa)` y filtro de estado nulo.
3. Si existe un período activo perteneciente a esa empresa, lo seleccionará y consultará inmediatamente.
4. Si no existe período activo, dejará la selección vacía aunque existan períodos cerrados.
5. Si la empresa no está configurada, mostrará un estado informativo y no consultará períodos.

Cada elemento del selector presentará nombre, rango de fechas y estado. Los estados Abierto y Cerrado se diferenciarán mediante texto y color, sin depender exclusivamente del color.

El comando `Consultar` requerirá una selección y llamará internamente al flujo de carga mediante el `IdPeriodoContable` seleccionado. Cambiar o consultar una selección local no invocará el callback que establece el período activo global.

Tanto períodos abiertos como cerrados usarán exclusivamente métodos de lectura. Un período abierto reflejará los nuevos asientos al volver a consultarlo; uno cerrado funcionará como consulta histórica.

## ViewModel y manejo de estados

El ViewModel coordinará:

- listado y selección local de períodos;
- carga asíncrona del Libro Mayor por ID;
- llamada al calculador;
- exposición del resumen y propiedades para bindings;
- estados sin empresa, sin selección, cargando y error.

Las excepciones `InvalidOperationException` de los repositorios se mostrarán con su mensaje. Las excepciones no previstas producirán un mensaje genérico del Balance General. Al cambiar de empresa o volver a navegar, se reconstruirá la lista y se descartará cualquier selección local que ya no pertenezca a la empresa actual.

La View no contendrá reglas contables.

## Diseño WPF

La vista reutilizará los recursos globales existentes (`EyebrowTextStyle`, `PageTitleTextStyle`, `SectionTitleTextStyle`, `BodyTextStyle`, colores, tarjetas y mensajes) y definirá estilos locales solo para filas y secciones del reporte.

La composición será:

- encabezado `REPORTE FINANCIERO / Balance General` y descripción;
- selector local de período y botón `Consultar` en la zona derecha;
- advertencias de saldo contrario y de `3203/3204`;
- columna izquierda con Activo Corriente, Activo No Corriente y Total Activo;
- columna derecha con Pasivo Corriente, Pasivo No Corriente, Total Pasivo, Capital Contable, Resultados y Reservas y Total Patrimonio;
- bloque final a ancho completo con Total Activo, Total Pasivo + Patrimonio, Diferencia y estado visual.

La vista usará scroll vertical y deshabilitará el horizontal. En ancho normal mostrará dos columnas equivalentes; en ancho reducido las apilará verticalmente mediante lógica exclusivamente presentacional en el code-behind de la View. No cambiarán tamaños de fuente para simular responsividad.

Cuando la diferencia sea cero se mostrará `✓ Balance cuadrado` con tratamiento positivo. En caso contrario se mostrará `⚠ El balance no está cuadrado` y la diferencia con tratamiento de advertencia/error.

La imagen de referencia `CN_REF_06_BALANCE_GENERAL(2).png` no estuvo disponible en los adjuntos recibidos. La adaptación visual se basará en la estructura indicada por el usuario y en `EstadoResultadosView.xaml` como referencia interna real.

## Integración mínima

Se añadirán:

- un `DataTemplate` para `BalanceGeneralViewModel` en `App.xaml`;
- construcción, comando y método de navegación en `MainWindowViewModel`;
- una opción `Balance General` dentro del menú Operaciones;
- el comando correspondiente en `InicioViewModel`;
- una tarjeta de sexto paso en `InicioView`.

No se alterará el comportamiento de Períodos Contables, Estado de Resultados ni los módulos existentes.

## Estrategia de pruebas

Se añadirá un proyecto ejecutable de pruebas de Core sin paquetes NuGet externos. Usará aserciones deterministas y devolverá código de salida distinto de cero ante un fallo.

Los casos cubrirán:

- cuenta principal sin subcuentas;
- principal con movimiento directo y subcuentas sin doble conteo;
- reconstrucción de principal cuando solo existen movimientos en hijas;
- ocultamiento de importes consolidados en cero;
- clasificación exclusiva por códigos de elemento y grupo;
- signos de Activo, Pasivo y Patrimonio;
- reductoras `1202` y `1204`, incluidas sus hijas;
- diagnóstico de saldo contrario basado en `NaturalezaCuenta`;
- saldo deudor normal en `3204` con impacto patrimonial negativo y sin falso diagnóstico de saldo contrario;
- saldo acreedor normal y saldo deudor contrario en las reductoras `1202/1204`;
- utilidad, pérdida, participación, IR y redondeo obtenidos desde Estado de Resultados;
- incorporación del Resultado Neto al Patrimonio;
- diagnóstico de movimientos o saldos reales en `3203/3204`;
- balance cuadrado y descuadrado;
- validación de argumento nulo y metadatos incompletos de una cuenta padre.

La verificación final ejecutará el proyecto de pruebas y compilará `ContaNexo_Desktop.slnx`. No abrirá la aplicación WPF.

## Restricciones de entrega

- No hacer commit, push, reset ni restore de Git.
- No abrir automáticamente la aplicación.
- No crear tabla ni Stored Procedure de Balance General.
- No agregar paquetes NuGet.
- No escribir movimientos en `3203/3204` ni en ninguna otra cuenta.
- No modificar módulos fuera de la integración mínima descrita.
- Enumerar al finalizar todos los archivos creados y modificados y explicar la responsabilidad de cada archivo nuevo.
