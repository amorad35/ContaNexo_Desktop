/*
    ContaNexo Desktop - Procedimientos almacenados
    Convencion: SP_Entidad_Accion

    Requiere ejecutar previamente:
      1. Database/ContaNexoDB.sql
      2. Database/SeedCatalogoCuentas.sql
*/

SET NOEXEC OFF;
GO

USE [master];
GO

IF DB_ID(N'ContaNexoDB') IS NULL
BEGIN
    RAISERROR(
        'ContaNexoDB no existe. Ejecute primero ContaNexoDB.sql y SeedCatalogoCuentas.sql.',
        16,
        1
    );
    SET NOEXEC ON;
END;
GO

USE [ContaNexoDB];
GO

IF DB_NAME() <> N'ContaNexoDB'
BEGIN
    DECLARE @baseActual SYSNAME = COALESCE(DB_NAME(), N'<desconocida>');

    RAISERROR(
        'Contexto inseguro: se esperaba ContaNexoDB y se obtuvo %s. No se crearan procedimientos.',
        16,
        1,
        @baseActual
    );
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'dbo.CuentaContable', N'U') IS NULL
   OR OBJECT_ID(N'dbo.GrupoContable', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ElementoContable', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DetalleCuenta', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Empresa', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PeriodoContable', N'U') IS NULL
BEGIN
    RAISERROR(
        'Faltan las tablas requeridas para los procedimientos almacenados.',
        16,
        1
    );
    SET NOEXEC ON;
END;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* PROCEDIMIENTOS DE EMPRESA */

