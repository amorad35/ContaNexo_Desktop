/*
    ContaNexo Desktop - Primera version de la base de datos
    Motor objetivo: SQL Server Express / LocalDB

    El script esta pensado para ejecutarse desde una instancia donde
    ContaNexoDB todavia no exista.
*/

SET NOEXEC OFF;
GO

USE [master];
GO

IF DB_ID(N'ContaNexoDB') IS NOT NULL
BEGIN
    RAISERROR(
        'ContaNexoDB ya existe. Este script solo crea la base de datos desde cero.',
        16,
        1
    );
    SET NOEXEC ON;
END
ELSE
BEGIN TRY
    EXEC (N'CREATE DATABASE [ContaNexoDB];');
END TRY
BEGIN CATCH
    DECLARE @mensajeCreacion NVARCHAR(2048) = ERROR_MESSAGE();

    RAISERROR(
        'No se pudo crear ContaNexoDB. El esquema no se ejecutara. Error: %s',
        16,
        1,
        @mensajeCreacion
    );
    SET NOEXEC ON;
END CATCH;
GO

USE [ContaNexoDB];
GO

IF DB_NAME() <> N'ContaNexoDB'
BEGIN
    DECLARE @baseActual SYSNAME = COALESCE(DB_NAME(), N'<desconocida>');

    RAISERROR(
        'Contexto de base de datos inseguro: se esperaba ContaNexoDB y se obtuvo %s. El esquema no se ejecutara.',
        16,
        1,
        @baseActual
    );
    SET NOEXEC ON;
END;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/* 1. TABLAS */

CREATE TABLE dbo.Empresa
(
    idEmpresa          INT IDENTITY(1,1) NOT NULL,
    nombreEmpresa      NVARCHAR(150) NOT NULL,
    rucEmpresa         VARCHAR(13) NULL,
    direccionEmpresa   NVARCHAR(200) NULL,
    telefonoEmpresa    VARCHAR(20) NULL,
    correoEmpresa      NVARCHAR(150) NULL
);
GO

CREATE TABLE dbo.ElementoContable
(
    idElementoContable INT IDENTITY(1,1) NOT NULL,
    codigoElemento     VARCHAR(2) NOT NULL,
    nombreElemento     NVARCHAR(50) NOT NULL,
    estadoElemento     BIT NOT NULL
);
GO

CREATE TABLE dbo.GrupoContable
(
    idGrupoContable    INT IDENTITY(1,1) NOT NULL,
    idElementoContable INT NOT NULL,
    codigoGrupo        VARCHAR(4) NOT NULL,
    nombreGrupo        NVARCHAR(100) NOT NULL,
    estadoGrupo        BIT NOT NULL
);
GO

CREATE TABLE dbo.CuentaContable
(
    idCuentaContable       INT IDENTITY(1,1) NOT NULL,
    idGrupoContable        INT NOT NULL,
    idCuentaPadre          INT NULL,
    codigoCuenta           VARCHAR(20) NOT NULL,
    nombreCuenta           NVARCHAR(150) NOT NULL,
    naturalezaCuenta       VARCHAR(10) NOT NULL,
    permiteMovimientoCuenta BIT NOT NULL,
    estadoCuenta           BIT NOT NULL,
    ordenCuenta            INT NOT NULL
);
GO

CREATE TABLE dbo.DetalleCuenta
(
    idDetalleCuenta        INT IDENTITY(1,1) NOT NULL,
    idCuentaContable       INT NOT NULL,
    descripcionDetalle     NVARCHAR(MAX) NULL,
    dinamicaDebitoDetalle  NVARCHAR(MAX) NULL,
    dinamicaCreditoDetalle NVARCHAR(MAX) NULL
);
GO

CREATE TABLE dbo.PeriodoContable
(
    idPeriodoContable  INT IDENTITY(1,1) NOT NULL,
    idEmpresa          INT NOT NULL,
    nombrePeriodo      NVARCHAR(100) NOT NULL,
    fechaInicioPeriodo DATE NOT NULL,
    fechaFinPeriodo    DATE NOT NULL,
    estadoPeriodo      VARCHAR(10) NOT NULL,
    fechaCierrePeriodo DATETIME2 NULL
);
GO

CREATE TABLE dbo.Asiento
(
    idAsiento          INT IDENTITY(1,1) NOT NULL,
    idPeriodoContable  INT NOT NULL,
    numeroAsiento      INT NOT NULL,
    fechaAsiento       DATE NOT NULL,
    tipoAsiento        VARCHAR(10) NOT NULL,
    descripcionAsiento NVARCHAR(500) NULL,
    estadoAsiento      VARCHAR(10) NOT NULL
);
GO

CREATE TABLE dbo.DetalleAsiento
(
    idDetalleAsiento  INT IDENTITY(1,1) NOT NULL,
    idAsiento         INT NOT NULL,
    idCuentaContable  INT NOT NULL,
    debeDetalle       DECIMAL(18,2) NOT NULL,
    haberDetalle      DECIMAL(18,2) NOT NULL,
    ordenDetalle      SMALLINT NOT NULL
);
GO

/* 2. CLAVES PRIMARIAS */

ALTER TABLE dbo.Empresa
    ADD CONSTRAINT PK_Empresa PRIMARY KEY CLUSTERED (idEmpresa);
ALTER TABLE dbo.ElementoContable
    ADD CONSTRAINT PK_ElementoContable PRIMARY KEY CLUSTERED (idElementoContable);
ALTER TABLE dbo.GrupoContable
    ADD CONSTRAINT PK_GrupoContable PRIMARY KEY CLUSTERED (idGrupoContable);
ALTER TABLE dbo.CuentaContable
    ADD CONSTRAINT PK_CuentaContable PRIMARY KEY CLUSTERED (idCuentaContable);
ALTER TABLE dbo.DetalleCuenta
    ADD CONSTRAINT PK_DetalleCuenta PRIMARY KEY CLUSTERED (idDetalleCuenta);
ALTER TABLE dbo.PeriodoContable
    ADD CONSTRAINT PK_PeriodoContable PRIMARY KEY CLUSTERED (idPeriodoContable);
ALTER TABLE dbo.Asiento
    ADD CONSTRAINT PK_Asiento PRIMARY KEY CLUSTERED (idAsiento);
ALTER TABLE dbo.DetalleAsiento
    ADD CONSTRAINT PK_DetalleAsiento PRIMARY KEY CLUSTERED (idDetalleAsiento);
GO

/* 3. CLAVES FORANEAS */

ALTER TABLE dbo.GrupoContable
    ADD CONSTRAINT FK_GrupoContable_ElementoContable
        FOREIGN KEY (idElementoContable)
        REFERENCES dbo.ElementoContable (idElementoContable);

ALTER TABLE dbo.CuentaContable
    ADD CONSTRAINT FK_CuentaContable_GrupoContable
        FOREIGN KEY (idGrupoContable)
        REFERENCES dbo.GrupoContable (idGrupoContable);

ALTER TABLE dbo.CuentaContable
    ADD CONSTRAINT FK_CuentaContable_CuentaPadre
        FOREIGN KEY (idCuentaPadre)
        REFERENCES dbo.CuentaContable (idCuentaContable);

ALTER TABLE dbo.DetalleCuenta
    ADD CONSTRAINT FK_DetalleCuenta_CuentaContable
        FOREIGN KEY (idCuentaContable)
        REFERENCES dbo.CuentaContable (idCuentaContable);

ALTER TABLE dbo.PeriodoContable
    ADD CONSTRAINT FK_PeriodoContable_Empresa
        FOREIGN KEY (idEmpresa)
        REFERENCES dbo.Empresa (idEmpresa);

ALTER TABLE dbo.Asiento
    ADD CONSTRAINT FK_Asiento_PeriodoContable
        FOREIGN KEY (idPeriodoContable)
        REFERENCES dbo.PeriodoContable (idPeriodoContable);

ALTER TABLE dbo.DetalleAsiento
    ADD CONSTRAINT FK_DetalleAsiento_Asiento
        FOREIGN KEY (idAsiento)
        REFERENCES dbo.Asiento (idAsiento);

ALTER TABLE dbo.DetalleAsiento
    ADD CONSTRAINT FK_DetalleAsiento_CuentaContable
        FOREIGN KEY (idCuentaContable)
        REFERENCES dbo.CuentaContable (idCuentaContable);
GO

/* 4. RESTRICCIONES UNIQUE */

ALTER TABLE dbo.ElementoContable
    ADD CONSTRAINT UQ_ElementoContable_codigoElemento UNIQUE (codigoElemento);
ALTER TABLE dbo.GrupoContable
    ADD CONSTRAINT UQ_GrupoContable_codigoGrupo UNIQUE (codigoGrupo);
ALTER TABLE dbo.CuentaContable
    ADD CONSTRAINT UQ_CuentaContable_codigoCuenta UNIQUE (codigoCuenta);
ALTER TABLE dbo.DetalleCuenta
    ADD CONSTRAINT UQ_DetalleCuenta_idCuentaContable UNIQUE (idCuentaContable);
ALTER TABLE dbo.Asiento
    ADD CONSTRAINT UQ_Asiento_idPeriodoContable_numeroAsiento
        UNIQUE (idPeriodoContable, numeroAsiento);
GO

/* 5. RESTRICCIONES CHECK */

ALTER TABLE dbo.CuentaContable
    ADD CONSTRAINT CK_CuentaContable_naturalezaCuenta
        CHECK (naturalezaCuenta IN ('Deudora', 'Acreedora'));

ALTER TABLE dbo.PeriodoContable
    ADD CONSTRAINT CK_PeriodoContable_estadoPeriodo
        CHECK (estadoPeriodo IN ('Abierto', 'Cerrado'));

ALTER TABLE dbo.PeriodoContable
    ADD CONSTRAINT CK_PeriodoContable_rangoFechas
        CHECK (fechaInicioPeriodo <= fechaFinPeriodo);

ALTER TABLE dbo.Asiento
    ADD CONSTRAINT CK_Asiento_tipoAsiento
        CHECK (tipoAsiento IN ('Normal', 'Ajuste'));

ALTER TABLE dbo.DetalleAsiento
    ADD CONSTRAINT CK_DetalleAsiento_debeHaber
        CHECK
        (
            (debeDetalle > 0 AND haberDetalle = 0)
            OR
            (debeDetalle = 0 AND haberDetalle > 0)
        );
GO

/* 6. VALORES PREDETERMINADOS */

ALTER TABLE dbo.ElementoContable
    ADD CONSTRAINT DF_ElementoContable_estadoElemento DEFAULT (1) FOR estadoElemento;
ALTER TABLE dbo.GrupoContable
    ADD CONSTRAINT DF_GrupoContable_estadoGrupo DEFAULT (1) FOR estadoGrupo;
ALTER TABLE dbo.CuentaContable
    ADD CONSTRAINT DF_CuentaContable_estadoCuenta DEFAULT (1) FOR estadoCuenta;
ALTER TABLE dbo.DetalleAsiento
    ADD CONSTRAINT DF_DetalleAsiento_debeDetalle DEFAULT (0) FOR debeDetalle;
ALTER TABLE dbo.DetalleAsiento
    ADD CONSTRAINT DF_DetalleAsiento_haberDetalle DEFAULT (0) FOR haberDetalle;
GO

/* 7. INDICES PARA RELACIONES Y CONSULTAS FRECUENTES */

CREATE INDEX IX_GrupoContable_idElementoContable
    ON dbo.GrupoContable (idElementoContable);

CREATE INDEX IX_CuentaContable_idGrupoContable
    ON dbo.CuentaContable (idGrupoContable);

CREATE INDEX IX_CuentaContable_idCuentaPadre
    ON dbo.CuentaContable (idCuentaPadre)
    WHERE idCuentaPadre IS NOT NULL;

CREATE INDEX IX_PeriodoContable_idEmpresa_fechas
    ON dbo.PeriodoContable (idEmpresa, fechaInicioPeriodo, fechaFinPeriodo);

CREATE INDEX IX_Asiento_idPeriodoContable_fechaAsiento
    ON dbo.Asiento (idPeriodoContable, fechaAsiento);

CREATE INDEX IX_DetalleAsiento_idAsiento_ordenDetalle
    ON dbo.DetalleAsiento (idAsiento, ordenDetalle);

CREATE INDEX IX_DetalleAsiento_idCuentaContable
    ON dbo.DetalleAsiento (idCuentaContable);
GO

/* 8. DATOS INICIALES */

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.ElementoContable
        (codigoElemento, nombreElemento)
    VALUES
        ('1', N'Activo'),
        ('2', N'Pasivo'),
        ('3', N'Patrimonio'),
        ('4', N'Ingresos'),
        ('5', N'Gastos');

    INSERT INTO dbo.GrupoContable
        (idElementoContable, codigoGrupo, nombreGrupo)
    SELECT
        elemento.idElementoContable,
        datos.codigoGrupo,
        datos.nombreGrupo
    FROM
    (
        VALUES
            ('1', '11', N'Activo Corriente'),
            ('1', '12', N'Activo No Corriente'),
            ('2', '21', N'Pasivo Corriente'),
            ('2', '22', N'Pasivo No Corriente'),
            ('3', '31', N'Capital Contable'),
            ('3', '32', N'Resultados y Reservas'),
            ('4', '41', N'Ingresos Operacionales'),
            ('4', '42', N'Ingresos No Operacionales'),
            ('5', '51', N'Gastos Operativos'),
            ('5', '52', N'Gastos de Ventas'),
            ('5', '53', N'Gastos No Operacionales')
    ) AS datos (codigoElemento, codigoGrupo, nombreGrupo)
    INNER JOIN dbo.ElementoContable AS elemento
        ON elemento.codigoElemento = datos.codigoElemento;

    IF @@ROWCOUNT <> 11
    BEGIN
        THROW 50002, 'No se insertaron todos los grupos contables esperados.', 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SET NOEXEC OFF;
GO

/*
DISCREPANCIAS DEL CATALOGO PENDIENTES DE REVISION

El catalogo fue auditado y contiene contradicciones internas que no permiten
elegir de forma inequivoca algunos codigos y nombres. El detalle verificable,
con las paginas en conflicto, se conserva en Database/README.md. Ninguna de
estas contradicciones altera los elementos o grupos sembrados en esta etapa.
*/
