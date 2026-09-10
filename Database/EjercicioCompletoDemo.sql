/*
    ContaNexo Desktop - Ejercicio contable integral de demostración
    Base objetivo: ContaNexoDB
    Recomendado: ejecutar después de Database/ResetDemoData.sql.
*/

USE [master];
GO

IF DB_ID(N'ContaNexoDB') IS NULL
BEGIN
    THROW 56001, 'ContaNexoDB no existe. Ejecute primero los scripts de instalación y reset.', 1;
END;
GO

USE [ContaNexoDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'ContaNexoDB'
BEGIN
    THROW 56002, 'Base de datos incorrecta. EjercicioCompletoDemo.sql solo puede ejecutarse en ContaNexoDB.', 1;
END;
GO

/* Variables editables. Sus tipos y longitudes coinciden con el esquema real. */
DECLARE @NombreEmpresa NVARCHAR(150) = N'Fabro Corporation S.A.';
DECLARE @RucEmpresa VARCHAR(13) = '1799999999001'; -- RUC ficticio de demostración.
DECLARE @DireccionEmpresa NVARCHAR(200) = N'Quito, Ecuador';
DECLARE @TelefonoEmpresa VARCHAR(20) = '022000000';
DECLARE @CorreoEmpresa NVARCHAR(150) = N'demo@fabro.example';
DECLARE @NombrePeriodo NVARCHAR(100) = N'Ejercicio Demo 2026';
DECLARE @FechaInicio DATE = DATEFROMPARTS(2026, 1, 1);
DECLARE @FechaFin DATE = DATEFROMPARTS(2026, 12, 31);

DECLARE @IdEmpresa INT;
DECLARE @IdPeriodoContable INT;
DECLARE @CantidadAsientos INT;
DECLARE @CantidadDetalles INT;
DECLARE @TotalDebe DECIMAL(38,2);
DECLARE @TotalHaber DECIMAL(38,2);

DECLARE @CuentasRequeridas TABLE
(
    codigoCuenta VARCHAR(20) NOT NULL PRIMARY KEY,
    codigoGrupo VARCHAR(4) NOT NULL,
    codigoCuentaPadre VARCHAR(20) NULL,
    nombreCuenta NVARCHAR(150) NOT NULL,
    naturalezaCuenta VARCHAR(10) NOT NULL,
    ordenCuenta INT NOT NULL,
    idCuentaContable INT NULL
);

INSERT INTO @CuentasRequeridas
    (codigoCuenta, codigoGrupo, codigoCuentaPadre, nombreCuenta,
     naturalezaCuenta, ordenCuenta)
VALUES
    ('1101',   '11', NULL,   N'Caja',                                  'Deudora',   1),
    ('1103',   '11', NULL,   N'Bancos',                                'Deudora',   3),
    ('1105',   '11', NULL,   N'Inventarios',                           'Deudora',   5),
    ('1107',   '11', NULL,   N'Cuentas por Cobrar Clientes',           'Deudora',   7),
    ('120103', '12', '1201', N'Equipos de Oficina',                    'Deudora',  19),
    ('120104', '12', '1201', N'Equipos de Computación',                'Deudora',  20),
    ('120106', '12', '1201', N'Muebles y Enseres',                     'Deudora',  22),
    ('120202', '12', '1202', N'Deprec. Acum. Equipos de Oficina',      'Acreedora',26),
    ('120301', '12', '1203', N'Licencias',                             'Deudora',  29),
    ('2101',   '21', NULL,   N'Proveedores',                           'Acreedora',35),
    ('2103',   '21', NULL,   N'Cuentas por Pagar',                     'Acreedora',37),
    ('2104',   '21', NULL,   N'Obligaciones con IESS',                 'Acreedora',38),
    ('2106',   '21', NULL,   N'Sueldos y Salarios por Pagar',          'Acreedora',42),
    ('3101',   '31', NULL,   N'Capital Social',                        'Acreedora',53),
    ('4101',   '41', NULL,   N'Ventas',                                'Acreedora',59),
    ('4102',   '41', NULL,   N'Ingresos por Servicios',                'Acreedora',60),
    ('420101', '42', '4201', N'Intereses Ganados',                     'Acreedora',63),
    ('5101',   '51', NULL,   N'Sueldos y Salarios Administrativos',    'Deudora',  67),
    ('5102',   '51', NULL,   N'Aportes Patronales',                    'Deudora',  68),
    ('5103',   '51', NULL,   N'Servicios Básicos',                     'Deudora',  69),
    ('5104',   '51', NULL,   N'Arrendamientos',                        'Deudora',  70),
    ('5105',   '51', NULL,   N'Papelería y Útiles de Oficina',         'Deudora',  71),
    ('5106',   '51', NULL,   N'Depreciaciones',                        'Deudora',  72),
    ('5108',   '51', NULL,   N'Honorarios Profesionales',              'Deudora',  74),
    ('5111',   '51', NULL,   N'Materiales de Limpieza',                'Deudora',  81),
    ('5201',   '52', NULL,   N'Publicidad y Propaganda',               'Deudora',  82),
    ('5203',   '52', NULL,   N'Gastos de Transporte',                  'Deudora',  84),
    ('5301',   '53', NULL,   N'Gastos Financieros',                    'Deudora',  86);

DECLARE @Movimientos TABLE
(
    numeroAsiento INT NOT NULL,
    fechaAsiento DATE NOT NULL,
    tipoAsiento VARCHAR(10) NOT NULL,
    descripcionAsiento NVARCHAR(500) NOT NULL,
    ordenDetalle SMALLINT NOT NULL,
    codigoCuenta VARCHAR(20) NOT NULL,
    debeDetalle DECIMAL(18,2) NOT NULL,
    haberDetalle DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (numeroAsiento, ordenDetalle)
);

INSERT INTO @Movimientos
    (numeroAsiento, fechaAsiento, tipoAsiento, descripcionAsiento,
     ordenDetalle, codigoCuenta, debeDetalle, haberDetalle)
VALUES
    (1,  '20260105', 'Normal', N'Aporte inicial de capital depositado en bancos', 1, '1103', 100000.00, 0.00),
    (1,  '20260105', 'Normal', N'Aporte inicial de capital depositado en bancos', 2, '3101', 0.00, 100000.00),

    (2,  '20260108', 'Normal', N'Constitución de fondo de caja', 1, '1101', 10000.00, 0.00),
    (2,  '20260108', 'Normal', N'Constitución de fondo de caja', 2, '1103', 0.00, 10000.00),

    (3,  '20260115', 'Normal', N'Compra de inventarios a crédito', 1, '1105', 15000.00, 0.00),
    (3,  '20260115', 'Normal', N'Compra de inventarios a crédito', 2, '2101', 0.00, 15000.00),

    (4,  '20260120', 'Normal', N'Compra de inventarios pagada por banco', 1, '1105', 5000.00, 0.00),
    (4,  '20260120', 'Normal', N'Compra de inventarios pagada por banco', 2, '1103', 0.00, 5000.00),

    (5,  '20260203', 'Normal', N'Adquisición de equipos de oficina', 1, '120103', 8000.00, 0.00),
    (5,  '20260203', 'Normal', N'Adquisición de equipos de oficina', 2, '1103', 0.00, 8000.00),

    (6,  '20260210', 'Normal', N'Adquisición a crédito de equipos de computación', 1, '120104', 12000.00, 0.00),
    (6,  '20260210', 'Normal', N'Adquisición a crédito de equipos de computación', 2, '2103', 0.00, 12000.00),

    (7,  '20260218', 'Normal', N'Compra de muebles y enseres mediante banco', 1, '120106', 6000.00, 0.00),
    (7,  '20260218', 'Normal', N'Compra de muebles y enseres mediante banco', 2, '1103', 0.00, 6000.00),

    (8,  '20260304', 'Normal', N'Prestación de servicios cobrada en efectivo', 1, '1101', 7000.00, 0.00),
    (8,  '20260304', 'Normal', N'Prestación de servicios cobrada en efectivo', 2, '4102', 0.00, 7000.00),

    (9,  '20260315', 'Normal', N'Venta al contado', 1, '1101', 12000.00, 0.00),
    (9,  '20260315', 'Normal', N'Venta al contado', 2, '4101', 0.00, 12000.00),

    (10, '20260402', 'Normal', N'Venta a crédito a clientes', 1, '1107', 18000.00, 0.00),
    (10, '20260402', 'Normal', N'Venta a crédito a clientes', 2, '4101', 0.00, 18000.00),

    (11, '20260417', 'Normal', N'Servicios prestados a crédito', 1, '1107', 9000.00, 0.00),
    (11, '20260417', 'Normal', N'Servicios prestados a crédito', 2, '4102', 0.00, 9000.00),

    (12, '20260505', 'Normal', N'Cobro a clientes mediante transferencia bancaria', 1, '1103', 15000.00, 0.00),
    (12, '20260505', 'Normal', N'Cobro a clientes mediante transferencia bancaria', 2, '1107', 0.00, 15000.00),

    (13, '20260520', 'Normal', N'Cobro en efectivo a clientes', 1, '1101', 5000.00, 0.00),
    (13, '20260520', 'Normal', N'Cobro en efectivo a clientes', 2, '1107', 0.00, 5000.00),

    (14, '20260603', 'Normal', N'Pago parcial a proveedores mediante banco', 1, '2101', 10000.00, 0.00),
    (14, '20260603', 'Normal', N'Pago parcial a proveedores mediante banco', 2, '1103', 0.00, 10000.00),

    (15, '20260618', 'Normal', N'Pago parcial de cuentas por pagar', 1, '2103', 6000.00, 0.00),
    (15, '20260618', 'Normal', N'Pago parcial de cuentas por pagar', 2, '1103', 0.00, 6000.00),

    (16, '20260701', 'Normal', N'Pago de arrendamiento de oficinas', 1, '5104', 3500.00, 0.00),
    (16, '20260701', 'Normal', N'Pago de arrendamiento de oficinas', 2, '1103', 0.00, 3500.00),

    (17, '20260712', 'Normal', N'Pago de servicios básicos', 1, '5103', 1800.00, 0.00),
    (17, '20260712', 'Normal', N'Pago de servicios básicos', 2, '1103', 0.00, 1800.00),

    (18, '20260728', 'Normal', N'Registro y pago parcial de nómina administrativa', 1, '5101', 8000.00, 0.00),
    (18, '20260728', 'Normal', N'Registro y pago parcial de nómina administrativa', 2, '1103', 0.00, 6500.00),
    (18, '20260728', 'Normal', N'Registro y pago parcial de nómina administrativa', 3, '2106', 0.00, 1500.00),

    (19, '20260803', 'Normal', N'Registro de aportes patronales por pagar', 1, '5102', 1200.00, 0.00),
    (19, '20260803', 'Normal', N'Registro de aportes patronales por pagar', 2, '2104', 0.00, 1200.00),

    (20, '20260816', 'Normal', N'Campaña de publicidad pagada por banco', 1, '5201', 2500.00, 0.00),
    (20, '20260816', 'Normal', N'Campaña de publicidad pagada por banco', 2, '1103', 0.00, 2500.00),

    (21, '20260902', 'Normal', N'Gastos de transporte pagados en efectivo', 1, '5203', 1100.00, 0.00),
    (21, '20260902', 'Normal', N'Gastos de transporte pagados en efectivo', 2, '1101', 0.00, 1100.00),

    (22, '20260914', 'Normal', N'Compra de papelería y útiles de oficina', 1, '5105', 650.00, 0.00),
    (22, '20260914', 'Normal', N'Compra de papelería y útiles de oficina', 2, '1101', 0.00, 650.00),

    (23, '20261001', 'Normal', N'Compra de materiales de limpieza', 1, '5111', 400.00, 0.00),
    (23, '20261001', 'Normal', N'Compra de materiales de limpieza', 2, '1101', 0.00, 400.00),

    (24, '20261015', 'Normal', N'Intereses ganados acreditados por el banco', 1, '1103', 750.00, 0.00),
    (24, '20261015', 'Normal', N'Intereses ganados acreditados por el banco', 2, '420101', 0.00, 750.00),

    (25, '20261101', 'Ajuste', N'Depreciación periódica de equipos de oficina', 1, '5106', 1800.00, 0.00),
    (25, '20261101', 'Ajuste', N'Depreciación periódica de equipos de oficina', 2, '120202', 0.00, 1800.00),

    (26, '20261112', 'Ajuste', N'Registro de comisiones y gastos bancarios', 1, '5301', 300.00, 0.00),
    (26, '20261112', 'Ajuste', N'Registro de comisiones y gastos bancarios', 2, '1103', 0.00, 300.00),

    (27, '20261120', 'Normal', N'Pago de honorarios profesionales', 1, '5108', 2200.00, 0.00),
    (27, '20261120', 'Normal', N'Pago de honorarios profesionales', 2, '1103', 0.00, 2200.00),

    (28, '20261202', 'Normal', N'Pago de salarios pendientes', 1, '2106', 1500.00, 0.00),
    (28, '20261202', 'Normal', N'Pago de salarios pendientes', 2, '1103', 0.00, 1500.00),

    (29, '20261210', 'Normal', N'Pago de obligaciones con IESS', 1, '2104', 1200.00, 0.00),
    (29, '20261210', 'Normal', N'Pago de obligaciones con IESS', 2, '1103', 0.00, 1200.00),

    (30, '20261218', 'Normal', N'Adquisición de licencia de software', 1, '120301', 3000.00, 0.00),
    (30, '20261218', 'Normal', N'Adquisición de licencia de software', 2, '1103', 0.00, 3000.00);

BEGIN TRY
    BEGIN TRANSACTION;

    IF NULLIF(LTRIM(RTRIM(@NombreEmpresa)), N'') IS NULL
        THROW 56003, 'El nombre de empresa no puede estar vacío.', 1;

    IF @FechaInicio > @FechaFin
        THROW 56004, 'La fecha inicial no puede ser posterior a la fecha final.', 1;

    IF (SELECT COUNT(*) FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
        WHERE LTRIM(RTRIM(nombreEmpresa)) = LTRIM(RTRIM(@NombreEmpresa))) > 1
        THROW 56005, 'Existen varias empresas con el nombre indicado; no es seguro elegir una automáticamente.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
        WHERE LTRIM(RTRIM(nombreEmpresa)) = LTRIM(RTRIM(@NombreEmpresa))
          AND ISNULL(NULLIF(LTRIM(RTRIM(rucEmpresa)), ''), '')
              <> ISNULL(NULLIF(LTRIM(RTRIM(@RucEmpresa)), ''), '')
    )
        THROW 56018, 'La empresa indicada ya existe, pero su RUC no coincide con el RUC de demostración.', 1;

    IF NULLIF(LTRIM(RTRIM(@RucEmpresa)), '') IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
           WHERE rucEmpresa = LTRIM(RTRIM(@RucEmpresa))
             AND LTRIM(RTRIM(nombreEmpresa)) <> LTRIM(RTRIM(@NombreEmpresa))
       )
        THROW 56019, 'El RUC de demostración pertenece a otra empresa.', 1;

    SELECT @IdEmpresa = idEmpresa
    FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
    WHERE LTRIM(RTRIM(nombreEmpresa)) = LTRIM(RTRIM(@NombreEmpresa))
      AND ISNULL(NULLIF(LTRIM(RTRIM(rucEmpresa)), ''), '')
          = ISNULL(NULLIF(LTRIM(RTRIM(@RucEmpresa)), ''), '');

    IF @IdEmpresa IS NULL
    BEGIN
        INSERT INTO dbo.Empresa
            (nombreEmpresa, rucEmpresa, direccionEmpresa, telefonoEmpresa,
             correoEmpresa, logoEmpresa)
        VALUES
            (LTRIM(RTRIM(@NombreEmpresa)), NULLIF(LTRIM(RTRIM(@RucEmpresa)), ''),
             NULLIF(LTRIM(RTRIM(@DireccionEmpresa)), N''),
             NULLIF(LTRIM(RTRIM(@TelefonoEmpresa)), ''),
             NULLIF(LTRIM(RTRIM(@CorreoEmpresa)), N''), NULL);

        SET @IdEmpresa = CONVERT(INT, SCOPE_IDENTITY());
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
        WHERE idEmpresa = @IdEmpresa
          AND nombrePeriodo = @NombrePeriodo
          AND (fechaInicioPeriodo <> @FechaInicio OR fechaFinPeriodo <> @FechaFin)
    )
        THROW 56006, 'Ya existe un período con el mismo nombre pero con fechas incompatibles.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
        WHERE idEmpresa = @IdEmpresa
          AND nombrePeriodo = @NombrePeriodo
          AND fechaInicioPeriodo = @FechaInicio
          AND fechaFinPeriodo = @FechaFin
    ) > 1
        THROW 56017, 'Existen varios períodos de demostración idénticos; no es seguro elegir uno automáticamente.', 1;

    SELECT @IdPeriodoContable = idPeriodoContable
    FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
    WHERE idEmpresa = @IdEmpresa
      AND nombrePeriodo = @NombrePeriodo
      AND fechaInicioPeriodo = @FechaInicio
      AND fechaFinPeriodo = @FechaFin;

    IF @IdPeriodoContable IS NULL
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idEmpresa = @IdEmpresa
              AND @FechaInicio <= fechaFinPeriodo
              AND @FechaFin >= fechaInicioPeriodo
        )
            THROW 56007, 'El período de demostración se superpone con otro período de la empresa.', 1;

        INSERT INTO dbo.PeriodoContable
            (idEmpresa, nombrePeriodo, fechaInicioPeriodo, fechaFinPeriodo,
             estadoPeriodo, fechaCierrePeriodo)
        VALUES
            (@IdEmpresa, @NombrePeriodo, @FechaInicio, @FechaFin, 'Abierto', NULL);

        SET @IdPeriodoContable = CONVERT(INT, SCOPE_IDENTITY());
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.PeriodoContable
        WHERE idPeriodoContable = @IdPeriodoContable
          AND estadoPeriodo = 'Abierto'
    )
        THROW 56008, 'El período de demostración existe, pero está cerrado.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Asiento WITH (UPDLOCK, HOLDLOCK)
               WHERE idPeriodoContable = @IdPeriodoContable)
        THROW 56009, 'El período de demostración ya contiene asientos. No se duplicó la carga.', 1;

    UPDATE requerida
    SET idCuentaContable = cuenta.idCuentaContable
    FROM @CuentasRequeridas AS requerida
    INNER JOIN dbo.CuentaContable AS cuenta WITH (HOLDLOCK)
        ON cuenta.codigoCuenta = requerida.codigoCuenta;

    DECLARE @CodigoFaltante VARCHAR(20) =
        (SELECT TOP (1) codigoCuenta FROM @CuentasRequeridas
         WHERE idCuentaContable IS NULL ORDER BY codigoCuenta);

    IF @CodigoFaltante IS NOT NULL
    BEGIN
        DECLARE @MensajeCuenta NVARCHAR(2048) =
            N'Falta la cuenta base requerida con código ' + @CodigoFaltante + N'.';
        THROW 56010, @MensajeCuenta, 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @CuentasRequeridas AS requerida
        INNER JOIN dbo.CuentaContable AS cuenta
            ON cuenta.idCuentaContable = requerida.idCuentaContable
        INNER JOIN dbo.GrupoContable AS grupo
            ON grupo.idGrupoContable = cuenta.idGrupoContable
        LEFT JOIN dbo.CuentaContable AS padre
            ON padre.idCuentaContable = cuenta.idCuentaPadre
        WHERE cuenta.estadoCuenta = 0
           OR cuenta.permiteMovimientoCuenta = 0
           OR grupo.codigoGrupo <> requerida.codigoGrupo
           OR ISNULL(padre.codigoCuenta, '') <> ISNULL(requerida.codigoCuentaPadre, '')
           OR cuenta.nombreCuenta <> requerida.nombreCuenta
           OR cuenta.naturalezaCuenta <> requerida.naturalezaCuenta
           OR cuenta.ordenCuenta <> requerida.ordenCuenta
    )
        THROW 56011, 'Una cuenta requerida no coincide exactamente con el catálogo base o no permite movimientos.', 1;

    IF (SELECT COUNT(DISTINCT numeroAsiento) FROM @Movimientos) <> 30
        THROW 56012, 'La definición del ejercicio no contiene los 30 asientos esperados.', 1;

    IF EXISTS
    (
        SELECT 1 FROM @Movimientos
        WHERE fechaAsiento < @FechaInicio OR fechaAsiento > @FechaFin
           OR tipoAsiento NOT IN ('Normal', 'Ajuste')
           OR NOT ((debeDetalle > 0 AND haberDetalle = 0)
                   OR (debeDetalle = 0 AND haberDetalle > 0))
    )
        THROW 56013, 'La definición contiene una fecha, tipo o detalle inválido.', 1;

    IF EXISTS
    (
        SELECT numeroAsiento
        FROM @Movimientos
        GROUP BY numeroAsiento
        HAVING MIN(ordenDetalle) <> 1
            OR COUNT(*) <> COUNT(DISTINCT ordenDetalle)
            OR COUNT(DISTINCT fechaAsiento) <> 1
            OR COUNT(DISTINCT tipoAsiento) <> 1
            OR COUNT(DISTINCT descripcionAsiento) <> 1
            OR SUM(debeDetalle) <> SUM(haberDetalle)
            OR SUM(debeDetalle) <= 0
    )
        THROW 56014, 'Uno o más asientos de demostración están descuadrados.', 1;

    DECLARE @AsientosCreados TABLE
    (
        numeroAsiento INT NOT NULL PRIMARY KEY,
        idAsiento INT NOT NULL
    );

    INSERT INTO dbo.Asiento
        (idPeriodoContable, numeroAsiento, fechaAsiento, tipoAsiento,
         descripcionAsiento, estadoAsiento)
    OUTPUT inserted.numeroAsiento, inserted.idAsiento
        INTO @AsientosCreados (numeroAsiento, idAsiento)
    SELECT
        @IdPeriodoContable,
        movimiento.numeroAsiento,
        MIN(movimiento.fechaAsiento),
        MIN(movimiento.tipoAsiento),
        MIN(movimiento.descripcionAsiento),
        'Registrado'
    FROM @Movimientos AS movimiento
    GROUP BY movimiento.numeroAsiento;

    INSERT INTO dbo.DetalleAsiento
        (idAsiento, idCuentaContable, debeDetalle, haberDetalle, ordenDetalle)
    SELECT
        asiento.idAsiento,
        cuenta.idCuentaContable,
        movimiento.debeDetalle,
        movimiento.haberDetalle,
        movimiento.ordenDetalle
    FROM @Movimientos AS movimiento
    INNER JOIN @AsientosCreados AS asiento
        ON asiento.numeroAsiento = movimiento.numeroAsiento
    INNER JOIN @CuentasRequeridas AS cuenta
        ON cuenta.codigoCuenta = movimiento.codigoCuenta;

    SELECT
        @CantidadAsientos = COUNT(DISTINCT asiento.idAsiento),
        @CantidadDetalles = COUNT(*),
        @TotalDebe = SUM(detalle.debeDetalle),
        @TotalHaber = SUM(detalle.haberDetalle)
    FROM @AsientosCreados AS asiento
    INNER JOIN dbo.DetalleAsiento AS detalle
        ON detalle.idAsiento = asiento.idAsiento;

    IF @CantidadAsientos <> 30 OR @CantidadDetalles <> (SELECT COUNT(*) FROM @Movimientos)
        THROW 56015, 'La cantidad insertada no coincide con la definición del ejercicio.', 1;

    IF @TotalDebe <> @TotalHaber
        THROW 56016, 'La carga final quedó descuadrada.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    empresa.idEmpresa,
    empresa.nombreEmpresa AS EmpresaUtilizada,
    empresa.rucEmpresa,
    periodo.idPeriodoContable,
    periodo.nombrePeriodo AS PeriodoUtilizado,
    periodo.fechaInicioPeriodo,
    periodo.fechaFinPeriodo,
    periodo.estadoPeriodo,
    @CantidadAsientos AS CantidadAsientosCreados,
    @CantidadDetalles AS CantidadDetallesCreados,
    @TotalDebe AS TotalDebeGeneral,
    @TotalHaber AS TotalHaberGeneral,
    @TotalDebe - @TotalHaber AS Diferencia
FROM dbo.Empresa AS empresa
INNER JOIN dbo.PeriodoContable AS periodo
    ON periodo.idEmpresa = empresa.idEmpresa
WHERE empresa.idEmpresa = @IdEmpresa
  AND periodo.idPeriodoContable = @IdPeriodoContable;

PRINT N'Ejercicio de demostración cargado correctamente.';
GO