CREATE OR ALTER PROCEDURE dbo.SP_Empresa_Listar
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            empresa.idEmpresa,
            empresa.nombreEmpresa,
            empresa.rucEmpresa,
            empresa.direccionEmpresa,
            empresa.telefonoEmpresa,
            empresa.correoEmpresa
        FROM dbo.Empresa AS empresa
        ORDER BY empresa.nombreEmpresa ASC,
                 empresa.idEmpresa ASC;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Empresa_Crear
    @nombreEmpresa NVARCHAR(150),
    @rucEmpresa VARCHAR(13) = NULL,
    @direccionEmpresa NVARCHAR(200) = NULL,
    @telefonoEmpresa VARCHAR(20) = NULL,
    @correoEmpresa NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @nombreNormalizado NVARCHAR(150) = LTRIM(RTRIM(@nombreEmpresa));
    DECLARE @rucNormalizado VARCHAR(13) = NULLIF(LTRIM(RTRIM(@rucEmpresa)), '');
    DECLARE @direccionNormalizada NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@direccionEmpresa)), N'');
    DECLARE @telefonoNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@telefonoEmpresa)), '');
    DECLARE @correoNormalizado NVARCHAR(150) = NULLIF(LTRIM(RTRIM(@correoEmpresa)), N'');
    DECLARE @idEmpresaNueva INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NULLIF(@nombreNormalizado, N'') IS NULL
            THROW 51001, 'El nombre de la empresa es obligatorio.', 1;

        INSERT INTO dbo.Empresa
            (nombreEmpresa, rucEmpresa, direccionEmpresa, telefonoEmpresa, correoEmpresa)
        VALUES
            (@nombreNormalizado, @rucNormalizado, @direccionNormalizada,
             @telefonoNormalizado, @correoNormalizado);

        SET @idEmpresaNueva = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT
            empresa.idEmpresa,
            empresa.nombreEmpresa,
            empresa.rucEmpresa,
            empresa.direccionEmpresa,
            empresa.telefonoEmpresa,
            empresa.correoEmpresa
        FROM dbo.Empresa AS empresa
        WHERE empresa.idEmpresa = @idEmpresaNueva;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_Empresa_Actualizar
    @idEmpresa INT,
    @nombreEmpresa NVARCHAR(150),
    @rucEmpresa VARCHAR(13) = NULL,
    @direccionEmpresa NVARCHAR(200) = NULL,
    @telefonoEmpresa VARCHAR(20) = NULL,
    @correoEmpresa NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @nombreNormalizado NVARCHAR(150) = LTRIM(RTRIM(@nombreEmpresa));
    DECLARE @rucNormalizado VARCHAR(13) = NULLIF(LTRIM(RTRIM(@rucEmpresa)), '');
    DECLARE @direccionNormalizada NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@direccionEmpresa)), N'');
    DECLARE @telefonoNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@telefonoEmpresa)), '');
    DECLARE @correoNormalizado NVARCHAR(150) = NULLIF(LTRIM(RTRIM(@correoEmpresa)), N'');

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
            WHERE idEmpresa = @idEmpresa
        )
            THROW 51101, 'La empresa que se desea actualizar no existe.', 1;

        IF NULLIF(@nombreNormalizado, N'') IS NULL
            THROW 51102, 'El nombre de la empresa es obligatorio.', 1;

        UPDATE dbo.Empresa
        SET nombreEmpresa = @nombreNormalizado,
            rucEmpresa = @rucNormalizado,
            direccionEmpresa = @direccionNormalizada,
            telefonoEmpresa = @telefonoNormalizado,
            correoEmpresa = @correoNormalizado
        WHERE idEmpresa = @idEmpresa;

        COMMIT TRANSACTION;

        SELECT
            empresa.idEmpresa,
            empresa.nombreEmpresa,
            empresa.rucEmpresa,
            empresa.direccionEmpresa,
            empresa.telefonoEmpresa,
            empresa.correoEmpresa
        FROM dbo.Empresa AS empresa
        WHERE empresa.idEmpresa = @idEmpresa;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* PROCEDIMIENTOS DE CUENTACONTABLE */

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_Listar
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            cuenta.idCuentaContable,
            cuenta.codigoCuenta,
            cuenta.nombreCuenta,
            cuenta.naturalezaCuenta,
            cuenta.estadoCuenta,
            cuenta.permiteMovimientoCuenta,
            cuenta.ordenCuenta,
            grupo.idGrupoContable,
            grupo.codigoGrupo,
            grupo.nombreGrupo,
            elemento.idElementoContable,
            elemento.codigoElemento,
            elemento.nombreElemento,
            cuenta.idCuentaPadre,
            padre.codigoCuenta AS codigoCuentaPadre,
            padre.nombreCuenta AS nombreCuentaPadre,
            CONVERT
            (
                BIT,
                CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.CuentaContable AS hija
                    WHERE hija.idCuentaPadre = cuenta.idCuentaContable
                ) THEN 1 ELSE 0 END
            ) AS tieneHijas
        FROM dbo.CuentaContable AS cuenta
        INNER JOIN dbo.GrupoContable AS grupo
            ON grupo.idGrupoContable = cuenta.idGrupoContable
        INNER JOIN dbo.ElementoContable AS elemento
            ON elemento.idElementoContable = grupo.idElementoContable
        LEFT JOIN dbo.CuentaContable AS padre
            ON padre.idCuentaContable = cuenta.idCuentaPadre
        ORDER BY cuenta.ordenCuenta, cuenta.codigoCuenta;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_ListarMovimiento
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            cuenta.idCuentaContable,
            cuenta.codigoCuenta,
            cuenta.nombreCuenta,
            cuenta.naturalezaCuenta,
            cuenta.ordenCuenta,
            grupo.idGrupoContable,
            grupo.codigoGrupo,
            grupo.nombreGrupo,
            elemento.idElementoContable,
            elemento.codigoElemento,
            elemento.nombreElemento
        FROM dbo.CuentaContable AS cuenta
        INNER JOIN dbo.GrupoContable AS grupo
            ON grupo.idGrupoContable = cuenta.idGrupoContable
        INNER JOIN dbo.ElementoContable AS elemento
            ON elemento.idElementoContable = grupo.idElementoContable
        WHERE cuenta.estadoCuenta = 1
          AND cuenta.permiteMovimientoCuenta = 1
        ORDER BY cuenta.ordenCuenta, cuenta.codigoCuenta;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_ObtenerPorId
    @idCuentaContable INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @idCuentaContable IS NULL
           OR NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.CuentaContable
                  WHERE idCuentaContable = @idCuentaContable
              )
        BEGIN
            THROW 52001, 'La cuenta contable solicitada no existe.', 1;
        END;

        SELECT
            cuenta.idCuentaContable,
            cuenta.codigoCuenta,
            cuenta.nombreCuenta,
            cuenta.naturalezaCuenta,
            cuenta.estadoCuenta,
            cuenta.permiteMovimientoCuenta,
            cuenta.ordenCuenta,
            grupo.idGrupoContable,
            grupo.codigoGrupo,
            grupo.nombreGrupo,
            elemento.idElementoContable,
            elemento.codigoElemento,
            elemento.nombreElemento,
            cuenta.idCuentaPadre,
            padre.codigoCuenta AS codigoCuentaPadre,
            padre.nombreCuenta AS nombreCuentaPadre,
            CONVERT
            (
                BIT,
                CASE WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.CuentaContable AS hija
                    WHERE hija.idCuentaPadre = cuenta.idCuentaContable
                ) THEN 1 ELSE 0 END
            ) AS tieneHijas,
            detalle.idDetalleCuenta,
            detalle.descripcionDetalle,
            detalle.dinamicaDebitoDetalle,
            detalle.dinamicaCreditoDetalle
        FROM dbo.CuentaContable AS cuenta
        INNER JOIN dbo.GrupoContable AS grupo
            ON grupo.idGrupoContable = cuenta.idGrupoContable
        INNER JOIN dbo.ElementoContable AS elemento
            ON elemento.idElementoContable = grupo.idElementoContable
        LEFT JOIN dbo.CuentaContable AS padre
            ON padre.idCuentaContable = cuenta.idCuentaPadre
        LEFT JOIN dbo.DetalleCuenta AS detalle
            ON detalle.idCuentaContable = cuenta.idCuentaContable
        WHERE cuenta.idCuentaContable = @idCuentaContable;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_Crear
    @idGrupoContable INT,
    @idCuentaPadre INT = NULL,
    @codigoCuenta VARCHAR(20),
    @nombreCuenta NVARCHAR(150),
    @naturalezaCuenta VARCHAR(10),
    @permiteMovimientoCuenta TINYINT = 1,
    @estadoCuenta TINYINT = 1,
    @ordenCuenta INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @codigoNormalizado VARCHAR(20) = LTRIM(RTRIM(@codigoCuenta));
    DECLARE @nombreNormalizado NVARCHAR(150) = LTRIM(RTRIM(@nombreCuenta));
    DECLARE @cantidadHijas BIGINT = 0;
    DECLARE @idCuentaNueva INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NULLIF(@codigoNormalizado, '') IS NULL
            THROW 52101, 'El código de la cuenta es obligatorio.', 1;

        IF NULLIF(@nombreNormalizado, N'') IS NULL
            THROW 52102, 'El nombre de la cuenta es obligatorio.', 1;

        IF @ordenCuenta IS NULL OR @ordenCuenta <= 0
            THROW 52103, 'El orden de la cuenta debe ser un entero mayor que cero.', 1;

        IF @naturalezaCuenta IS NULL
           OR @naturalezaCuenta NOT IN ('Deudora', 'Acreedora')
            THROW 52104, 'La naturaleza debe ser Deudora o Acreedora.', 1;

        IF @permiteMovimientoCuenta IS NULL
           OR @permiteMovimientoCuenta NOT IN (0, 1)
            THROW 52105, 'permiteMovimientoCuenta solo acepta 0 o 1.', 1;

        IF @estadoCuenta IS NULL OR @estadoCuenta NOT IN (0, 1)
            THROW 52106, 'estadoCuenta solo acepta 0 o 1.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.GrupoContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idGrupoContable = @idGrupoContable
              AND estadoGrupo = 1
        )
            THROW 52107, 'El grupo contable no existe o está inactivo.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
            WHERE codigoCuenta = @codigoNormalizado
        )
            THROW 52108, 'Ya existe una cuenta con el código indicado.', 1;

        IF @idCuentaPadre IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
                WHERE idCuentaContable = @idCuentaPadre
            )
                THROW 52109, 'La cuenta padre indicada no existe.', 1;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.CuentaContable
                WHERE idCuentaContable = @idCuentaPadre
                  AND idGrupoContable = @idGrupoContable
            )
                THROW 52110, 'La cuenta hija debe pertenecer al mismo grupo que su padre.', 1;

            SELECT @cantidadHijas = COUNT_BIG(*)
            FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idCuentaPadre = @idCuentaPadre;
        END;

        INSERT INTO dbo.CuentaContable
            (idGrupoContable, idCuentaPadre, codigoCuenta, nombreCuenta,
             naturalezaCuenta, permiteMovimientoCuenta, estadoCuenta, ordenCuenta)
        VALUES
            (@idGrupoContable, @idCuentaPadre, @codigoNormalizado,
             @nombreNormalizado, @naturalezaCuenta,
             CONVERT(BIT, @permiteMovimientoCuenta),
             CONVERT(BIT, @estadoCuenta), @ordenCuenta);

        SET @idCuentaNueva = CONVERT(INT, SCOPE_IDENTITY());

        IF @idCuentaPadre IS NOT NULL AND @cantidadHijas = 0
        BEGIN
            UPDATE dbo.CuentaContable
            SET permiteMovimientoCuenta = 0
            WHERE idCuentaContable = @idCuentaPadre;
        END;

        COMMIT TRANSACTION;

        SELECT @idCuentaNueva AS idCuentaContable;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_Actualizar
    @idCuentaContable INT,
    @idGrupoContable INT,
    @idCuentaPadre INT = NULL,
    @codigoCuenta VARCHAR(20),
    @nombreCuenta NVARCHAR(150),
    @naturalezaCuenta VARCHAR(10),
    @ordenCuenta INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @codigoNormalizado VARCHAR(20) = LTRIM(RTRIM(@codigoCuenta));
    DECLARE @nombreNormalizado NVARCHAR(150) = LTRIM(RTRIM(@nombreCuenta));
    DECLARE @idCuentaPadreAnterior INT;
    DECLARE @cantidadHijasNuevaPadre BIGINT = 0;
    DECLARE @cambiaPadre BIT = 0;
    DECLARE @generaCiclo BIT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @idCuentaPadreAnterior = idCuentaPadre
        FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
        WHERE idCuentaContable = @idCuentaContable;

        IF @@ROWCOUNT = 0
            THROW 52201, 'La cuenta contable que se desea actualizar no existe.', 1;

        IF NULLIF(@codigoNormalizado, '') IS NULL
            THROW 52202, 'El código de la cuenta es obligatorio.', 1;

        IF NULLIF(@nombreNormalizado, N'') IS NULL
            THROW 52203, 'El nombre de la cuenta es obligatorio.', 1;

        IF @ordenCuenta IS NULL OR @ordenCuenta <= 0
            THROW 52204, 'El orden de la cuenta debe ser un entero mayor que cero.', 1;

        IF @naturalezaCuenta IS NULL
           OR @naturalezaCuenta NOT IN ('Deudora', 'Acreedora')
            THROW 52205, 'La naturaleza debe ser Deudora o Acreedora.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.GrupoContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idGrupoContable = @idGrupoContable
              AND estadoGrupo = 1
        )
            THROW 52206, 'El grupo contable no existe o está inactivo.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
            WHERE codigoCuenta = @codigoNormalizado
              AND idCuentaContable <> @idCuentaContable
        )
            THROW 52207, 'Ya existe otra cuenta con el código indicado.', 1;

        IF @idCuentaPadre = @idCuentaContable
            THROW 52208, 'Una cuenta no puede ser su propia cuenta padre.', 1;

        IF @idCuentaPadre IS NOT NULL
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
                WHERE idCuentaContable = @idCuentaPadre
            )
                THROW 52209, 'La cuenta padre indicada no existe.', 1;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.CuentaContable
                WHERE idCuentaContable = @idCuentaPadre
                  AND idGrupoContable = @idGrupoContable
            )
                THROW 52210, 'La cuenta debe pertenecer al mismo grupo que su padre.', 1;

            ;WITH Ancestros AS
            (
                SELECT cuenta.idCuentaContable, cuenta.idCuentaPadre
                FROM dbo.CuentaContable AS cuenta
                WHERE cuenta.idCuentaContable = @idCuentaPadre

                UNION ALL

                SELECT padre.idCuentaContable, padre.idCuentaPadre
                FROM dbo.CuentaContable AS padre
                INNER JOIN Ancestros AS anterior
                    ON padre.idCuentaContable = anterior.idCuentaPadre
            )
            SELECT TOP (1) @generaCiclo = 1
            FROM Ancestros
            WHERE idCuentaContable = @idCuentaContable
            OPTION (MAXRECURSION 32767);

            IF @generaCiclo = 1
                THROW 52211, 'La cuenta padre indicada produciría un ciclo jerárquico.', 1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.CuentaContable AS hija
            WHERE hija.idCuentaPadre = @idCuentaContable
              AND hija.idGrupoContable <> @idGrupoContable
        )
            THROW 52212, 'No se puede cambiar el grupo porque una o más cuentas hijas quedarían en otro grupo.', 1;

        SET @cambiaPadre =
            CASE
                WHEN @idCuentaPadreAnterior = @idCuentaPadre THEN 0
                WHEN @idCuentaPadreAnterior IS NULL AND @idCuentaPadre IS NULL THEN 0
                ELSE 1
            END;

        IF @cambiaPadre = 1 AND @idCuentaPadre IS NOT NULL
        BEGIN
            SELECT @cantidadHijasNuevaPadre = COUNT_BIG(*)
            FROM dbo.CuentaContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idCuentaPadre = @idCuentaPadre;
        END;

        UPDATE dbo.CuentaContable
        SET idGrupoContable = @idGrupoContable,
            idCuentaPadre = @idCuentaPadre,
            codigoCuenta = @codigoNormalizado,
            nombreCuenta = @nombreNormalizado,
            naturalezaCuenta = @naturalezaCuenta,
            ordenCuenta = @ordenCuenta
        WHERE idCuentaContable = @idCuentaContable;

        IF @cambiaPadre = 1
           AND @idCuentaPadre IS NOT NULL
           AND @cantidadHijasNuevaPadre = 0
        BEGIN
            UPDATE dbo.CuentaContable
            SET permiteMovimientoCuenta = 0
            WHERE idCuentaContable = @idCuentaPadre;
        END;

        COMMIT TRANSACTION;

        SELECT @idCuentaContable AS idCuentaContable;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_ConfigurarMovimiento
    @idCuentaContable INT,
    @permiteMovimientoCuenta TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @permiteMovimientoCuenta IS NULL
           OR @permiteMovimientoCuenta NOT IN (0, 1)
            THROW 52301, 'permiteMovimientoCuenta solo acepta 0 o 1.', 1;

        UPDATE dbo.CuentaContable
        SET permiteMovimientoCuenta = CONVERT(BIT, @permiteMovimientoCuenta)
        WHERE idCuentaContable = @idCuentaContable;

        IF @@ROWCOUNT = 0
            THROW 52302, 'La cuenta contable indicada no existe.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CuentaContable_CambiarEstado
    @idCuentaContable INT,
    @estadoCuenta TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @estadoCuenta IS NULL OR @estadoCuenta NOT IN (0, 1)
            THROW 52401, 'estadoCuenta solo acepta 0 o 1.', 1;

        UPDATE dbo.CuentaContable
        SET estadoCuenta = CONVERT(BIT, @estadoCuenta)
        WHERE idCuentaContable = @idCuentaContable;

        IF @@ROWCOUNT = 0
            THROW 52402, 'La cuenta contable indicada no existe.', 1;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

