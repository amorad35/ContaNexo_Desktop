/*
    ContaNexo Desktop - Catálogo contable inicial completo
    Fuente: CATÁLOGO DE CUENTAS ACTIVO, PASIVO Y PATRIMONIO
            GASTOS E INGRESOS - ACTUALIZADO.pdf

    Requiere una ContaNexoDB recién creada con Database/ContaNexoDB.sql.
    No elimina, sobrescribe ni combina cuentas existentes.
*/

USE [master];
GO

IF DB_ID(N'ContaNexoDB') IS NULL
BEGIN
    THROW 50101, 'ContaNexoDB no existe. Ejecute primero Database/ContaNexoDB.sql.', 1;
END;
GO

USE [ContaNexoDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.CuentaContable', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DetalleCuenta', N'U') IS NULL
   OR OBJECT_ID(N'dbo.GrupoContable', N'U') IS NULL
BEGIN
    THROW 50102, 'Faltan las tablas CuentaContable, DetalleCuenta o GrupoContable.', 1;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK))
    BEGIN
        THROW 50103, 'CuentaContable ya contiene datos. El seed solo se ejecuta sobre un catálogo vacío.', 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.DetalleCuenta WITH (UPDLOCK, HOLDLOCK))
    BEGIN
        THROW 50104, 'DetalleCuenta ya contiene datos. El seed no sobrescribe detalles existentes.', 1;
    END;

    DECLARE @Catalogo TABLE
    (
        codigoCuenta VARCHAR(20) NOT NULL PRIMARY KEY,
        codigoGrupo VARCHAR(4) NOT NULL,
        codigoCuentaPadre VARCHAR(20) NULL,
        nombreCuenta NVARCHAR(150) NOT NULL,
        naturalezaCuenta VARCHAR(10) NOT NULL,
        permiteMovimientoCuenta BIT NOT NULL,
        ordenCuenta INT NOT NULL UNIQUE
    );

    INSERT INTO @Catalogo
        (codigoCuenta, codigoGrupo, codigoCuentaPadre, nombreCuenta,
         naturalezaCuenta, permiteMovimientoCuenta, ordenCuenta)
    VALUES
        (N'1101', N'11', NULL, N'Caja', 'Deudora', 1, 1),
        (N'1102', N'11', NULL, N'Caja Chica', 'Deudora', 1, 2),
        (N'1103', N'11', NULL, N'Bancos', 'Deudora', 1, 3),
        (N'1104', N'11', NULL, N'Fondo Fijo', 'Deudora', 1, 4),
        (N'1105', N'11', NULL, N'Inventarios', 'Deudora', 1, 5),
        (N'1106', N'11', NULL, N'Documentos por Cobrar a Corto Plazo', 'Deudora', 1, 6),
        (N'1107', N'11', NULL, N'Cuentas por Cobrar Clientes', 'Deudora', 1, 7),
        (N'1108', N'11', NULL, N'Cuentas por Cobrar Empleados', 'Deudora', 1, 8),
        (N'1109', N'11', NULL, N'Inversiones Temporales', 'Deudora', 1, 9),
        (N'1110', N'11', NULL, N'IVA Crédito Tributario', 'Deudora', 1, 10),
        (N'1111', N'11', NULL, N'Retenciones en la Fuente por Cobrar', 'Deudora', 1, 11),
        (N'1112', N'11', NULL, N'Anticipos Sueldos', 'Deudora', 1, 12),
        (N'1113', N'11', NULL, N'Anticipos a Proveedores', 'Deudora', 1, 13),
        (N'1114', N'11', NULL, N'Arriendos Prepagados', 'Deudora', 1, 14),
        (N'1115', N'11', NULL, N'Otros Activos Corrientes', 'Deudora', 1, 15),
        (N'1201', N'12', NULL, N'Propiedad, Planta y Equipo', 'Deudora', 0, 16),
        (N'120101', N'12', N'1201', N'Terrenos', 'Deudora', 1, 17),
        (N'120102', N'12', N'1201', N'Edificios', 'Deudora', 1, 18),
        (N'120103', N'12', N'1201', N'Equipos de Oficina', 'Deudora', 1, 19),
        (N'120104', N'12', N'1201', N'Equipos de Computación', 'Deudora', 1, 20),
        (N'120105', N'12', N'1201', N'Vehículos', 'Deudora', 1, 21),
        (N'120106', N'12', N'1201', N'Muebles y Enseres', 'Deudora', 1, 22),
        (N'120107', N'12', N'1201', N'Maquinaria y Equipos', 'Deudora', 1, 23),
        (N'1202', N'12', NULL, N'Depreciación Acumulada', 'Acreedora', 0, 24),
        (N'120201', N'12', N'1202', N'Deprec. Acum. Edificaciones', 'Acreedora', 1, 25),
        (N'120202', N'12', N'1202', N'Deprec. Acum. Equipos de Oficina', 'Acreedora', 1, 26),
        (N'120203', N'12', N'1202', N'Deprec. Acum. Vehículo', 'Acreedora', 1, 27),
        (N'1203', N'12', NULL, N'Intangibles', 'Deudora', 0, 28),
        (N'120301', N'12', N'1203', N'Licencias', 'Deudora', 1, 29),
        (N'120302', N'12', N'1203', N'Software', 'Deudora', 1, 30),
        (N'120303', N'12', N'1203', N'Marcas y Patentes', 'Deudora', 1, 31),
        (N'1204', N'12', NULL, N'Amortización Acumulada', 'Acreedora', 1, 32),
        (N'1205', N'12', NULL, N'Inversiones a Largo Plazo', 'Deudora', 1, 33),
        (N'1206', N'12', NULL, N'Otros Activos No Corrientes', 'Deudora', 1, 34),
        (N'2101', N'21', NULL, N'Proveedores', 'Acreedora', 1, 35),
        (N'2102', N'21', NULL, N'Documentos por Pagar a Corto Plazo', 'Acreedora', 1, 36),
        (N'2103', N'21', NULL, N'Cuentas por Pagar', 'Acreedora', 1, 37),
        (N'2104', N'21', NULL, N'Obligaciones con IESS', 'Acreedora', 1, 38),
        (N'2105', N'21', NULL, N'Retenciones por Pagar', 'Acreedora', 0, 39),
        (N'210501', N'21', N'2105', N'Retención en la Fuente', 'Acreedora', 1, 40),
        (N'210502', N'21', N'2105', N'Retención IVA', 'Acreedora', 1, 41),
        (N'2106', N'21', NULL, N'Sueldos y Salarios por Pagar', 'Acreedora', 1, 42),
        (N'2107', N'21', NULL, N'IVA Débito Fiscal', 'Acreedora', 1, 43),
        (N'2108', N'21', NULL, N'Préstamos Bancarios', 'Acreedora', 1, 44),
        (N'2110', N'21', NULL, N'Provisiones', 'Acreedora', 0, 45),
        (N'211001', N'21', N'2110', N'Provisión de Vacaciones', 'Acreedora', 1, 46),
        (N'211002', N'21', N'2110', N'Provisión de Décimos', 'Acreedora', 1, 47),
        (N'2111', N'21', NULL, N'Otros Pasivos Corrientes', 'Acreedora', 1, 48),
        (N'2201', N'22', NULL, N'Préstamos Bancarios a Largo Plazo', 'Acreedora', 1, 49),
        (N'2202', N'22', NULL, N'Documentos por Pagar a Largo Plazo', 'Acreedora', 1, 50),
        (N'2203', N'22', NULL, N'Provisiones a Largo Plazo', 'Acreedora', 1, 51),
        (N'2204', N'22', NULL, N'Otros Pasivos No Corrientes', 'Acreedora', 1, 52),
        (N'3101', N'31', NULL, N'Capital Social', 'Acreedora', 1, 53),
        (N'3102', N'31', NULL, N'Aportes de Socios', 'Acreedora', 1, 54),
        (N'3201', N'32', NULL, N'Reservas', 'Acreedora', 1, 55),
        (N'3202', N'32', NULL, N'Resultados Acumulados', 'Acreedora', 1, 56),
        (N'3203', N'32', NULL, N'Utilidad del Ejercicio', 'Acreedora', 1, 57),
        (N'3204', N'32', NULL, N'Pérdida del Ejercicio', 'Deudora', 1, 58),
        (N'4101', N'41', NULL, N'Ventas', 'Acreedora', 1, 59),
        (N'4102', N'41', NULL, N'Ingresos por Servicios', 'Acreedora', 1, 60),
        (N'4103', N'41', NULL, N'Otros Ingresos Operacionales', 'Acreedora', 1, 61),
        (N'4201', N'42', NULL, N'Ingresos Financieros', 'Acreedora', 0, 62),
        (N'420101', N'42', N'4201', N'Intereses Ganados', 'Acreedora', 1, 63),
        (N'420102', N'42', N'4201', N'Rendimientos Bancarios', 'Acreedora', 1, 64),
        (N'4202', N'42', NULL, N'Ingresos Diversos', 'Acreedora', 1, 65),
        (N'4203', N'42', NULL, N'Otros Ingresos No Operacionales', 'Acreedora', 1, 66),
        (N'5101', N'51', NULL, N'Sueldos y Salarios Administrativos', 'Deudora', 1, 67),
        (N'5102', N'51', NULL, N'Aportes Patronales', 'Deudora', 1, 68),
        (N'5103', N'51', NULL, N'Servicios Básicos', 'Deudora', 1, 69),
        (N'5104', N'51', NULL, N'Arrendamientos', 'Deudora', 1, 70),
        (N'5105', N'51', NULL, N'Papelería y Útiles de Oficina', 'Deudora', 1, 71),
        (N'5106', N'51', NULL, N'Depreciaciones', 'Deudora', 1, 72),
        (N'5107', N'51', NULL, N'Amortizaciones', 'Deudora', 1, 73),
        (N'5108', N'51', NULL, N'Honorarios Profesionales', 'Deudora', 1, 74),
        (N'5109', N'51', NULL, N'Servicios Notariales', 'Deudora', 1, 75),
        (N'5110', N'51', NULL, N'Servicios Generales', 'Deudora', 0, 76),
        (N'5110.01', N'51', N'5110', N'Limpieza y Mantenimiento', 'Deudora', 1, 77),
        (N'5110.02', N'51', N'5110', N'Seguridad y Vigilancia', 'Deudora', 1, 78),
        (N'5110.03', N'51', N'5110', N'Jardinería', 'Deudora', 1, 79),
        (N'5110.04', N'51', N'5110', N'Mantenimiento de Áreas Comunes', 'Deudora', 1, 80),
        (N'5111', N'51', NULL, N'Materiales de Limpieza', 'Deudora', 1, 81),
        (N'5201', N'52', NULL, N'Publicidad y Propaganda', 'Deudora', 1, 82),
        (N'5202', N'52', NULL, N'Comisiones en Ventas', 'Deudora', 1, 83),
        (N'5203', N'52', NULL, N'Gastos de Transporte', 'Deudora', 1, 84),
        (N'5204', N'52', NULL, N'Empaques y Embalajes', 'Deudora', 1, 85),
        (N'5301', N'53', NULL, N'Gastos Financieros', 'Deudora', 1, 86),
        (N'5302', N'53', NULL, N'Pérdidas en Venta de Activos', 'Deudora', 1, 87),
        (N'5303', N'53', NULL, N'Multas y Recargos', 'Deudora', 1, 88),
        (N'5304', N'53', NULL, N'Gastos Diversos', 'Deudora', 1, 89),
        (N'5305', N'53', NULL, N'Otros Gastos No Operacionales', 'Deudora', 1, 90);

    IF (SELECT COUNT(*) FROM @Catalogo) <> 90
    BEGIN
        THROW 50105, 'El catálogo preparado no contiene las 90 cuentas esperadas.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Catalogo AS catalogo
        LEFT JOIN dbo.GrupoContable AS grupo
            ON grupo.codigoGrupo = catalogo.codigoGrupo
        WHERE grupo.idGrupoContable IS NULL
    )
    BEGIN
        THROW 50106, 'Falta uno o más grupos requeridos por el catálogo.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Catalogo AS hija
        LEFT JOIN @Catalogo AS padre
            ON padre.codigoCuenta = hija.codigoCuentaPadre
        WHERE hija.codigoCuentaPadre IS NOT NULL
          AND (padre.codigoCuenta IS NULL OR padre.codigoGrupo <> hija.codigoGrupo)
    )
    BEGIN
        THROW 50107, 'Existe una cuenta hija sin padre válido en el mismo grupo.', 1;
    END;

    INSERT INTO dbo.CuentaContable
        (idGrupoContable, idCuentaPadre, codigoCuenta, nombreCuenta,
         naturalezaCuenta, permiteMovimientoCuenta, ordenCuenta)
    SELECT
        grupo.idGrupoContable,
        NULL,
        catalogo.codigoCuenta,
        catalogo.nombreCuenta,
        catalogo.naturalezaCuenta,
        catalogo.permiteMovimientoCuenta,
        catalogo.ordenCuenta
    FROM @Catalogo AS catalogo
    INNER JOIN dbo.GrupoContable AS grupo
        ON grupo.codigoGrupo = catalogo.codigoGrupo
    WHERE catalogo.codigoCuentaPadre IS NULL;

    IF @@ROWCOUNT <> 67
    BEGIN
        THROW 50108, 'No se insertaron las 67 cuentas de nivel principal esperadas.', 1;
    END;

    INSERT INTO dbo.CuentaContable
        (idGrupoContable, idCuentaPadre, codigoCuenta, nombreCuenta,
         naturalezaCuenta, permiteMovimientoCuenta, ordenCuenta)
    SELECT
        grupo.idGrupoContable,
        padre.idCuentaContable,
        catalogo.codigoCuenta,
        catalogo.nombreCuenta,
        catalogo.naturalezaCuenta,
        catalogo.permiteMovimientoCuenta,
        catalogo.ordenCuenta
    FROM @Catalogo AS catalogo
    INNER JOIN dbo.GrupoContable AS grupo
        ON grupo.codigoGrupo = catalogo.codigoGrupo
    INNER JOIN dbo.CuentaContable AS padre
        ON padre.codigoCuenta = catalogo.codigoCuentaPadre
    WHERE catalogo.codigoCuentaPadre IS NOT NULL;

    IF @@ROWCOUNT <> 23
    BEGIN
        THROW 50109, 'No se insertaron las 23 subcuentas esperadas.', 1;
    END;

    DECLARE @DetalleCatalogo TABLE
    (
        codigoCuenta VARCHAR(20) NOT NULL PRIMARY KEY,
        descripcionDetalle NVARCHAR(MAX) NULL,
        dinamicaDebitoDetalle NVARCHAR(MAX) NULL,
        dinamicaCreditoDetalle NVARCHAR(MAX) NULL
    );

    INSERT INTO @DetalleCatalogo
        (codigoCuenta, descripcionDetalle, dinamicaDebitoDetalle,
         dinamicaCreditoDetalle)
    VALUES
        (N'1101', N'Esta cuenta registra todo el dinero en efectivo que la entidad tiene
disponible para usar de inmediato. El efectivo puede estar en
billetes, monedas, cheques recibidos al día o cualquier valor que
represente dinero listo para utilizar.
Es una de las cuentas más importantes, porque representa la
liquidez inmediata de la entidad y permite realizar pagos, compras o
cubrir gastos del día a día.
Se utiliza cada vez que la entidad recibe o entrega dinero físico, por
ejemplo: cuando cobra una venta, cuando repone caja chica,
cuando paga a un proveedor en efectivo o cuando se realiza un
arqueo de caja.
Además, permite controlar los sobrantes o faltantes de dinero que
se detecten al momento de contar el efectivo (arqueo).', N'La cuenta aumenta cuando entra dinero a la entidad.
1.-Cuando recibimos dinero por ventas en efectivo.
2.-Cuando los clientes pagan obligaciones en efectivo.
3.-Cuando se realizan transferencias desde bancos hacia caja
(retiros).
4.-Cuando se reciben depósitos o remesas al contado.
5.-Cuando se registran sobrantes en arqueos de caja (porque
aumenta el saldo real).
Idea clave para estudiantes: Toda entrada de efectivo se carga a
Caja, porque aumenta el activo.', N'La cuenta disminuye cuando sale dinero de la entidad.
1. Cuando se pagan gastos en efectivo (servicios básicos,
materiales, compras menores).
2. Cuando se entregan anticipos al personal.
3. Cuando se realizan depósitos de efectivo en bancos.
4. Cuando se pagan deudas a proveedores en efectivo.
5. Cuando se registran faltantes en arqueos (porque disminuye el
efectivo real).
Idea clave para estudiantes: Toda salida de efectivo se abona a
Caja, porque disminuye el activo.'),
        (N'1102', N'Representa un fondo de dinero asignado para realizar pagos
pequeños y urgentes, como comprar esferos, pagar un envío,
comprar un repuesto económico, pagar una movilidad, etc. Se
maneja bajo el sistema de fondo fijo, es decir, siempre debe
mantenerse un mismo saldo autorizado.
El objetivo es facilitar pagos inmediatos sin necesidad de emitir un
cheque o realizar una transferencia bancaria. Cuando el dinero se
utiliza, se repone mediante un comprobante, manteniendo así el
fondo operativo.', N'La cuenta aumenta cuando se constituye o repone el fondo de caja
chica.
1. Constitución inicial del fondo fijo.
2. Reposición del fondo después de gastar.
3. Incrementos autorizados al fondo fijo.
Idea clave: Caja chica solo crece cuando se repone o aumenta el
fondo.', N'La cuenta disminuye cuando se usa el fondo o se liquida.
1. Cuando se realiza un gasto menor y el responsable entrega
facturas para justificarlo.
2. Cuando se reduce el monto del fondo fijo.
3. Cuando se liquida totalmente el fondo.'),
        (N'1103', N'Registra los fondos que la entidad mantiene en instituciones
financieras, ya sea en cuentas corrientes o de ahorros. El dinero en
bancos es más seguro que el efectivo y permite realizar pagos
mediante cheques, transferencias y débitos automáticos.
Los saldos bancarios representan uno de los activos de mayor
importancia, ya que permiten transacciones formales y rastreables.', N'La cuenta aumenta cuando ingresan fondos al banco.
1. Depósitos en efectivo o cheques.
2. Transferencias desde otras cuentas bancarias o desde caja.
3. Notas de crédito por intereses ganados o ajustes a favor.
4. Pagos recibidos de clientes por transferencia bancaria.
Idea clave: Si el dinero llega al banco, se carga Bancos.', N'La cuenta disminuye cuando salen fondos del banco.
1. Cheques girados.
2. Transferencias electrónicas a proveedores.
3. Débitos automáticos por servicios o impuestos.
4. Notas de débito por comisiones bancarias.
Idea clave: Si el dinero sale del banco, se abona Bancos.'),
        (N'1104', N'Fondo institucional creado para cumplir funciones específicas que
requieren liquidez constante, distinto de la caja chica. Por ejemplo:
fondos para viáticos, fondos rotativos, fondos de tesorería.
Es dinero reservado para actividades permanentes de la entidad y
su administración está regulada por políticas internas.', N'1. Constitución inicial del fondo.
2. Aumento del fondo autorizado por dirección.
3. Reposiciones parciales o totales.', N'1. Gastos pagados con el fondo.
2. Disminución del fondo por decisión administrativa.
3. Liquidación total del fondo.'),
        (N'1105', N'Registra el valor de los bienes o productos destinados para la venta
dentro de la actividad comercial de la empresa. Comprende las
mercaderías disponibles para comercialización y controladas como
existencias del negocio.', N'La cuenta aumenta cuando ingresan productos al inventario.
1. Compra de mercaderías al contado.
2. Compra de mercaderías a crédito.
3. Devoluciones realizadas por clientes.
4. Ajustes por sobrantes de inventario.
Idea clave: Si ingresan productos al inventario, se carga.', N'La cuenta disminuye cuando salen productos del inventario.
1. Venta de mercaderías.
2. Devoluciones a proveedores.
3. Pérdidas o daños de inventario.
4. Ajustes por faltantes en inventarios físicos.
Idea clave: Si salen productos del inventario, se abona.'),
        (N'1106', N'Representa derechos de cobro respaldados por documentos
formales, como pagarés, letras de cambio o convenios firmados,
cuyo cobro se espera dentro de un año. Estos documentos tienen
fecha de vencimiento, monto acordado y firmas de las partes, por lo
que otorgan mayor seguridad que una cuenta por cobrar común.
Se usan en operaciones formales de crédito otorgado por la entidad.', N'La cuenta aumenta cuando se reciben documentos firmados a favor
de la entidad.
1. Emisión o aceptación de un pagaré.
2. Conversión de una cuenta por cobrar en documento formal.
3. Reconocimiento de nuevos documentos otorgados a clientes o
terceros.
Idea clave: Si nos firman un documento para poder cobrar, se carga
esta cuenta.', N'La cuenta disminuye cuando el documento es pagado o cancelado.
1. Cobro total o parcial del documento.
2. Descuentos o condonaciones autorizadas.
3. Castigo por incobrabilidad.
Idea clave: Si se cobra o se elimina el documento, se abona.'),
        (N'1107', N'Registra las deudas que los clientes tienen con la entidad por ventas
realizadas a crédito. Constituye uno de los activos más importantes
porque representa dinero que ingresará en el corto plazo.
Además permite controlar qué clientes deben, cuánto deben y
cuándo deben pagar. Es fundamental para gestionar la cartera y
evitar morosidad.', N'La cuenta aumenta cuando se vende a crédito.
1. Ventas de bienes a crédito.
2. Prestación de servicios con pago diferido.
3. Reconocimientos de saldos a favor del cliente (ajustes,
reclamos).
Idea clave: Si el cliente queda debiendo, se carga esta cuenta.', N'La cuenta disminuye cuando el cliente paga o se hace un ajuste.
1. Pagos recibidos en efectivo o banco.
2. Notas de crédito por devoluciones o descuentos.
3. Castigos por incobrabilidad.
Idea clave: Si el cliente paga o se perdona la deuda, se abona.'),
        (N'1108', N'Registra valores que la entidad entrega a los empleados y que estos
deben devolver posteriormente: préstamos, anticipos de sueldos,
valores entregados para trámites personales o cobros indebidos
descontables.
Es muy común en instituciones donde se otorgan beneficios de
crédito interno o adelantos.', N'La cuenta aumenta cuando el empleado recibe un valor que deberá
devolver.
1. Préstamos entregados al personal.
2. Anticipos de sueldo.
3. Entregas en efectivo para trámites institucionales.
Idea clave: Si el empleado queda debiendo, se carga.', N'La cuenta disminuye cuando el empleado devuelve el dinero.
1. Descuentos en rol de pagos.
2. Pagos en efectivo.
3. Liquidaciones o compensaciones.
Idea clave: Si el empleado paga, se abona.'),
        (N'1109', N'Registra los recursos invertidos por la entidad en activos financieros
de corto plazo, es decir, inversiones que pueden convertirse
fácilmente en efectivo en menos de un año. Ejemplos: certificados de
depósito a corto plazo, papeles comerciales, inversiones en bolsa de
rápida liquidación o fondos de inversión.
Estas inversiones se realizan para obtener rentabilidad sin
comprometer la disponibilidad inmediata del dinero. Se consideran
de bajo riesgo y alta liquidez.', N'La cuenta aumenta cuando la entidad invierte o adquiere activos
financieros temporales.
1. Compra de certificados de depósito a corto plazo.
2. Inversiones realizadas en entidades financieras.
3. Adquisición de acciones o bonos de rápida conversión.
4. Transferencias de dinero desde caja o bancos hacia la inversión.
Idea clave: Si la empresa compra una inversión, se carga
Inversiones temporales.', N'La cuenta disminuye cuando se recupera, vende o vence una
inversión.
1. Venta o redención de una inversión financiera.
2. Vencimiento de un certificado de depósito.
3. Transferencias hacia bancos por devolución del capital invertido.
Idea clave: Si la empresa recupera la inversión, se abona esta
cuenta porque disminuye.'),
        (N'1110', N'Registra el IVA pagado en compras de bienes o servicios que puede
ser recuperado o descontado del IVA generado en ventas. Es un
activo temporal, porque luego se compensa al declarar impuestos.', N'La cuenta aumenta cuando la empresa realiza compras gravadas
con IVA.
1. Compra de mercaderías.
2. Compra de activos fijos.
3. Pago de servicios gravados con IVA.
4. Gastos administrativos con factura autorizada.
Idea clave: Si la empresa paga IVA en compras, se carga.', N'La cuenta disminuye cuando el IVA es compensado o utilizado en
la declaración tributaria.
1. Compensación del IVA en declaraciones mensuales.
2. Notas de crédito de proveedores.
3. Ajustes tributarios.
4. Reclasificación contable del IVA utilizado.
Idea clave: Si el IVA crédito tributario se utiliza o disminuye, se
abona.'),
        (N'1111', N'Registra los valores retenidos por clientes sobre pagos realizados a
la empresa, los cuales constituyen un crédito tributario
recuperable ante la administración tributaria.', N'1. Registro de retenciones en la fuente efectuadas por clientes a
favor de la empresa.
Idea clave: Aumenta cuando un cliente retiene impuestos sobre
una factura emitida por la empresa.', N'1. Aplicación, compensación o recuperación de las retenciones en
la declaración de impuestos.
Idea clave: Disminuye cuando el crédito tributario es utilizado o
compensado.'),
        (N'1112', N'Registra los valores entregados anticipadamente a los trabajadores
por concepto de sueldos o remuneraciones que posteriormente
serán descontados en el rol de pagos correspondiente. Representa
un derecho de cobro temporal para la empresa.', N'La cuenta aumenta cuando la empresa entrega anticipos de sueldo
al personal.
1. Entrega de anticipos quincenales.
2. Adelantos extraordinarios de sueldo.
3. Transferencias anticipadas al trabajador.
Idea clave: Si el trabajador recibe dinero por adelantado, se carga.', N'La cuenta disminuye cuando el trabajador devuelve el dinero o se
realiza el descuento correspondiente.
1. Descuento en el rol de pagos.
2. Devolución en efectivo.
3. Compensación con liquidaciones laborales.
Idea clave: Si el anticipo es recuperado o descontado, se abona.'),
        (N'1113', N'Registra valores entregados a proveedores antes de recibir los
bienes o servicios. Es frecuente cuando el proveedor exige pagos
anticipados para reservar productos, iniciar producción o asegurar
el pedido.
Una vez entregado el bien o servicio, el anticipo se rebaja y se
registra como gasto o inventario, según corresponda.', N'1. Pagos anticipados a proveedores.
2. Transferencias para reservar bienes o servicios.
3. Pagos para iniciar órdenes de producción.
Idea clave: Si la empresa paga antes, se carga.', N'1. Recepción de la mercadería o servicio.
2. Aplicación del anticipo a la factura del proveedor.
3. Devoluciones del proveedor.
Idea clave: Cuando se recibe lo comprado, el anticipo se abona.'),
        (N'1114', N'Registra los valores pagados anticipadamente por concepto de
arriendos o alquileres que corresponden a períodos futuros.
Representa un derecho o beneficio económico que será utilizado
posteriormente por la empresa.', N'La cuenta aumenta cuando la empresa realiza pagos anticipados
de arriendo.
1. Pago adelantado de alquiler de oficinas.
2. Cancelación anticipada de locales comerciales.
3. Anticipos de contratos de arrendamiento.
Idea clave: Si se paga un arriendo por adelantado, se carga.', N'La cuenta disminuye conforme transcurre el tiempo y el arriendo se
convierte en gasto del período.
1. Reconocimiento mensual del gasto de arriendo.
2. Ajustes contables por consumo del servicio.
3. Finalización del período pagado anticipadamente.
Idea clave: Si el arriendo prepagado ya fue utilizado, se abona.'),
        (N'1115', N'Incluye activos diversos de corto plazo no clasificados en otras
cuentas: depósitos en garantía, reclamos pendientes de cobro,
valores por reembolso, documentos internos no formales, etc.
Es una cuenta auxiliar para partidas menores o excepcionales.', N'La cuenta aumenta cuando se reconocen valores o derechos de
corto plazo no clasificados en otras cuentas.
1. Anticipos menores recuperables.
2. Valores pendientes de liquidación.
3. Depósitos temporales recuperables.
4. Derechos de cobro de corto plazo diversos.', N'La cuenta disminuye cuando los valores son recuperados,
utilizados o reclasificados.
1. Recuperación de valores entregados.
2. Reclasificación a otras cuentas.
3. Liquidación de anticipos.
4. Ajustes contables de regularización.'),
        (N'1201', N'Son los bienes físicos utilizados por la entidad para producir
bienes, prestar servicios o realizar sus actividades. Tienen una
vida útil mayor a un año.
Ejemplos:
• Terrenos: no se deprecian.
• Edificios: construcción destinada a operaciones.
• Equipos de oficina: impresoras, trituradoras de papel,
fotocopiadoras, equipos de apoyo administrativo.
• Equipos de computación: laptops, computadoras de escritorio,
servidores, CPU, monitores, reguladores y equipos tecnológicos.
• Vehículos: automóviles, camionetas, camiones y demás medios
de transporte utilizados por la empresa.
• Muebles y enseres: escritorios, sillas, mesas, archivadores,
estanterías y mobiliario en general.
• Maquinaria y Equipos: tornos, empacadoras, compresores,
generadores eléctricos, bandas transportadoras y maquinaria
utilizada en los procesos operativos o productivos.
Estos activos no se consumen en una sola operación, sino que se
usan durante mucho tiempo; por eso su costo se distribuye a
través de la depreciación.', N'La cuenta aumenta cuando la empresa adquiere un bien de larga
duración.
1. Compra de terrenos, edificios, computadoras, muebles,
vehículos, etc.
2. Mejoras que aumentan el valor del activo (ampliaciones,
adaptaciones).
3. Costos adicionales necesarios para dejar el activo en
funcionamiento: transporte, instalación, montaje.
Idea clave: Cuando entra un bien durable a la empresa, cargamos
Propiedad, Planta y Equipo.', N'La cuenta disminuye cuando el activo se vende, se da de baja o se
pierde.
1. Venta del activo.
2. Donación o retiro por obsolescencia.
3. Pérdidas por siniestros o robos.
Idea clave: Cuando el activo sale o deja de existir, lo abonamos.'),
        (N'1202', N'Es una cuenta que resta valor a los activos fijos (excepto terrenos).
Representa el desgaste natural, uso o paso del tiempo de los bienes.
No significa salida de dinero, sino una pérdida de valor contable.
Ejemplo: una computadora cuesta $1.200 y dura 3 años. Cada año
pierde parte de su valor, y esa parte se registra aquí.', N'Se carga cuando se elimina la depreciación acumulada al dar de
baja un activo.
1. Venta del activo.
2. Destrucción, pérdida o donación del activo.
Idea clave: Cuando el activo desaparece, también desaparece su
depreciación.', N'Se abona cuando se reconoce la depreciación del periodo.
1. Registro mensual o anual de la depreciación.
2. Ajustes por corrección de vida útil.
Idea clave: Cada mes “se gasta” el uso del bien, así que
aumentamos la depreciación acumulada.'),
        (N'1203', N'Son bienes que no tienen forma física, pero representan un valor
económico para la entidad.
Ejemplos:
• Software profesional (contable, administrativo).
• Licencias de funcionamiento o permisos especiales.
• Patentes, derechos de autor, propiedad intelectual.
Se utilizan por varios años y su valor se distribuye mediante
amortización, similar a la depreciación.', N'Aumenta cuando la empresa adquiere o desarrolla un intangible.
1. Compra de software.
2. Adquisición de licencias.
3. Registro de marcas o patentes.
Idea clave: Si compramos un intangible, se carga.', N'Disminuye cuando el intangible deja de usarse o pierde vigencia.
1. Baja de licencias o software.
2. Caducidad de derechos.
3. Venta del intangible.
Idea clave: Si el intangible deja de servir, se abona.'),
        (N'1204', N'Representa la pérdida de valor de los intangibles por el paso del
tiempo o por su uso. No implica salida de dinero; simplemente
reduce el valor contable del intangible.
Es la versión “intangibles” de la depreciación.', N'Disminuye cuando se da de baja un intangible.
1. Eliminación del intangible.
2. Ajustes o correcciones.', N'Aumenta cada periodo cuando se reconoce la amortización.
1. Amortización mensual o anual del intangible.
Idea clave: Cada mes el intangible “se gasta”, y se abona esta
cuenta.'),
        (N'1205', N'Registra inversiones que no se convertirán en efectivo durante el
año corriente. Son recursos colocados para obtener rendimientos
futuros.
Ejemplos:
• Certificados a plazo mayor a un año.
• Acciones o participaciones permanentes.
• Bonos a largo plazo.
• Inversiones en otras entidades.
Estas inversiones se mantienen como parte de la estrategia
financiera de largo plazo.', N'La cuenta aumenta cuando la empresa adquiere inversiones de
largo plazo.
1. Compra de bonos o títulos financieros.
2. Adquisición de acciones permanentes.
3. Depósitos e inversiones a plazo mayor a un año.
4. Incremento de inversiones financieras permanentes.
Idea clave: Si la empresa invierte recursos a largo plazo, se carga.', N'La cuenta disminuye cuando las inversiones son vendidas,
recuperadas o reclasificadas.
1. Venta de inversiones.
2. Cobro o vencimiento de títulos financieros.
3. Reclasificación a corto plazo.
4. Pérdidas o bajas de inversiones.
Idea clave: Si la inversión deja de pertenecer a la empresa, se
abona.'),
        (N'1206', N'Incluye activos de largo plazo que no encajan en las categorías
anteriores.
Ejemplos:
• Depósitos en garantía (por contratos).
• Obras en proceso.
• Derechos a largo plazo.
• Bienes no usados actualmente, pero destinados a uso futuro.', N'1. Reconocimiento de activos permanentes.
2. Pagos de depósitos en garantía.
3. Incorporación de bienes para uso futuro.', N'1. Recuperación de depósitos.
2. Baja del activo.
3. Transferencias a otras cuentas.'),
        (N'2101', N'Registra las deudas con proveedores por compras de bienes o
servicios al crédito, es decir, cuando la empresa recibe mercancías
o servicios y se compromete a pagar después.
Es una de las obligaciones más comunes y controladas en cualquier
empresa, pues representa compromisos a corto plazo con terceros.', N'La cuenta disminuye cuando la empresa paga lo que debe.
1. Pagos en efectivo o banco a proveedores.
2. Notas de crédito del proveedor (descuentos o devoluciones).
3. Compensaciones o acuerdos de pago.
Idea clave: Cuando cancelamos la deuda, se carga Proveedores.', N'La cuenta aumenta cuando la empresa queda debiendo.
1. Compras al crédito.
2. Facturas recibidas por bienes o servicios.
3. Ajustes que aumentan la obligación.
Idea clave: Si recibimos algo y aún no pagamos, se abona.'),
        (N'2102', N'Registra las obligaciones formales respaldadas por documentos
firmados, como pagarés o letras de cambio, que la empresa debe
pagar en menos de un año.
Son más formales que una cuenta por pagar común y generalmente
incluye intereses.', N'La cuenta disminuye cuando se paga o se cancela el documento.
1. Pago total o parcial del pagaré.
2. Descuentos o condonaciones autorizadas.
3. Castigo del documento por novación o refinanciamiento.
Idea clave: Cuando pagamos el documento, lo cargamos.', N'Aumenta cuando la empresa firma un documento de deuda.
1. Firma de pagarés a corto plazo.
2. Conversión de cuentas por pagar en documentos formales.
3. Reconocimiento de nuevos documentos emitidos.
Idea clave: Si firmamos un documento y quedamos debiendo, se
abona.'),
        (N'2103', N'Registra las obligaciones pendientes de pago que mantiene la
empresa con proveedores, acreedores o terceros por la compra de
bienes o servicios a crédito dentro del giro normal del negocio.
Representa deudas de corto plazo que deberán cancelarse
posteriormente.', N'La cuenta disminuye cuando la empresa cancela total o
parcialmente sus obligaciones.
1. Pago a proveedores.
2. Cancelación de deudas en efectivo o transferencia.
3. Devoluciones de compras.
4. Compensaciones o ajustes contables.
Idea clave: Si la empresa paga la deuda, se carga.', N'La cuenta aumenta cuando la empresa adquiere obligaciones a
crédito.
1. Compra de mercaderías a crédito.
2. Recepción de servicios pendientes de pago.
3. Obligaciones contraídas con proveedores.
4. Registro de facturas por pagar.'),
        (N'2104', N'Son valores que la empresa debe al Instituto Ecuatoriano de
Seguridad Social por aportes personales y patronales. Se generan
cada mes y deben ser declarados y pagados en los plazos
establecidos.', N'Disminuye cuando se paga al IESS.
1. Pago de aportes mensuales.
2. Pago de multas o intereses por mora.
Idea clave: Cuando cancelamos al IESS, se carga.', N'Aumenta cuando se generan nuevas obligaciones.
1. Aportes patronales del mes.
2. Aportes personales retenidos.
3. Multas o recargos generados por mora.
Idea clave: Cuando nace la obligación con el IESS, se abona.'),
        (N'2105', N'Registra valores que la empresa retiene a proveedores, empleados
o terceros (Renta o IVA) que luego debe pagar al SRI. La empresa
actúa como “agente de retención”.', N'Disminuye cuando se pagan las retenciones al SRI.
1. Pago mensual de las retenciones de renta.
2. Pago de retenciones de IVA.
Idea clave: Si entregamos al SRI lo retenido, se carga.', N'Aumenta cuando la empresa retiene impuestos.
1. Retención al proveedor por facturas.
2. Retención por servicios profesionales.
3. Retención de IVA según ley.
Idea clave: Si retenemos impuestos, se abona.'),
        (N'2106', N'Registra las remuneraciones devengadas por los empleados que
aún no han sido pagadas. Incluye sueldos, horas extras, comisiones
y beneficios devengados al final del mes.', N'Disminuye cuando se paga la nómina.
1. Pago del rol mensual.
2. Liquidaciones individuales.
Idea clave: Cuando pagamos el sueldo, se carga.', N'Aumenta cuando se genera la obligación.
1. Registro de sueldos del mes.
2. Horas extras y recargos.
3. Beneficios laborales devengados.
Idea clave: Mientras el sueldo esté pendiente, se abona.'),
        (N'2107', N'Registra el IVA generado en las ventas realizadas por la empresa.
Ese IVA no es un ingreso, sino un impuesto que se debe entregar al
SRI.', N'Disminuye cuando se paga o se compensa el IVA al SRI.
1. Pago del IVA mensual.
2. Compensación con crédito fiscal.
Idea clave: Cuando entregamos ese IVA al SRI, se carga.', N'Aumenta cuando la empresa vende bienes o servicios gravados.
1. IVA generado por ventas.
2. Ajustes por emisión de facturas.
Idea clave: Si generamos IVA por vender, se abona.'),
        (N'2108', N'Registra préstamos u obligaciones financieras con bancos o
cooperativas que deben pagarse en menos de un año. Suelen
incluir intereses y cuotas periódicas.', N'Disminuye cuando se paga el crédito.
1. Pagos de cuotas.
2. Abonos al capital.
Idea clave: Si disminuye la deuda, se carga.', N'Aumenta cuando se recibe un préstamo.
1. Créditos recibidos de instituciones financieras.
2. Notas de débito por capital inicial.
Idea clave: Si la empresa recibe un préstamo, se abona.'),
        (N'2110', N'Registra obligaciones laborales que la empresa sabe que tendrá
que pagar, pero que aún no se pagan. Ejemplo: vacaciones
acumuladas y décimos devengados.
Se registran para reflejar de manera realista las obligaciones
laborales.', N'Disminuye cuando se pagan las obligaciones previstas.
1. Pago de vacaciones.
2. Pago de décimos.
Idea clave: Si se paga la obligación, se carga.', N'Aumenta cuando se generan nuevas provisiones.
1. Reconocimiento de vacaciones devengadas.
2. Cálculo de décimos del periodo.
Idea clave: Cuando la empresa acumula obligaciones, se abona.'),
        (N'2111', N'Incluye obligaciones diversas que deben pagarse en menos de un
año y que no se clasifican en las cuentas anteriores: pagos
pendientes, reclamos, valores por liquidar, cuentas temporales,
etc.', N'La cuenta disminuye cuando las obligaciones son canceladas,
compensadas o regularizadas.
1. Pago de obligaciones pendientes.
2. Compensación de valores adeudados.
3. Ajustes o reclasificaciones contables.
4. Eliminación de provisiones no utilizadas.
Idea clave: Si la obligación se paga o desaparece, se carga.', N'La cuenta aumenta cuando la empresa adquiere nuevas
obligaciones de corto plazo no clasificadas en otras cuentas.
1. Registro de obligaciones diversas.
2. Provisiones temporales pendientes de pago.
3. Valores retenidos a terceros.
4. Pasivos ocasionales de corto plazo.
Idea clave: Si la empresa adquiere una obligación pendiente, se
abona.'),
        (N'2201', N'Registra las deudas contraídas con bancos o instituciones
financieras que deben pagarse en un plazo mayor a un año. Incluye
el valor principal del préstamo, excluyendo los intereses del
periodo.
Ejemplos:
• Préstamos para comprar maquinaria.
• Créditos para construir instalaciones.
• Financiamiento para proyectos a largo plazo.
Es una de las cuentas más importantes del pasivo, porque afecta la
estructura financiera de la empresa durante varios años.', N'La cuenta disminuye cuando se pagan las cuotas del préstamo.
1. Pago del capital del préstamo (no intereses).
2. Cancelación anticipada del crédito.
Idea clave: Cuando devolvemos parte del préstamo, se carga.', N'La cuenta aumenta cuando la empresa recibe un préstamo a largo
plazo.
1. Crédito recibido del banco.
2. Ampliación del préstamo existente.
Idea clave: Cuando el banco nos entrega el dinero, se abona.'),
        (N'2202', N'Registra obligaciones formalizadas mediante documentos
firmados (pagarés, letras de cambio o contratos) cuyo
vencimiento es mayor a un año.
Es una deuda formal y suele incluir intereses, cronogramas de
pago y condiciones especiales.', N'La cuenta disminuye cuando la empresa paga el documento.
1. Pago parcial o total del documento.
2. Cancelación o refinanciamiento del documento.
Idea clave: Si pagamos el documento, se carga.', N'Aumenta cuando la empresa firma un documento a largo plazo.
1. Reconocimiento de un documento por pagar.
2. Conversión de una deuda en documento formal.
Idea clave: Si firmamos algo y quedamos debiendo, se abona.'),
        (N'2203', N'Registra obligaciones futuras que la empresa estima que deberá
pagar después de un año. No son deudas exactas, sino
estimaciones contables, basadas en la mejor información
disponible.
Ejemplos:
• Provisión para indemnizaciones futuras.
• Provisión para litigios legales.
• Provisión para mantenimiento mayor.
• Provisión por garantías extendidas.
Permite reflejar compromisos económicos que afectarán periodos
futuros.', N'La cuenta disminuye cuando se paga o se cancela la obligación
estimada.
1. Pago de indemnizaciones estimadas.
2. Pago por litigios.
3. Ajustes por disminución del pasivo.
Idea clave: Si se paga o disminuye la obligación, se carga.', N'Aumenta cuando se reconoce una obligación futura estimada.
1. Creación de provisiones por obligaciones probables.
2. Ajustes por incremento de estimaciones.
Idea clave: Si surge una obligación futura, se abona.'),
        (N'2204', N'Registra obligaciones diversas de largo plazo que no encajan en
las cuentas anteriores. Son deudas cuyo pago se realizará
después de un año.
Ejemplos:
• Contratos de arrendamiento financiero a largo plazo.
• Pagos diferidos.
• Compromisos contractuales de largo plazo.
• Valores retenidos para pagos futuros.', N'Disminuye cuando se paga la obligación.
1. Pagos parciales o totales del pasivo.
2. Cancelación contractual.
3. Ajustes por reducción del compromiso.
Idea clave: Si pagamos, disminuye; por eso se carga.', N'Aumenta cuando surge una nueva obligación a largo plazo.
1. Registro de nuevos compromisos.
2. Ajustes que aumentan el pasivo.
Idea clave: Si nace una deuda a largo plazo, se abona.'),
        (N'3101', N'Representa el valor aportado por los dueños o socios al
momento de constituir la empresa o cuando realizan aportes
adicionales. Puede ser en dinero, bienes, maquinaria o
cualquier activo transferido a la empresa.
Es la base del patrimonio y refleja la participación de los socios
en la entidad. No se devuelve, excepto en liquidaciones o
disminuciones de capital legalmente autorizadas.', N'Disminuye cuando se reduce o devuelve parte del capital a los
socios.
1. Devolución de aportes a los socios.
2. Disminuciones de capital autorizadas legalmente.
Idea clave: Cuando la empresa entrega capital al socio, se
carga.', N'Aumenta cuando los socios aportan bienes o dinero a la
empresa.
1. Aportes iniciales de capital.
2. Aportes adicionales de los socios.
3. Capitalización de utilidades o reservas.
Idea clave: Si los socios aportan, se abona.'),
        (N'3102', N'Registra los aportes voluntarios o extraordinarios realizados por los
socios, distintos del capital social formal. Estos aportes fortalecen
las operaciones o la liquidez de la empresa y pueden ser:
• Aportes para cubrir pérdidas
• Aportes para financiar proyectos
• Aportes temporales o permanentes.
No forman parte del capital autorizado, pero sí del patrimonio.', N'Disminuye cuando se devuelven aportes o cuando se capitalizan.
1. Devolución de aportes voluntarios.
2. Traslado a capital social (capitalización).
3. Ajustes por disminuciones autorizadas.
Idea clave: Si se devuelve el aporte, se carga.', N'Aumenta cuando los socios depositan recursos adicionales.
1. Aportes en dinero.
2. Aportes en bienes.
3. Aportes para fortalecer la empresa.
Idea clave: Si el socio aporta fuera del capital, se abona.'),
        (N'3201', N'Representa utilidades retenidas que la empresa no distribuye,
sino que las reserva para fines específicos:
•Reserva legal
• Reserva por revalorización del patrimonio
• Reservas voluntarias
Las reservas fortalecen financieramente a la entidad y sirven para
protegerla ante emergencias o proyectos futuros.', N'Disminuye cuando se usa la reserva.
1. Compensación de pérdidas del ejercicio.
2. Capitalización de reservas.
3. Uso autorizado por socios o asamblea.
Idea clave: Cuando la empresa usa la reserva, se carga.', N'Aumenta cuando se destinan utilidades a reservas.
1. Asignación de reserva legal.
2. Constitución de reservas voluntarias.
3. Incrementos por nuevas utilidades.
Idea clave: Si se guarda parte de la utilidad, se abona.'),
        (N'3202', N'Representa utilidades o pérdidas de años anteriores que la
empresa ha acumulado. Es decir, el resultado histórico que no se
ha distribuido ni usado.
Se compone de:
• Utilidades acumuladas
• Pérdidas acumuladas
Refleja el desempeño de la empresa en periodos anteriores.', N'Disminuye cuando la empresa registra pérdidas acumuladas o
distribuye utilidades.
1. Distribución de dividendos.
2. Absorción de pérdidas.
3. Ajustes contables por periodos anteriores.
Idea clave: Si se usa o se pierde utilidad acumulada, se carga.', N'Aumenta cuando se trasladan utilidades de ejercicios anteriores.
1. Cierre contable del ejercicio anterior.
2. Ajustes que incrementan resultados acumulados.
Idea clave: Si aumentan las utilidades acumuladas, se abona.'),
        (N'3203', N'Representa la utilidad o pérdida generada en el periodo actual
(aún no cerrada). Este valor se obtiene restando ingresos menos
costos y gastos del ejercicio.
Es una cuenta temporal que refleja si la empresa está ganando o
perdiendo en el año actual.', N'Se carga cuando el ejercicio genera pérdida.
1. Registro de pérdidas del periodo.
2. Ajustes que disminuyen la utilidad.
Idea clave: Si hay pérdida, se carga.', N'Se abona cuando el ejercicio genera utilidad.
1. Registro de utilidades del ejercicio.
2. Ajustes que incrementan la utilidad.
Idea clave: Si la empresa gana, se abona.'),
        (N'3204', N'Registra el resultado negativo obtenido por la empresa durante un
período contable, cuando los gastos y costos superan a los
ingresos generados.
Representa una disminución del patrimonio de la entidad.', N'La cuenta aumenta cuando al cierre del ejercicio se determina una
pérdida contable.
1. Cierre de cuentas de gastos.
2. Determinación del resultado negativo del período.
3. Ajustes que incrementen la pérdida del ejercicio.
Idea clave: Si la empresa obtiene pérdidas, se carga.', N'La cuenta disminuye cuando la pérdida es absorbida, compensada
o reclasificada dentro del patrimonio.
1. Compensación con utilidades acumuladas.
2. Capitalización de pérdidas.
3. Ajustes patrimoniales.
4. Cierre contable del ejercicio siguiente.
Idea clave: Si la pérdida se compensa o elimina, se abona.'),
        (N'4101', N'Registra los ingresos obtenidos por la venta de bienes, productos o
mercancías que forman parte de la actividad principal de la empresa.
Incluye ventas al contado y al crédito (estas últimas generan cuentas
por cobrar). Este es el ingreso más importante cuando la empresa se
dedica a comercializar productos.', N'Se carga cuando se anula o disminuye una venta:
1. Devoluciones de ventas.
2. Descuentos comerciales concedidos después de facturar.
3. Errores en la facturación que reducen el valor vendido.
Idea clave: Si la venta se revierte o corrige, se carga.', N'Aumenta con las ventas realizadas durante el periodo.
1. Ventas al contado.
2. Ventas al crédito.
3. Emisión de facturas por productos entregados.
Idea clave: Cada venta suma a esta cuenta.'),
        (N'4102', N'Registra los valores obtenidos por la empresa por la prestación de
servicios relacionados con su actividad económica. Representa
ingresos generados por asesorías, mantenimiento, transporte,
instalación u otros servicios prestados a clientes.', N'La cuenta disminuye al cierre del ejercicio o por ajustes y
devoluciones relacionadas con los servicios prestados.
1. Cierre contable de ingresos.
2. Notas de crédito por devolución de servicios.
3. Ajustes o reclasificaciones contables.
Idea clave: Si el ingreso disminuye o se cierra, se carga.', N'La cuenta aumenta cuando la empresa presta servicios y genera
ingresos.
1. Cobro por servicios prestados.
2. Facturación de servicios.
3. Reconocimiento de ingresos operacionales por servicios.
Idea clave: Si la empresa genera ingresos por servicios, se abona.'),
        (N'4103', N'Ingresos relacionados con la actividad de la empresa, pero que
no provienen directamente de ventas ni de servicios. Ejemplos:
• Cobro de fletes.
• Venta de desperdicios o subproductos.
• Multas cobradas a clientes.
• Comisiones por operaciones internas relacionadas con la
actividad principal.', N'Se carga cuando estos ingresos deben corregirse o anularse.
1. Notas de crédito emitidas por cobros erróneos.
2. Ajustes que disminuyen el ingreso.
Idea clave: Si el ingreso no corresponde, se carga.', N'Aumenta cuando se generan ingresos operativos adicionales.
1. Cobro de servicios complementarios.
2. Venta de desechos o reciclables.
3. Comisiones operativas.
Idea clave: Todo ingreso relacionado con la operación, que no
sea venta o servicio directo, se abona aquí.'),
        (N'4201', N'Registra los ingresos obtenidos por la empresa provenientes de
actividades financieras distintas a su operación principal. Incluye
intereses cobrados, rendimientos generados en cuentas bancarias,
inversiones y otros beneficios financieros.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de ingresos financieros al final del período.
2. Ajustes o reclasificaciones contables.
3. Reversión de ingresos registrados incorrectamente.
Idea clave: Si el ingreso financiero disminuye o se cierra, se carga.', N'La cuenta aumenta cuando la empresa obtiene beneficios
financieros.
1. Cobro de intereses por inversiones.
2. Rendimientos generados en cuentas bancarias.
3. Ganancias por certificados financieros o pólizas.
4. Reconocimiento de ingresos financieros devengados.
Idea clave: Si la empresa obtiene ingresos financieros, se abona.'),
        (N'420101', N'Registra los ingresos obtenidos por la empresa provenientes del cobro
de intereses generados por préstamos otorgados, inversiones
financieras o financiamientos concedidos a terceros. Representa
beneficios económicos de carácter financiero.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables
relacionados con ingresos financieros.
1. Cierre contable de ingresos.
2. Ajustes o reclasificaciones.
3. Reversión de intereses registrados incorrectamente.
Idea clave: Si el ingreso por intereses disminuye o se cierra, se carga.', N'La cuenta aumenta cuando la empresa genera o cobra intereses
financieros.
1. Cobro de intereses por préstamos otorgados.
2. Reconocimiento de intereses devengados.
3. Rendimientos generados por inversiones financieras.
Idea clave: Si la empresa obtiene ingresos por intereses, se abona.'),
        (N'420102', N'Registra los ingresos obtenidos por la empresa provenientes de
cuentas bancarias, pólizas, depósitos a plazo fijo o inversiones
mantenidas en instituciones financieras. Representa beneficios
económicos generados por recursos depositados en bancos.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables
relacionados con ingresos financieros.
1. Cierre contable de ingresos.
2. Ajustes o reclasificaciones contables.
3. Reversión de rendimientos registrados incorrectamente.
Idea clave: Si el rendimiento bancario disminuye o se cierra, se carga.', N'La cuenta aumenta cuando la empresa recibe o reconoce
rendimientos generados por entidades financieras.
1. Intereses acreditados por el banco.
2. Rendimientos de pólizas o depósitos a plazo.
3. Ganancias generadas en cuentas de ahorro empresariales.
4. Reconocimiento de rendimientos devengados.
Idea clave: Si la empresa obtiene ganancias financieras bancarias, se
abona.'),
        (N'4202', N'Registra ingresos ocasionales o secundarios obtenidos por la empresa
que no provienen directamente de su actividad principal ni de
operaciones financieras específicas. Incluye ingresos no frecuentes y
conceptos varios no clasificados en otras cuentas.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de ingresos al final del período.
2. Ajustes o reclasificaciones contables.
3. Reversión de ingresos registrados incorrectamente.
Idea clave: Si el ingreso diverso disminuye o se cierra, se carga.', N'La cuenta aumenta cuando la empresa obtiene ingresos ocasionales o
no clasificados en otras cuentas.
1. Recuperación de gastos.
2. Cobro de penalidades o recargos.
3. Ingresos por sobrantes de caja.4. Ingresos eventuales diversos.Idea
clave: Si la empresa obtiene ingresos adicionales u ocasionales, se
abona.'),
        (N'4203', N'Registra ingresos obtenidos por la empresa que no corresponden a la
actividad principal del negocio y que tampoco forman parte de
ingresos financieros específicos. Incluye ganancias ocasionales,
recuperaciones y otros conceptos extraordinarios o secundarios.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de ingresos al final del período.
2. Ajustes o reclasificaciones.
3. Reversión de ingresos registrados incorrectamente.
Idea clave: Si el ingreso no operacional disminuye o se cierra, se
carga.', N'La cuenta aumenta cuando la empresa obtiene ingresos ajenos a su
operación principal.
1. Recuperación de valores castigados.
2. Ganancias ocasionales.
3. Ingresos por indemnizaciones.
4. Otros ingresos eventuales no relacionados con ventas.
Idea clave: Si la empresa obtiene ingresos secundarios u
ocasionales, se abona.'),
        (N'5101', N'Registra el gasto asociado a las remuneraciones del personal
administrativo: secretaría, contabilidad, talento humano, gerencia
administrativa, coordinadores, etc. Incluye sueldos, horas extras,
recargos y cualquier otro beneficio devengado por el área
administrativa.', N'1. Registro mensual de sueldos administrativos devengados.
2. Registro de horas extras y recargos administrativos.
Idea clave: Cada vez que se genera un sueldo administrativo, se
carga.', N'1. Correcciones de nómina mal registradas.
2. Reversión de gastos administrativos registrados en exceso.'),
        (N'5102', N'Registra el valor correspondiente a las obligaciones patronales que
la empresa debe aportar al IESS por su personal administrativo.
Este gasto forma parte del costo laboral total del empleado.', N'1. Registro mensual del aporte patronal del área administrativa.
Idea clave: El aporte patronal es un gasto asociado al sueldo, por
eso se carga.', N'1. Correcciones de aportes mal calculados.
2. Ajustes por errores de registro.'),
        (N'5103', N'Comprende el gasto por consumo de luz, agua, internet, teléfono y
otros servicios esenciales utilizados por el área administrativa para el
funcionamiento de la oficina.', N'1. Registro del consumo mensual de servicios básicos.
2. Facturas emitidas por proveedores de servicios.
Idea clave: Todo servicio usado para operar oficinas se carga aquí.', N'1. Anulación o corrección de facturas mal registradas.'),
        (N'5104', N'Registra el gasto por el uso de oficinas, bodegas, locales u otros
espacios arrendados necesarios para la administración. Incluye
contratos mensuales o anuales.', N'1. Registro de la factura mensual de arriendo.
2. Pagos anticipados de alquiler administrativo.
Idea clave: El arriendo de las oficinas es un gasto administrativo.', N'1. Correcciones por pagos duplicados o mal registrados.'),
        (N'5105', N'Registra el gasto por la adquisición de materiales de oficina: hojas,
lapiceros, carpetas, tóner, suministros de impresión y otros insumos
utilizados por el personal administrativo.', N'1. Compras de útiles y suministros de oficina.
Idea clave: Todo material usado por la oficina es gasto administrativo.', N'1. Devoluciones o correcciones de compras mal registradas.'),
        (N'5106', N'Registra el gasto del periodo por el desgaste de los activos fijos
administrativos: computadoras, impresoras, muebles, equipos de
oficina, etc.', N'1. Registro mensual de la depreciación administrativa.
Idea clave: Es un gasto contable, no implica salida de dinero.', N'1. Reversión de depreciación mal calculada.'),
        (N'5107', N'Registra el gasto de uso de activos intangibles administrativos:
software, licencias, marcas, patentes u otros intangibles vinculados al
área administrativa.', N'1. Reconocimiento de la amortización del periodo.
Idea clave: El software también “se gasta” con el tiempo.', N'1. Ajustes o correcciones de amortización mal registrada.'),
        (N'5108', N'Registra pagos a profesionales externos que prestan servicios
administrativos, como abogados, contadores, auditores, consultores,
asesores, ingenieros, etc.', N'1. Pago o devengamiento de honorarios de profesionales externos.
Idea clave: Si el profesional presta un servicio a la oficina, se carga.', N'1. Correcciones o notas de crédito del proveedor.'),
        (N'5109', N'Registra los pagos efectuados a notarías por servicios relacionados con
la legalización y formalización de actos jurídicos y administrativos de la
empresa. Incluye, entre otros, certificación de documentos,
reconocimiento de firmas, protocolización de contratos, elaboración y
elevación a escritura pública, declaraciones juramentadas,
autenticación de copias, poderes especiales o generales y demás
trámites notariales requeridos para la gestión empresarial.', N'1. Pago o devengamiento de servicios notariales recibidos por la
empresa.
Idea clave: Si la empresa contrata o recibe un servicio notarial, se
carga esta cuenta.', N'1. Correcciones, anulaciones o notas de crédito relacionadas con
servicios notariales registrados previamente.'),
        (N'5110', N'Registra los pagos por servicios de apoyo necesarios para el
funcionamiento y conservación de las instalaciones de la empresa,
prestados por terceros.
Limpieza y Mantenimiento. - gastos por servicios de limpieza y
mantenimiento de las instalaciones de la empresa. (limpieza de
oficinas, reparación de puertas, mantenimiento de aires
acondicionados)
Seguridad y Vigilancia. - gastos por servicios destinados a la
protección y vigilancia de la empresa. (guardianía, monitoreo de
cámaras, servicio de alarmas)
Jardinería. - gastos por el cuidado y mantenimiento de jardines y áreas
verdes. (poda de árboles, corte de césped, mantenimiento de jardines)
Mantenimiento de áreas comunes. - gastos por la conservación y
reparación de espacios de uso común. (mantenimiento de pasillos,
parqueaderos, salas de espera y patios)', N'1. Pago o devengamiento de servicios generales contratados por la
empresa.
Idea clave: Si la empresa recibe servicios de limpieza, mantenimiento,
seguridad o jardinería, se carga esta cuenta o la subcuenta
correspondiente.', N'1. Correcciones, anulaciones o notas de crédito relacionadas con
servicios generales registrados previamente.'),
        (N'5111', N'Registra la adquisición de materiales e insumos utilizados para el aseo,
higiene y mantenimiento de las instalaciones de la empresa. Incluye
productos como detergentes, desinfectantes, cloro, jabón líquido,
papel higiénico, ambientadores, fundas de basura, escobas,
trapeadores, cepillos, paños de limpieza y demás artículos destinados
a conservar en condiciones adecuadas las áreas de trabajo.', N'1. Compra o consumo de materiales de limpieza para uso de la
empresa.
Idea clave: Si la empresa adquiere insumos destinados al aseo e
higiene de sus instalaciones, se carga esta cuenta.', N'1. Devoluciones, correcciones o ajustes relacionados con materiales
de limpieza registrados previamente.'),
        (N'5201', N'Registra gastos destinados a promocionar los productos o servicios
de la empresa: anuncios en redes sociales, medios tradicionales,
material publicitario, campañas promocionales, diseño gráfico,
impresión de volantes y banners.
Estos gastos ayudan a aumentar la visibilidad y las ventas.', N'1. Pago de campañas publicitarias.
2. Contratación de publicidad en medios.
3. Adquisición de materiales promocionales.
Idea clave: Cada gasto que sirve para promocionar se carga aquí.', N'1. Notas de crédito o devoluciones.
2. Correcciones por errores de registro.'),
        (N'5202', N'Registra los valores pagados a vendedores, agentes o intermediarios
como recompensa por las ventas realizadas. Son gastos
directamente ligados al nivel de ventas.', N'1. Devengamiento de comisiones por ventas.
2. Pagos de comisiones a vendedores.
Idea clave: Mientras más se vende, más comisiones se registran.', N'1. Correcciones de comisiones mal calculadas.
2. Reversión por ventas anuladas.'),
        (N'5203', N'Registra los gastos por movilización relacionados con la distribución
de bienes o servicios: entrega a clientes, transporte de mercaderías,
movilización del equipo de ventas y viáticos de vendedores.', N'1. Pago de transporte para entregas.
2. Viáticos del equipo de ventas.
3. Fletes por distribución de productos.
Idea clave: Todo gasto para llevar el producto al cliente se carga aquí.', N'1. Reversiones de pagos duplicados.
2. Ajustes por errores en la facturación.'),
        (N'5204', N'Registra el gasto por adquisición de cajas, fundas, envases,
etiquetas, protectores y cualquier material usado para empacar o
presentar productos para su venta.
Es indispensable cuando los productos requieren presentación o
protección.', N'1. Compra de material de empaque.
2. Gastos de presentación de productos.
Idea clave: Todo lo que se usa para empacar y entregar al cliente se
carga aquí.', N'1. Reversión de compras o devoluciones.'),
        (N'5301', N'Registra los gastos originados por obligaciones financieras y
operaciones de financiamiento de la empresa. Incluye intereses
pagados, comisiones bancarias, recargos y otros costos relacionados
con préstamos o servicios financieros.', N'La cuenta aumenta cuando la empresa incurre en gastos financieros.
1. Pago de intereses bancarios.
2. Intereses por préstamos.
3. Comisiones financieras.
4. Recargos y gastos bancarios.
Idea clave: Si la empresa incurre en gastos financieros, se carga.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de cuentas de gastos.
2. Ajustes o reclasificaciones contables.
3. Reversión de gastos registrados incorrectamente.
Idea clave: Si el gasto financiero disminuye o se cierra, se abona.'),
        (N'5302', N'Registra las pérdidas generadas cuando la empresa vende un activo
fijo o bien de larga duración por un valor inferior a su valor en libros
contable. Representa una disminución económica ocasionada por la
disposición de activos de la empresa.', N'La cuenta aumenta cuando se reconoce una pérdida por la venta de
activos.
1. Venta de muebles y equipos por debajo de su valor contable.
2. Pérdidas en venta de vehículos.
3. Baja de activos con valor residual superior al precio de venta.
Idea clave: Si la empresa pierde valor en la venta de un activo, se
carga.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de cuentas de gastos.
2. Ajustes o reclasificaciones contables.
3. Reversión de pérdidas registradas incorrectamente.
Idea clave: Si la pérdida disminuye o se cierra, se abona.'),
        (N'5303', N'Registra los gastos ocasionados por sanciones, multas, intereses
moratorios, recargos o penalidades impuestas a la empresa por
incumplimientos legales, tributarios, laborales, contractuales o
administrativos.', N'La cuenta aumenta cuando la empresa incurre en multas o recargos.
1. Multas tributarias.
2. Recargos por pagos atrasados.
3. Penalidades contractuales.
4. Intereses por mora.
Idea clave: Si la empresa incurre en sanciones o recargos, se carga.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de cuentas de gastos.
2. Ajustes o reclasificaciones.
3. Reversión de multas registradas incorrectamente.
Idea clave: Si el gasto disminuye o se cierra, se abona.'),
        (N'5304', N'Registra gastos ocasionales o secundarios que no forman parte de la
actividad principal de la empresa y que no se encuentran clasificados
en otras cuentas específicas de gastos. Incluye egresos eventuales de
diferente naturaleza.', N'La cuenta aumenta cuando la empresa incurre en gastos no
clasificados en otras cuentas.
1. Gastos ocasionales.
2. Egresos menores diversos.
3. Pagos no recurrentes.
4. Ajustes o pérdidas menores eventuales.
Idea clave: Si la empresa incurre en gastos diversos, se carga.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de cuentas de gastos.
2. Ajustes o reclasificaciones contables.
3. Reversión de gastos registrados incorrectamente.
Idea clave: Si el gasto disminuye o se cierra, se abona.'),
        (N'5305', N'Registra gastos y egresos que no corresponden a la actividad principal
de la empresa y que no se encuentran clasificados en otras cuentas
específicas de gastos no operacionales. Incluye pérdidas, ajustes y
desembolsos ocasionales de carácter extraordinario o secundario.', N'La cuenta aumenta cuando la empresa incurre en gastos no
operacionales adicionales.
1. Egresos ocasionales no relacionados con ventas.
2. Ajustes extraordinarios.
3. Pérdidas eventuales diversas.
4. Gastos secundarios no clasificados.
Idea clave: Si la empresa incurre en gastos no operacionales, se carga.', N'La cuenta disminuye al cierre del ejercicio o por ajustes contables.
1. Cierre de cuentas de gastos.
2. Ajustes o reclasificaciones.
3. Reversión de gastos registrados incorrectamente.
Idea clave: Si el gasto disminuye o se cierra, se abona.');

    IF (SELECT COUNT(*) FROM @DetalleCatalogo) <> 69
    BEGIN
        THROW 50110, 'No se prepararon los 69 detalles educativos esperados.', 1;
    END;

    INSERT INTO dbo.DetalleCuenta
        (idCuentaContable, descripcionDetalle, dinamicaDebitoDetalle,
         dinamicaCreditoDetalle)
    SELECT
        cuenta.idCuentaContable,
        detalle.descripcionDetalle,
        detalle.dinamicaDebitoDetalle,
        detalle.dinamicaCreditoDetalle
    FROM @DetalleCatalogo AS detalle
    INNER JOIN dbo.CuentaContable AS cuenta
        ON cuenta.codigoCuenta = detalle.codigoCuenta;

    IF @@ROWCOUNT <> 69
    BEGIN
        THROW 50111, 'No se insertaron los 69 detalles educativos esperados.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.CuentaContable AS cuenta
        WHERE (cuenta.permiteMovimientoCuenta = 0
               AND NOT EXISTS
                   (SELECT 1 FROM dbo.CuentaContable AS hija
                    WHERE hija.idCuentaPadre = cuenta.idCuentaContable))
           OR (cuenta.permiteMovimientoCuenta = 1
               AND EXISTS
                   (SELECT 1 FROM dbo.CuentaContable AS hija
                    WHERE hija.idCuentaPadre = cuenta.idCuentaContable))
    )
    BEGIN
        THROW 50112, 'La configuración inicial de movimiento no coincide con la jerarquía.', 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
