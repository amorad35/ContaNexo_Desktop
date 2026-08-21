# ContaNexoDB - primera versión

`ContaNexoDB.sql` crea desde cero la base `ContaNexoDB` para SQL Server Express o LocalDB. El script se detiene si la base ya existe para evitar modificar o mezclar accidentalmente una instalación previa.

## Estructura

- `Empresa` es la raíz organizativa.
- `PeriodoContable` pertenece a una empresa y valida su rango de fechas y estado.
- `ElementoContable` y `GrupoContable` representan los dos primeros niveles del catálogo.
- `CuentaContable` implementa el resto de la jerarquía mediante la autorreferencia nullable `idCuentaPadre`; no se creó una tabla de subcuentas.
- `DetalleCuenta` mantiene, como relación uno a cero/uno, la descripción y dinámicas educativas de una cuenta.
- `Asiento` pertenece a un período. Los asientos normales y de ajuste comparten esta tabla y se distinguen mediante `tipoAsiento`.
- `DetalleAsiento` contiene las partidas y exige que cada línea tenga un valor positivo únicamente en Debe o únicamente en Haber.

Las claves foráneas no usan eliminación en cascada: así se evita borrar accidentalmente historia contable. Los índices cubren las relaciones y rutas de consulta más habituales. El Libro Mayor, los balances y los estados financieros no se almacenan, pues se derivarán de los asientos, sus detalles y las cuentas. Tampoco se almacenan totales de asiento.

## Scripts y orden de ejecución

1. `Database/ContaNexoDB.sql` crea desde cero `ContaNexoDB`, sus ocho tablas, claves, restricciones, índices, cinco elementos y once grupos.
2. `Database/SeedCatalogoCuentas.sql` carga el catálogo oficial sobre una `ContaNexoDB` recién creada. Inserta 90 cuentas y 69 detalles educativos.
3. `Database/StoredProcedures.sql` crea o actualiza los procedimientos almacenados del módulo `CuentaContable`.

Los scripts de datos obtienen las relaciones por códigos y no dependen de valores `IDENTITY` predeterminados. El seed usa `XACT_ABORT`, `TRY/CATCH` y una transacción; si `CuentaContable` o `DetalleCuenta` ya contienen datos se detiene sin borrar, combinar ni sobrescribir información.

`ContaNexoDBv1` pertenece al antiguo proyecto web. No forma parte de ContaNexo Desktop y ninguno de estos scripts debe ejecutarse, consultarse ni adaptarse contra esa base.

## Catálogo cargado

El seed conserva la jerarquía `Elemento → Grupo → Cuenta → Subcuenta` mediante `CuentaContable.idCuentaPadre`, sin crear una tabla de subcuentas:

- 90 cuentas en total: 67 de nivel principal y 23 subcuentas.
- 7 cuentas padre con `permiteMovimientoCuenta = 0`.
- 83 cuentas hoja con `permiteMovimientoCuenta = 1`.
- 69 filas en `DetalleCuenta`, correspondientes a las fichas que sí contienen descripción y dinámicas propias en el PDF.
- Las subcuentas sin ficha independiente no heredan ni duplican el texto educativo de su padre.
- `ordenCuenta` sigue el orden Activo, Pasivo, Patrimonio, Ingresos y Gastos, colocando cada subcuenta inmediatamente después de su padre.

## Decisiones canónicas aplicadas

- Se usan `1112 Anticipos Sueldos`, `1113 Anticipos a Proveedores`, `1114 Arriendos Prepagados` y `1115 Otros Activos Corrientes`.
- Préstamos Bancarios corriente usa `2108`; no se carga `2109`.
- Se usan `5105 Papelería y Útiles de Oficina`, `5106 Depreciaciones` y `5202 Comisiones en Ventas`; no se cargan `6105`, `6106` ni `6202`.
- Las denominaciones oficiales de esta versión son `31 Capital Contable`, `32 Resultados y Reservas`, `42 Ingresos No Operacionales`, `1203 Intangibles` y `5305 Otros Gastos No Operacionales`.
- Se incluyen las subcuentas detalladas `120107`, `120201` a `120203`, `120301` a `120303`, `210501`, `210502`, `211001`, `211002`, `420101`, `420102` y `5110.01` a `5110.04`.
- Los códigos `5110.01` a `5110.04` conservan el punto mostrado en el catálogo.

En cuanto a naturaleza contable, las cuentas de Activo son `Deudora`, mientras `1202 Depreciación Acumulada`, sus subcuentas y `1204 Amortización Acumulada` son `Acreedora`. Pasivo, Patrimonio e Ingresos son `Acreedora`; Gastos es `Deudora`. Como excepción adicional inequívoca de naturaleza, `3204 Pérdida del Ejercicio` es `Deudora`, coherente con la dinámica de su ficha: la pérdida aumenta al débito y disminuye al crédito.

La configuración inicial sigue una regla uniforme: toda cuenta con una o más hijas tiene `permiteMovimientoCuenta = 0`, y toda cuenta sin hijas tiene `permiteMovimientoCuenta = 1`. Por ello, `1202 Depreciación Acumulada` conserva `120201`, `120202` y `120203` como hijas y queda inicialmente sin movimiento directo. Posteriormente, la aplicación podrá habilitar manualmente cualquier cuenta padre estableciendo `permiteMovimientoCuenta = 1`.

## Procedimientos almacenados

ContaNexo utiliza procedimientos almacenados como mecanismo principal de acceso y modificación en SQL Server. La convención de nombres es `SP_Entidad_Accion`. `StoredProcedures.sql` implementa:

### CuentaContable

- `SP_CuentaContable_Listar`: devuelve el catálogo completo, sus grupos, elementos, padre e indicador calculado `tieneHijas`.
- `SP_CuentaContable_ListarMovimiento`: devuelve cuentas activas configuradas para movimiento directo, tengan o no hijas.
- `SP_CuentaContable_ObtenerPorId`: devuelve una cuenta con jerarquía y detalle educativo.
- `SP_CuentaContable_Crear`: crea cuentas y deshabilita automáticamente a la padre cuando recibe su primera hija.
- `SP_CuentaContable_Actualizar`: modifica identidad contable y jerarquía, evita ciclos y aplica la regla de primera hija al reasignar.
- `SP_CuentaContable_ConfigurarMovimiento`: permite habilitar o deshabilitar manualmente cualquier cuenta, incluidas las cuentas padre.
- `SP_CuentaContable_CambiarEstado`: realiza activación o desactivación lógica sin cascadas.

La desactivación automática de una padre ocurre solo al pasar de cero a una hija. Si ya tenía hijas y fue habilitada manualmente, agregar otra hija no cambia esa configuración. Cuando una cuenta deja a su antigua padre sin hijas, esa padre tampoco se habilita automáticamente.

`permiteMovimientoCuenta` controla la selección para asientos directos. No determina si una cuenta puede mostrarse como agrupadora en Estado de Resultados o Balance General: esa presentación se calculará posteriormente a partir de la jerarquía real.

### PeriodoContable

- `SP_PeriodoContable_Listar`: consulta períodos y permite filtrar opcionalmente por empresa y estado.
- `SP_PeriodoContable_ObtenerPorId`: devuelve un período con información básica de su empresa.
- `SP_PeriodoContable_Crear`: crea siempre períodos abiertos y rechaza rangos solapados para la misma empresa.
- `SP_PeriodoContable_Actualizar`: modifica nombre y fechas únicamente mientras el período esté abierto.
- `SP_PeriodoContable_Cerrar`: cambia un período abierto a cerrado y registra la fecha de cierre.

ContaNexo implementa actualmente solo el modo contable formal. Una empresa puede tener varios períodos, pero sus rangos de fechas son inclusivos y no pueden solaparse total ni parcialmente, incluso si un período ya está cerrado. Los períodos consecutivos sí están permitidos; por ejemplo, un período que termina el 31 de enero puede ser seguido por otro que inicia el 1 de febrero.

Los períodos cerrados se conservan como historial y por ahora no existe reapertura. El modo de Actividades o Prácticas, incluidos posibles períodos académicos repetibles, queda como ampliación futura separada de `PeriodoContable`.

El cierre actual implementa únicamente el cambio básico de estado y la fecha de cierre. Las validaciones contables previas relacionadas con Libro Diario, asientos cuadrados, Libro Mayor, Balance de Sumas y Saldos, Estado de Resultados y Balance General se incorporarán cuando existan esos módulos.

## Decisiones y pendientes del esquema

- `estadoAsiento` queda como `VARCHAR(10) NOT NULL`, pero sin restricción `CHECK`: todavía no se han definido sus valores permitidos.
- El equilibrio `SUM(debeDetalle) = SUM(haberDetalle)` se validará posteriormente en la aplicación antes de confirmar el asiento.
- El seed comprueba que cada hija tenga un padre del mismo grupo, aunque el esquema no impone esa regla mediante una restricción entre tablas.

## Registro histórico de discrepancias del catálogo

Se auditó el PDF completo de 42 páginas. Las referencias usan el número físico de página. Todas las discrepancias que afectaban esta carga quedaron resueltas mediante decisiones explícitas del proyecto:

| Conflicto original | Ubicaciones | Resolución aplicada |
|---|---|---|
| Secuencia desplazada `1112` a `1115` dentro de las tablas | Páginas 2, 10 y 11 | Se adoptan los códigos de los encabezados y del listado general: `1112`, `1113`, `1114`, `1115`. |
| Préstamos Bancarios `2108` frente a `2109` | Páginas 2, 3 y 20 | Se adopta `2108`; la ficha educativa rotulada `2109` se asocia a `2108`. |
| Grupo `31`: Capital frente a Capital Contable | Páginas 3 y 24 | `Capital Contable`. |
| Grupo `32`: Resultados y Reservas frente a Resultados Acumulados | Páginas 3, 25 a 27 | `Resultados y Reservas`. |
| Grupo `42`: variantes operacionales/no operacionales | Páginas 28 y 30 a 33 | `Ingresos No Operacionales`. |
| Cuenta `1203`: Intangibles frente a Activos Intangibles | Páginas 2 y 14 | `Intangibles`. |
| Encabezados `6105`, `6106` y `6202` frente a campos internos `5105`, `5106` y `5202` | Páginas 28, 34, 35 y 38 | `5105`, `5106` y `5202`. |
| Cuenta `5305`: Gastos No Operacionales frente a Otros Gastos No Operacionales | Páginas 28 y 41 | `Otros Gastos No Operacionales`. |
| Subcuentas presentes solo en fichas detalladas | Páginas 12, 13, 20 y 36 | Se incluyen todas las subcuentas explícitas, sin inventar otras. |
| Formato `5110.01` a `5110.04` frente al formato continuo de otras subcuentas | Página 36 | Se conserva literalmente el formato con punto. |

## Ejecución

Ejecutar, en orden, `Database/ContaNexoDB.sql`, `Database/SeedCatalogoCuentas.sql` y `Database/StoredProcedures.sql`, conectado a la instancia de SQL Server Express o LocalDB destinada a ContaNexo Desktop. La cuenta utilizada debe tener permiso para crear bases de datos.