/* PROCEDIMIENTOS DE PERIODOCONTABLE */

CREATE OR ALTER PROCEDURE dbo.SP_PeriodoContable_Listar
    @idEmpresa INT = NULL,
    @estadoPeriodo VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @estadoNormalizado VARCHAR(10) =
        CASE
            WHEN @estadoPeriodo IS NULL THEN NULL
            ELSE LTRIM(RTRIM(@estadoPeriodo))
        END;

    BEGIN TRY
        IF @estadoNormalizado IS NOT NULL
           AND @estadoNormalizado NOT IN ('Abierto', 'Cerrado')
            THROW 53001, 'El estado del período debe ser Abierto, Cerrado o NULL.', 1;

        SELECT
            periodo.idPeriodoContable,
            empresa.idEmpresa,
            empresa.nombreEmpresa,
            periodo.nombrePeriodo,
            periodo.fechaInicioPeriodo,
            periodo.fechaFinPeriodo,
            periodo.estadoPeriodo,
            periodo.fechaCierrePeriodo
        FROM dbo.PeriodoContable AS periodo
        INNER JOIN dbo.Empresa AS empresa
            ON empresa.idEmpresa = periodo.idEmpresa
        WHERE (@idEmpresa IS NULL OR periodo.idEmpresa = @idEmpresa)
          AND (@estadoNormalizado IS NULL
               OR periodo.estadoPeriodo = @estadoNormalizado)
        ORDER BY periodo.fechaInicioPeriodo DESC,
                 periodo.idPeriodoContable DESC;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_PeriodoContable_ObtenerPorId
    @idPeriodoContable INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @idPeriodoContable IS NULL
           OR NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.PeriodoContable
                  WHERE idPeriodoContable = @idPeriodoContable
              )
            THROW 53101, 'El período contable solicitado no existe.', 1;

        SELECT
            periodo.idPeriodoContable,
            empresa.idEmpresa,
            empresa.nombreEmpresa,
            empresa.rucEmpresa,
            periodo.nombrePeriodo,
            periodo.fechaInicioPeriodo,
            periodo.fechaFinPeriodo,
            periodo.estadoPeriodo,
            periodo.fechaCierrePeriodo
        FROM dbo.PeriodoContable AS periodo
        INNER JOIN dbo.Empresa AS empresa
            ON empresa.idEmpresa = periodo.idEmpresa
        WHERE periodo.idPeriodoContable = @idPeriodoContable;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_PeriodoContable_Crear
    @idEmpresa INT,
    @nombrePeriodo NVARCHAR(100),
    @fechaInicioPeriodo DATE,
    @fechaFinPeriodo DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @nombreNormalizado NVARCHAR(100) = LTRIM(RTRIM(@nombrePeriodo));
    DECLARE @idPeriodoNuevo INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
            WHERE idEmpresa = @idEmpresa
        )
            THROW 53201, 'La empresa indicada no existe.', 1;

        IF NULLIF(@nombreNormalizado, N'') IS NULL
            THROW 53202, 'El nombre del período es obligatorio.', 1;

        IF @fechaInicioPeriodo IS NULL
            THROW 53203, 'La fecha inicial del período es obligatoria.', 1;

        IF @fechaFinPeriodo IS NULL
            THROW 53204, 'La fecha final del período es obligatoria.', 1;

        IF @fechaInicioPeriodo > @fechaFinPeriodo
            THROW 53205, 'La fecha inicial no puede ser posterior a la fecha final.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idEmpresa = @idEmpresa
              AND @fechaInicioPeriodo <= fechaFinPeriodo
              AND @fechaFinPeriodo >= fechaInicioPeriodo
        )
            THROW 53206, 'El período se solapa con otro período de la misma empresa.', 1;

        INSERT INTO dbo.PeriodoContable
            (idEmpresa, nombrePeriodo, fechaInicioPeriodo, fechaFinPeriodo,
             estadoPeriodo, fechaCierrePeriodo)
        VALUES
            (@idEmpresa, @nombreNormalizado, @fechaInicioPeriodo,
             @fechaFinPeriodo, 'Abierto', NULL);

        SET @idPeriodoNuevo = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT
            periodo.idPeriodoContable,
            periodo.idEmpresa,
            periodo.nombrePeriodo,
            periodo.fechaInicioPeriodo,
            periodo.fechaFinPeriodo,
            periodo.estadoPeriodo,
            periodo.fechaCierrePeriodo
        FROM dbo.PeriodoContable AS periodo
        WHERE periodo.idPeriodoContable = @idPeriodoNuevo;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_PeriodoContable_Actualizar
    @idPeriodoContable INT,
    @nombrePeriodo NVARCHAR(100),
    @fechaInicioPeriodo DATE,
    @fechaFinPeriodo DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @nombreNormalizado NVARCHAR(100) = LTRIM(RTRIM(@nombrePeriodo));
    DECLARE @idEmpresaBloqueo INT;
    DECLARE @idEmpresa INT;
    DECLARE @estadoActual VARCHAR(10);

    BEGIN TRY
        SELECT @idEmpresaBloqueo = idEmpresa
        FROM dbo.PeriodoContable
        WHERE idPeriodoContable = @idPeriodoContable;

        IF @idEmpresaBloqueo IS NULL
            THROW 53301, 'El período contable que se desea actualizar no existe.', 1;

        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Empresa WITH (UPDLOCK, HOLDLOCK)
            WHERE idEmpresa = @idEmpresaBloqueo
        )
            THROW 53302, 'La empresa relacionada con el período no existe.', 1;

        SELECT
            @idEmpresa = idEmpresa,
            @estadoActual = estadoPeriodo
        FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
        WHERE idPeriodoContable = @idPeriodoContable
          AND idEmpresa = @idEmpresaBloqueo;

        IF @idEmpresa IS NULL
            THROW 53301, 'El período contable que se desea actualizar no existe.', 1;

        IF @estadoActual <> 'Abierto'
            THROW 53303, 'Solo se puede modificar un período que esté Abierto.', 1;

        IF NULLIF(@nombreNormalizado, N'') IS NULL
            THROW 53304, 'El nombre del período es obligatorio.', 1;

        IF @fechaInicioPeriodo IS NULL
            THROW 53305, 'La fecha inicial del período es obligatoria.', 1;

        IF @fechaFinPeriodo IS NULL
            THROW 53306, 'La fecha final del período es obligatoria.', 1;

        IF @fechaInicioPeriodo > @fechaFinPeriodo
            THROW 53307, 'La fecha inicial no puede ser posterior a la fecha final.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
            WHERE idEmpresa = @idEmpresa
              AND idPeriodoContable <> @idPeriodoContable
              AND @fechaInicioPeriodo <= fechaFinPeriodo
              AND @fechaFinPeriodo >= fechaInicioPeriodo
        )
            THROW 53308, 'El período actualizado se solaparía con otro período de la misma empresa.', 1;

        UPDATE dbo.PeriodoContable
        SET nombrePeriodo = @nombreNormalizado,
            fechaInicioPeriodo = @fechaInicioPeriodo,
            fechaFinPeriodo = @fechaFinPeriodo
        WHERE idPeriodoContable = @idPeriodoContable;

        COMMIT TRANSACTION;

        SELECT
            periodo.idPeriodoContable,
            periodo.idEmpresa,
            periodo.nombrePeriodo,
            periodo.fechaInicioPeriodo,
            periodo.fechaFinPeriodo,
            periodo.estadoPeriodo,
            periodo.fechaCierrePeriodo
        FROM dbo.PeriodoContable AS periodo
        WHERE periodo.idPeriodoContable = @idPeriodoContable;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_PeriodoContable_Cerrar
    @idPeriodoContable INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @estadoActual VARCHAR(10);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @estadoActual = estadoPeriodo
        FROM dbo.PeriodoContable WITH (UPDLOCK, HOLDLOCK)
        WHERE idPeriodoContable = @idPeriodoContable;

        IF @estadoActual IS NULL
            THROW 53401, 'El período contable que se desea cerrar no existe.', 1;

        IF @estadoActual <> 'Abierto'
            THROW 53402, 'El período contable ya está Cerrado.', 1;

        UPDATE dbo.PeriodoContable
        SET estadoPeriodo = 'Cerrado',
            fechaCierrePeriodo = SYSDATETIME()
        WHERE idPeriodoContable = @idPeriodoContable;

        COMMIT TRANSACTION;

        SELECT
            periodo.idPeriodoContable,
            periodo.estadoPeriodo,
            periodo.fechaCierrePeriodo
        FROM dbo.PeriodoContable AS periodo
        WHERE periodo.idPeriodoContable = @idPeriodoContable;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO
