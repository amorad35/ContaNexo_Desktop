/*
    ContaNexo Desktop - Restablecimiento de datos de demostración
    Ejecutar con sqlcmd desde la raíz del repositorio, o en SSMS con modo SQLCMD.
    El catálogo se restaura reutilizando literalmente el seed oficial para evitar
    mantener una segunda copia de sus 90 cuentas y 69 detalles educativos.
*/

:On Error exit
:setvar SeedCatalogoPath ".\Database\SeedCatalogoCuentas.sql"

USE [master];
GO

IF DB_ID(N'ContaNexoDB') IS NULL
BEGIN
    THROW 55001, 'ContaNexoDB no existe. No se realizó ningún cambio.', 1;
END;
GO

USE [ContaNexoDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'ContaNexoDB'
BEGIN
    THROW 55002, 'Base de datos incorrecta. ResetDemoData.sql solo puede ejecutarse en ContaNexoDB.', 1;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* Datos operativos: orden inverso de las claves foráneas. */
    DELETE FROM dbo.DetalleAsiento;
    DELETE FROM dbo.Asiento;
    DELETE FROM dbo.PeriodoContable;
    DELETE FROM dbo.Empresa;

    /* Catálogo: elimina detalles y luego las cuentas hoja hasta vaciar la jerarquía. */
    DELETE FROM dbo.DetalleCuenta;

    WHILE EXISTS (SELECT 1 FROM dbo.CuentaContable)
    BEGIN
        DELETE cuenta
        FROM dbo.CuentaContable AS cuenta
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.CuentaContable AS hija
            WHERE hija.idCuentaPadre = cuenta.idCuentaContable
        );

        IF @@ROWCOUNT = 0
            THROW 55003, 'No se pudo vaciar CuentaContable: existe una jerarquía circular.', 1;
    END;

    /* El esquema base define exactamente cinco elementos y once grupos. */
    DELETE FROM dbo.GrupoContable;
    DELETE FROM dbo.ElementoContable;

    INSERT INTO dbo.ElementoContable
        (codigoElemento, nombreElemento, estadoElemento)
    VALUES
        ('1', N'Activo', 1),
        ('2', N'Pasivo', 1),
        ('3', N'Patrimonio', 1),
        ('4', N'Ingresos', 1),
        ('5', N'Gastos', 1);

    INSERT INTO dbo.GrupoContable
        (idElementoContable, codigoGrupo, nombreGrupo, estadoGrupo)
    SELECT
        elemento.idElementoContable,
        datos.codigoGrupo,
        datos.nombreGrupo,
        1
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

    IF (SELECT COUNT(*) FROM dbo.ElementoContable) <> 5
       OR (SELECT COUNT(*) FROM dbo.GrupoContable) <> 11
        THROW 55004, 'No se restauraron todos los elementos y grupos base.', 1;

    /* Solo se reinician identidades operativas; las del catálogo no se alteran. */
    DBCC CHECKIDENT ('dbo.DetalleAsiento', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Asiento', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.PeriodoContable', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Empresa', RESEED, 0) WITH NO_INFOMSGS;

    /*
       La transacción exterior permanece abierta. El seed oficial abre y confirma
       una transacción anidada; la confirmación definitiva se realiza después de
       incluirlo y validar el resultado completo.
    */
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Seed oficial, sin duplicar ni modificar sus datos. */
:r $(SeedCatalogoPath)

USE [ContaNexoDB];
GO

BEGIN TRY
    IF DB_NAME() <> N'ContaNexoDB'
        THROW 55005, 'El seed terminó fuera de ContaNexoDB.', 1;

    IF @@TRANCOUNT = 0
        THROW 55008, 'La transacción exterior del reset no está activa.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Empresa)
       OR EXISTS (SELECT 1 FROM dbo.PeriodoContable)
       OR EXISTS (SELECT 1 FROM dbo.Asiento)
       OR EXISTS (SELECT 1 FROM dbo.DetalleAsiento)
        THROW 55006, 'El reset dejó datos operativos inesperados.', 1;

    IF (SELECT COUNT(*) FROM dbo.ElementoContable) <> 5
       OR (SELECT COUNT(*) FROM dbo.GrupoContable) <> 11
       OR (SELECT COUNT(*) FROM dbo.CuentaContable) <> 90
       OR (SELECT COUNT(*) FROM dbo.DetalleCuenta) <> 69
        THROW 55007, 'El catálogo base no quedó completo.', 1;

    COMMIT TRANSACTION;
    PRINT N'Reset completado: datos operativos eliminados y catálogo base restaurado.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
