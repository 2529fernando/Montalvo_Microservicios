-- Script SQL Server 2022 / SQL Express: sin DROP y sin borrar datos.
-- Ejecutar con sqlcmd -b para detenerse ante el primer error (en SSMS: modo SQLCMD y :ON ERROR EXIT).
-- Completa tablas existentes compatibles; no inventa IDs ni modifica claves o tipos.
-- IdProducto fuera de CatalogoDB es referencia lógica, NO FK entre BD.

-- ================================================================
-- MICROSERVICIO CATALOGO / CatalogoDB
-- ================================================================
IF DB_ID(N'CatalogoDB') IS NULL
    EXEC (N'CREATE DATABASE [CatalogoDB]');
GO
USE [CatalogoDB];
GO

-- Validar el esquema existente antes de completar esta base.
-- Si falla, corregir o migrar los datos indicados y volver a ejecutar con sqlcmd -b.
DECLARE @Claves TABLE (Tabla sysname, Clave sysname);
INSERT INTO @Claves VALUES (N'Marca', N'IdMarca'), (N'Categoria', N'IdCategoria'), (N'Productos', N'IdProducto');
DECLARE @Referencias TABLE (Tabla sysname, Columna sysname, Padre sysname NULL, ClavePadre sysname NULL);
INSERT INTO @Referencias VALUES
    (N'Productos', N'IdMarca', N'Marca', N'IdMarca'),
    (N'Productos', N'IdCategoria', N'Categoria', N'IdCategoria');
DECLARE @Tabla sysname, @Columna sysname, @Padre sysname, @ClavePadre sysname;
DECLARE @IdTabla int, @Problema bit, @Sql nvarchar(max), @Mensaje nvarchar(2048);

WHILE EXISTS (SELECT 1 FROM @Claves)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Clave FROM @Claves;
    DELETE FROM @Claves WHERE Tabla = @Tabla;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U');
    IF @IdTabla IS NOT NULL AND (
        NOT EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = @IdTabla AND name = @Columna
                AND system_type_id = 56 AND is_identity = 1 AND is_nullable = 0)
        OR NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = @IdTabla AND i.is_primary_key = 1
                AND ic.key_ordinal = 1 AND c.name = @Columna
                AND NOT EXISTS (SELECT 1 FROM sys.index_columns otro
                    WHERE otro.object_id = i.object_id AND otro.index_id = i.index_id AND otro.key_ordinal > 1)))
    BEGIN
        SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N': se requiere ', @Columna,
            N' INT IDENTITY y clave primaria de una sola columna. Migra esta tabla antes de continuar; no se modificaron sus claves.');
        THROW 50001, @Mensaje, 1;
    END;
END;

WHILE EXISTS (SELECT 1 FROM @Referencias)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Columna, @Padre = Padre, @ClavePadre = ClavePadre FROM @Referencias;
    DELETE FROM @Referencias WHERE Tabla = @Tabla AND Columna = @Columna;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U');
    IF @IdTabla IS NULL CONTINUE;

    SET @Problema = 0;
    IF COL_LENGTH(N'dbo.' + QUOTENAME(@Tabla), @Columna) IS NULL
    BEGIN
        SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') SET @Encontrado = 1;';
        EXEC sys.sp_executesql @Sql, N'@Encontrado bit OUTPUT', @Encontrado = @Problema OUTPUT;
        IF @Problema = 1
        BEGIN
            SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N': falta ', @Columna,
                N' y la tabla contiene filas. Asigna las referencias reales mediante una migracion antes de continuar.');
            THROW 50002, @Mensaje, 1;
        END;
    END
    ELSE IF @Padre IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'dbo.' + QUOTENAME(@Padre), N'U') IS NULL
            SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') SET @Encontrado = 1;';
        ELSE
            SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N' hijo WHERE hijo.' + QUOTENAME(@Columna)
                + N' IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Padre) + N' padre WHERE padre.'
                + QUOTENAME(@ClavePadre) + N' = hijo.' + QUOTENAME(@Columna) + N')) SET @Encontrado = 1;';
        EXEC sys.sp_executesql @Sql, N'@Encontrado bit OUTPUT', @Encontrado = @Problema OUTPUT;
        IF @Problema = 1
        BEGIN
            SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N'.', @Columna, N': hay referencias nulas o sin registro en dbo.',
                @Padre, N'.', @ClavePadre, N'. Corrige los registros antes de crear o habilitar la clave foranea.');
            THROW 50003, @Mensaje, 1;
        END;
    END;
END;
GO

IF OBJECT_ID(N'dbo.[Marca]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Marca] (
        [IdMarca] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Marca PRIMARY KEY,
        [Nombre] NVARCHAR(100) NOT NULL CONSTRAINT DF_Marca_Nombre DEFAULT (N'')
    );
END;
GO

IF COL_LENGTH(N'dbo.Marca', N'Nombre') IS NULL
    ALTER TABLE dbo.[Marca] ADD [Nombre] NVARCHAR(100) NOT NULL CONSTRAINT DF_Marca_Nombre DEFAULT (N'');
GO

IF OBJECT_ID(N'dbo.[Categoria]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Categoria] (
        [IdCategoria] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categoria PRIMARY KEY,
        [Nombre] NVARCHAR(100) NOT NULL CONSTRAINT DF_Categoria_Nombre DEFAULT (N'')
    );
END;
GO

IF COL_LENGTH(N'dbo.Categoria', N'Nombre') IS NULL
    ALTER TABLE dbo.[Categoria] ADD [Nombre] NVARCHAR(100) NOT NULL CONSTRAINT DF_Categoria_Nombre DEFAULT (N'');
GO

IF OBJECT_ID(N'dbo.[Productos]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Productos] (
        [IdProducto] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Productos PRIMARY KEY,
        [IdMarca] INT NOT NULL,
        [IdCategoria] INT NOT NULL,
        [Nombre] NVARCHAR(150) NOT NULL CONSTRAINT DF_Productos_Nombre DEFAULT (N''),
        [Descripcion] NVARCHAR(500) NULL,
        [Precio] DECIMAL(10,2) NOT NULL CONSTRAINT DF_Productos_Precio DEFAULT (0),
        [Estado] BIT NOT NULL CONSTRAINT DF_Productos_Estado DEFAULT (1)
    );
END;
GO

IF COL_LENGTH(N'dbo.Productos', N'IdMarca') IS NULL
    ALTER TABLE dbo.[Productos] ADD [IdMarca] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Productos', N'IdCategoria') IS NULL
    ALTER TABLE dbo.[Productos] ADD [IdCategoria] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Productos', N'Nombre') IS NULL
    ALTER TABLE dbo.[Productos] ADD [Nombre] NVARCHAR(150) NOT NULL CONSTRAINT DF_Productos_Nombre DEFAULT (N'');
GO
IF COL_LENGTH(N'dbo.Productos', N'Descripcion') IS NULL
    ALTER TABLE dbo.[Productos] ADD [Descripcion] NVARCHAR(500) NULL;
GO
IF COL_LENGTH(N'dbo.Productos', N'Precio') IS NULL
    ALTER TABLE dbo.[Productos] ADD [Precio] DECIMAL(10,2) NOT NULL CONSTRAINT DF_Productos_Precio DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.Productos', N'Estado') IS NULL
    ALTER TABLE dbo.[Productos] ADD [Estado] BIT NOT NULL CONSTRAINT DF_Productos_Estado DEFAULT (1);
GO

-- Relaciones internas e indices de consulta; IdProducto externo sigue siendo referencia logica.
DECLARE @Relaciones TABLE (Tabla sysname, Columna sysname, Padre sysname NULL, ClavePadre sysname NULL, Restriccion sysname NULL);
INSERT INTO @Relaciones VALUES
    (N'Productos', N'IdMarca', N'Marca', N'IdMarca', N'FK_Productos_Marca'),
    (N'Productos', N'IdCategoria', N'Categoria', N'IdCategoria', N'FK_Productos_Categoria');
DECLARE @Tabla sysname, @Columna sysname, @Padre sysname, @ClavePadre sysname, @Restriccion sysname;
DECLARE @IdTabla int, @Sql nvarchar(max), @Mensaje nvarchar(2048), @Indice sysname, @Sufijo int;

WHILE EXISTS (SELECT 1 FROM @Relaciones)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Columna, @Padre = Padre, @ClavePadre = ClavePadre, @Restriccion = Restriccion FROM @Relaciones;
    DELETE FROM @Relaciones WHERE Tabla = @Tabla AND Columna = @Columna;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla));

    IF @Padre IS NOT NULL
    BEGIN
        -- Aceptar una FK equivalente con otro nombre, sin crear relaciones duplicadas.
        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
            WHERE fk.parent_object_id = @IdTabla AND fk.referenced_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Padre))
                AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @Columna
                AND COL_NAME(fc.referenced_object_id, fc.referenced_column_id) = @ClavePadre
                AND (SELECT COUNT(*) FROM sys.foreign_key_columns todas WHERE todas.constraint_object_id = fk.object_id) = 1)
        BEGIN
            IF OBJECT_ID(N'dbo.' + QUOTENAME(@Restriccion)) IS NOT NULL
            BEGIN
                SET @Mensaje = CONCAT(DB_NAME(), N': la restriccion ', @Restriccion,
                    N' ya existe con una definicion diferente. Revisa el esquema antes de continuar.');
                THROW 50004, @Mensaje, 1;
            END;
            SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' WITH CHECK ADD CONSTRAINT '
                + QUOTENAME(@Restriccion) + N' FOREIGN KEY (' + QUOTENAME(@Columna) + N') REFERENCES dbo.'
                + QUOTENAME(@Padre) + N' (' + QUOTENAME(@ClavePadre) + N');';
            EXEC sys.sp_executesql @Sql;
        END;

        -- Reactivar y comprobar tambien las FK existentes, incluso si tenian otro nombre.
        SELECT @Sql = STRING_AGG(CAST(N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' WITH CHECK CHECK CONSTRAINT '
                + QUOTENAME(fk.name) + N';' AS nvarchar(max)), NCHAR(10))
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
        WHERE fk.parent_object_id = @IdTabla AND fk.referenced_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Padre))
            AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @Columna
            AND COL_NAME(fc.referenced_object_id, fc.referenced_column_id) = @ClavePadre
            AND (SELECT COUNT(*) FROM sys.foreign_key_columns todas WHERE todas.constraint_object_id = fk.object_id) = 1;
        IF @Sql IS NOT NULL EXEC sys.sp_executesql @Sql;
    END;

    -- Un indice cuyo primer campo sea la referencia tambien satisface esta consulta.
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = @IdTabla AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.has_filter = 0
            AND ic.key_ordinal = 1 AND COL_NAME(ic.object_id, ic.column_id) = @Columna)
    BEGIN
        SET @Indice = N'IX_' + @Tabla + N'_' + @Columna;
        SET @Sufijo = 0;
        -- Conservar cualquier indice previo con ese nombre y una definicion diferente.
        WHILE EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @IdTabla AND name = @Indice)
        BEGIN
            SET @Sufijo += 1;
            SET @Indice = N'IX_' + @Tabla + N'_' + @Columna + N'_FK' + CONVERT(nvarchar(10), @Sufijo);
        END;
        SET @Sql = N'CREATE INDEX ' + QUOTENAME(@Indice) + N' ON dbo.' + QUOTENAME(@Tabla) + N' (' + QUOTENAME(@Columna) + N');';
        EXEC sys.sp_executesql @Sql;
    END;
END;
GO

-- Comprobar tablas creadas
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME;
GO

-- ================================================================
-- MICROSERVICIO INVENTARIO / InventarioDB
-- ================================================================
IF DB_ID(N'InventarioDB') IS NULL
    EXEC (N'CREATE DATABASE [InventarioDB]');
GO
USE [InventarioDB];
GO

-- Validar el esquema existente antes de completar esta base.
-- Si falla, corregir o migrar los datos indicados y volver a ejecutar con sqlcmd -b.
DECLARE @Claves TABLE (Tabla sysname, Clave sysname);
INSERT INTO @Claves VALUES (N'Bodega', N'IdBodega'), (N'Existencias', N'IdExistencias'), (N'MovimientosInv', N'IdMovimiento'), (N'Stock', N'IdStock');
DECLARE @Referencias TABLE (Tabla sysname, Columna sysname, Padre sysname NULL, ClavePadre sysname NULL);
INSERT INTO @Referencias VALUES
    (N'Existencias', N'IdProducto', NULL, NULL),
    (N'Existencias', N'IdBodega', N'Bodega', N'IdBodega'),
    (N'MovimientosInv', N'IdBodega', N'Bodega', N'IdBodega'),
    (N'MovimientosInv', N'IdProducto', NULL, NULL),
    (N'Stock', N'IdBodega', N'Bodega', N'IdBodega'),
    (N'Stock', N'IdProducto', NULL, NULL);
DECLARE @Tabla sysname, @Columna sysname, @Padre sysname, @ClavePadre sysname;
DECLARE @IdTabla int, @Problema bit, @Sql nvarchar(max), @Mensaje nvarchar(2048);

WHILE EXISTS (SELECT 1 FROM @Claves)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Clave FROM @Claves;
    DELETE FROM @Claves WHERE Tabla = @Tabla;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U');
    IF @IdTabla IS NOT NULL AND (
        NOT EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = @IdTabla AND name = @Columna
                AND system_type_id = 56 AND is_identity = 1 AND is_nullable = 0)
        OR NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = @IdTabla AND i.is_primary_key = 1
                AND ic.key_ordinal = 1 AND c.name = @Columna
                AND NOT EXISTS (SELECT 1 FROM sys.index_columns otro
                    WHERE otro.object_id = i.object_id AND otro.index_id = i.index_id AND otro.key_ordinal > 1)))
    BEGIN
        SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N': se requiere ', @Columna,
            N' INT IDENTITY y clave primaria de una sola columna. Migra esta tabla antes de continuar; no se modificaron sus claves.');
        THROW 50001, @Mensaje, 1;
    END;
END;

WHILE EXISTS (SELECT 1 FROM @Referencias)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Columna, @Padre = Padre, @ClavePadre = ClavePadre FROM @Referencias;
    DELETE FROM @Referencias WHERE Tabla = @Tabla AND Columna = @Columna;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U');
    IF @IdTabla IS NULL CONTINUE;

    SET @Problema = 0;
    IF COL_LENGTH(N'dbo.' + QUOTENAME(@Tabla), @Columna) IS NULL
    BEGIN
        SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') SET @Encontrado = 1;';
        EXEC sys.sp_executesql @Sql, N'@Encontrado bit OUTPUT', @Encontrado = @Problema OUTPUT;
        IF @Problema = 1
        BEGIN
            SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N': falta ', @Columna,
                N' y la tabla contiene filas. Asigna las referencias reales mediante una migracion antes de continuar.');
            THROW 50002, @Mensaje, 1;
        END;
    END
    ELSE IF @Padre IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'dbo.' + QUOTENAME(@Padre), N'U') IS NULL
            SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') SET @Encontrado = 1;';
        ELSE
            SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N' hijo WHERE hijo.' + QUOTENAME(@Columna)
                + N' IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Padre) + N' padre WHERE padre.'
                + QUOTENAME(@ClavePadre) + N' = hijo.' + QUOTENAME(@Columna) + N')) SET @Encontrado = 1;';
        EXEC sys.sp_executesql @Sql, N'@Encontrado bit OUTPUT', @Encontrado = @Problema OUTPUT;
        IF @Problema = 1
        BEGIN
            SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N'.', @Columna, N': hay referencias nulas o sin registro en dbo.',
                @Padre, N'.', @ClavePadre, N'. Corrige los registros antes de crear o habilitar la clave foranea.');
            THROW 50003, @Mensaje, 1;
        END;
    END;
END;
GO

IF OBJECT_ID(N'dbo.[Bodega]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Bodega] (
        [IdBodega] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bodega PRIMARY KEY,
        [Nombre] NVARCHAR(100) NOT NULL CONSTRAINT DF_Bodega_Nombre DEFAULT (N''),
        [Ubicacion] NVARCHAR(200) NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.Bodega', N'Nombre') IS NULL
    ALTER TABLE dbo.[Bodega] ADD [Nombre] NVARCHAR(100) NOT NULL CONSTRAINT DF_Bodega_Nombre DEFAULT (N'');
GO
IF COL_LENGTH(N'dbo.Bodega', N'Ubicacion') IS NULL
    ALTER TABLE dbo.[Bodega] ADD [Ubicacion] NVARCHAR(200) NULL;
GO

IF OBJECT_ID(N'dbo.[Existencias]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Existencias] (
        [IdExistencias] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Existencias PRIMARY KEY,
        [IdProducto] INT NOT NULL,
        [IdBodega] INT NOT NULL,
        [Cantidad] DECIMAL(18,2) NOT NULL CONSTRAINT DF_Existencias_Cantidad DEFAULT (0),
        [FechaActualizacion] DATETIME2(0) NOT NULL CONSTRAINT DF_Existencias_FechaActualizacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF COL_LENGTH(N'dbo.Existencias', N'IdProducto') IS NULL
    ALTER TABLE dbo.[Existencias] ADD [IdProducto] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Existencias', N'IdBodega') IS NULL
    ALTER TABLE dbo.[Existencias] ADD [IdBodega] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Existencias', N'Cantidad') IS NULL
    ALTER TABLE dbo.[Existencias] ADD [Cantidad] DECIMAL(18,2) NOT NULL CONSTRAINT DF_Existencias_Cantidad DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.Existencias', N'FechaActualizacion') IS NULL
    ALTER TABLE dbo.[Existencias] ADD [FechaActualizacion] DATETIME2(0) NOT NULL CONSTRAINT DF_Existencias_FechaActualizacion DEFAULT (SYSUTCDATETIME());
GO

IF OBJECT_ID(N'dbo.[MovimientosInv]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[MovimientosInv] (
        [IdMovimiento] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientosInv PRIMARY KEY,
        [IdBodega] INT NOT NULL,
        [IdProducto] INT NOT NULL,
        [TipoMovimiento] NVARCHAR(20) NOT NULL CONSTRAINT DF_MovimientosInv_TipoMovimiento DEFAULT (N''),
        [Cantidad] DECIMAL(18,2) NOT NULL CONSTRAINT DF_MovimientosInv_Cantidad DEFAULT (0),
        [FechaMovimiento] DATETIME2(0) NOT NULL CONSTRAINT DF_MovimientosInv_FechaMovimiento DEFAULT (SYSUTCDATETIME()),
        [Observacion] NVARCHAR(300) NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.MovimientosInv', N'IdBodega') IS NULL
    ALTER TABLE dbo.[MovimientosInv] ADD [IdBodega] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.MovimientosInv', N'IdProducto') IS NULL
    ALTER TABLE dbo.[MovimientosInv] ADD [IdProducto] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.MovimientosInv', N'TipoMovimiento') IS NULL
    ALTER TABLE dbo.[MovimientosInv] ADD [TipoMovimiento] NVARCHAR(20) NOT NULL CONSTRAINT DF_MovimientosInv_TipoMovimiento DEFAULT (N'');
GO
IF COL_LENGTH(N'dbo.MovimientosInv', N'Cantidad') IS NULL
    ALTER TABLE dbo.[MovimientosInv] ADD [Cantidad] DECIMAL(18,2) NOT NULL CONSTRAINT DF_MovimientosInv_Cantidad DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.MovimientosInv', N'FechaMovimiento') IS NULL
    ALTER TABLE dbo.[MovimientosInv] ADD [FechaMovimiento] DATETIME2(0) NOT NULL CONSTRAINT DF_MovimientosInv_FechaMovimiento DEFAULT (SYSUTCDATETIME());
GO
IF COL_LENGTH(N'dbo.MovimientosInv', N'Observacion') IS NULL
    ALTER TABLE dbo.[MovimientosInv] ADD [Observacion] NVARCHAR(300) NULL;
GO

IF OBJECT_ID(N'dbo.[Stock]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Stock] (
        [IdStock] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stock PRIMARY KEY,
        [IdBodega] INT NOT NULL,
        [IdProducto] INT NOT NULL,
        [CantidadDisponible] DECIMAL(18,2) NOT NULL CONSTRAINT DF_Stock_CantidadDisponible DEFAULT (0),
        [FechaActualizacion] DATETIME2(0) NOT NULL CONSTRAINT DF_Stock_FechaActualizacion DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF COL_LENGTH(N'dbo.Stock', N'IdBodega') IS NULL
    ALTER TABLE dbo.[Stock] ADD [IdBodega] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Stock', N'IdProducto') IS NULL
    ALTER TABLE dbo.[Stock] ADD [IdProducto] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Stock', N'CantidadDisponible') IS NULL
    ALTER TABLE dbo.[Stock] ADD [CantidadDisponible] DECIMAL(18,2) NOT NULL CONSTRAINT DF_Stock_CantidadDisponible DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.Stock', N'FechaActualizacion') IS NULL
    ALTER TABLE dbo.[Stock] ADD [FechaActualizacion] DATETIME2(0) NOT NULL CONSTRAINT DF_Stock_FechaActualizacion DEFAULT (SYSUTCDATETIME());
GO

-- Relaciones internas e indices de consulta; IdProducto externo sigue siendo referencia logica.
DECLARE @Relaciones TABLE (Tabla sysname, Columna sysname, Padre sysname NULL, ClavePadre sysname NULL, Restriccion sysname NULL);
INSERT INTO @Relaciones VALUES
    (N'Existencias', N'IdProducto', NULL, NULL, NULL),
    (N'Existencias', N'IdBodega', N'Bodega', N'IdBodega', N'FK_Existencias_Bodega'),
    (N'MovimientosInv', N'IdBodega', N'Bodega', N'IdBodega', N'FK_MovimientosInv_Bodega'),
    (N'MovimientosInv', N'IdProducto', NULL, NULL, NULL),
    (N'Stock', N'IdBodega', N'Bodega', N'IdBodega', N'FK_Stock_Bodega'),
    (N'Stock', N'IdProducto', NULL, NULL, NULL);
DECLARE @Tabla sysname, @Columna sysname, @Padre sysname, @ClavePadre sysname, @Restriccion sysname;
DECLARE @IdTabla int, @Sql nvarchar(max), @Mensaje nvarchar(2048), @Indice sysname, @Sufijo int;

WHILE EXISTS (SELECT 1 FROM @Relaciones)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Columna, @Padre = Padre, @ClavePadre = ClavePadre, @Restriccion = Restriccion FROM @Relaciones;
    DELETE FROM @Relaciones WHERE Tabla = @Tabla AND Columna = @Columna;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla));

    IF @Padre IS NOT NULL
    BEGIN
        -- Aceptar una FK equivalente con otro nombre, sin crear relaciones duplicadas.
        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
            WHERE fk.parent_object_id = @IdTabla AND fk.referenced_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Padre))
                AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @Columna
                AND COL_NAME(fc.referenced_object_id, fc.referenced_column_id) = @ClavePadre
                AND (SELECT COUNT(*) FROM sys.foreign_key_columns todas WHERE todas.constraint_object_id = fk.object_id) = 1)
        BEGIN
            IF OBJECT_ID(N'dbo.' + QUOTENAME(@Restriccion)) IS NOT NULL
            BEGIN
                SET @Mensaje = CONCAT(DB_NAME(), N': la restriccion ', @Restriccion,
                    N' ya existe con una definicion diferente. Revisa el esquema antes de continuar.');
                THROW 50004, @Mensaje, 1;
            END;
            SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' WITH CHECK ADD CONSTRAINT '
                + QUOTENAME(@Restriccion) + N' FOREIGN KEY (' + QUOTENAME(@Columna) + N') REFERENCES dbo.'
                + QUOTENAME(@Padre) + N' (' + QUOTENAME(@ClavePadre) + N');';
            EXEC sys.sp_executesql @Sql;
        END;

        -- Reactivar y comprobar tambien las FK existentes, incluso si tenian otro nombre.
        SELECT @Sql = STRING_AGG(CAST(N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' WITH CHECK CHECK CONSTRAINT '
                + QUOTENAME(fk.name) + N';' AS nvarchar(max)), NCHAR(10))
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
        WHERE fk.parent_object_id = @IdTabla AND fk.referenced_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Padre))
            AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @Columna
            AND COL_NAME(fc.referenced_object_id, fc.referenced_column_id) = @ClavePadre
            AND (SELECT COUNT(*) FROM sys.foreign_key_columns todas WHERE todas.constraint_object_id = fk.object_id) = 1;
        IF @Sql IS NOT NULL EXEC sys.sp_executesql @Sql;
    END;

    -- Un indice cuyo primer campo sea la referencia tambien satisface esta consulta.
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = @IdTabla AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.has_filter = 0
            AND ic.key_ordinal = 1 AND COL_NAME(ic.object_id, ic.column_id) = @Columna)
    BEGIN
        SET @Indice = N'IX_' + @Tabla + N'_' + @Columna;
        SET @Sufijo = 0;
        -- Conservar cualquier indice previo con ese nombre y una definicion diferente.
        WHILE EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @IdTabla AND name = @Indice)
        BEGIN
            SET @Sufijo += 1;
            SET @Indice = N'IX_' + @Tabla + N'_' + @Columna + N'_FK' + CONVERT(nvarchar(10), @Sufijo);
        END;
        SET @Sql = N'CREATE INDEX ' + QUOTENAME(@Indice) + N' ON dbo.' + QUOTENAME(@Tabla) + N' (' + QUOTENAME(@Columna) + N');';
        EXEC sys.sp_executesql @Sql;
    END;
END;
GO

-- Comprobar tablas creadas
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME;
GO

-- ================================================================
-- MICROSERVICIO PEDIDOS / PedidosDB
-- ================================================================
IF DB_ID(N'PedidosDB') IS NULL
    EXEC (N'CREATE DATABASE [PedidosDB]');
GO
USE [PedidosDB];
GO

-- Validar el esquema existente antes de completar esta base.
-- Si falla, corregir o migrar los datos indicados y volver a ejecutar con sqlcmd -b.
DECLARE @Claves TABLE (Tabla sysname, Clave sysname);
INSERT INTO @Claves VALUES (N'Cliente', N'IdCliente'), (N'Pedido', N'IdPedido'), (N'DetallePedido', N'IdDetallePedido');
DECLARE @Referencias TABLE (Tabla sysname, Columna sysname, Padre sysname NULL, ClavePadre sysname NULL);
INSERT INTO @Referencias VALUES
    (N'Pedido', N'IdCliente', N'Cliente', N'IdCliente'),
    (N'DetallePedido', N'IdPedido', N'Pedido', N'IdPedido'),
    (N'DetallePedido', N'IdProducto', NULL, NULL);
DECLARE @Tabla sysname, @Columna sysname, @Padre sysname, @ClavePadre sysname;
DECLARE @IdTabla int, @Problema bit, @Sql nvarchar(max), @Mensaje nvarchar(2048);

WHILE EXISTS (SELECT 1 FROM @Claves)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Clave FROM @Claves;
    DELETE FROM @Claves WHERE Tabla = @Tabla;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U');
    IF @IdTabla IS NOT NULL AND (
        NOT EXISTS (SELECT 1 FROM sys.columns
            WHERE object_id = @IdTabla AND name = @Columna
                AND system_type_id = 56 AND is_identity = 1 AND is_nullable = 0)
        OR NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = @IdTabla AND i.is_primary_key = 1
                AND ic.key_ordinal = 1 AND c.name = @Columna
                AND NOT EXISTS (SELECT 1 FROM sys.index_columns otro
                    WHERE otro.object_id = i.object_id AND otro.index_id = i.index_id AND otro.key_ordinal > 1)))
    BEGIN
        SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N': se requiere ', @Columna,
            N' INT IDENTITY y clave primaria de una sola columna. Migra esta tabla antes de continuar; no se modificaron sus claves.');
        THROW 50001, @Mensaje, 1;
    END;
END;

WHILE EXISTS (SELECT 1 FROM @Referencias)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Columna, @Padre = Padre, @ClavePadre = ClavePadre FROM @Referencias;
    DELETE FROM @Referencias WHERE Tabla = @Tabla AND Columna = @Columna;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla), N'U');
    IF @IdTabla IS NULL CONTINUE;

    SET @Problema = 0;
    IF COL_LENGTH(N'dbo.' + QUOTENAME(@Tabla), @Columna) IS NULL
    BEGIN
        SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') SET @Encontrado = 1;';
        EXEC sys.sp_executesql @Sql, N'@Encontrado bit OUTPUT', @Encontrado = @Problema OUTPUT;
        IF @Problema = 1
        BEGIN
            SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N': falta ', @Columna,
                N' y la tabla contiene filas. Asigna las referencias reales mediante una migracion antes de continuar.');
            THROW 50002, @Mensaje, 1;
        END;
    END
    ELSE IF @Padre IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'dbo.' + QUOTENAME(@Padre), N'U') IS NULL
            SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N') SET @Encontrado = 1;';
        ELSE
            SET @Sql = N'IF EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Tabla) + N' hijo WHERE hijo.' + QUOTENAME(@Columna)
                + N' IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@Padre) + N' padre WHERE padre.'
                + QUOTENAME(@ClavePadre) + N' = hijo.' + QUOTENAME(@Columna) + N')) SET @Encontrado = 1;';
        EXEC sys.sp_executesql @Sql, N'@Encontrado bit OUTPUT', @Encontrado = @Problema OUTPUT;
        IF @Problema = 1
        BEGIN
            SET @Mensaje = CONCAT(DB_NAME(), N'.dbo.', @Tabla, N'.', @Columna, N': hay referencias nulas o sin registro en dbo.',
                @Padre, N'.', @ClavePadre, N'. Corrige los registros antes de crear o habilitar la clave foranea.');
            THROW 50003, @Mensaje, 1;
        END;
    END;
END;
GO

IF OBJECT_ID(N'dbo.[Cliente]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Cliente] (
        [IdCliente] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cliente PRIMARY KEY,
        [Nombre] NVARCHAR(150) NOT NULL CONSTRAINT DF_Cliente_Nombre DEFAULT (N''),
        [Correo] NVARCHAR(150) NULL,
        [Telefono] NVARCHAR(30) NULL
    );
END;
GO

IF COL_LENGTH(N'dbo.Cliente', N'Nombre') IS NULL
    ALTER TABLE dbo.[Cliente] ADD [Nombre] NVARCHAR(150) NOT NULL CONSTRAINT DF_Cliente_Nombre DEFAULT (N'');
GO
IF COL_LENGTH(N'dbo.Cliente', N'Correo') IS NULL
    ALTER TABLE dbo.[Cliente] ADD [Correo] NVARCHAR(150) NULL;
GO
IF COL_LENGTH(N'dbo.Cliente', N'Telefono') IS NULL
    ALTER TABLE dbo.[Cliente] ADD [Telefono] NVARCHAR(30) NULL;
GO

IF OBJECT_ID(N'dbo.[Pedido]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Pedido] (
        [IdPedido] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pedido PRIMARY KEY,
        [IdCliente] INT NOT NULL,
        [FechaPedido] DATETIME2(0) NOT NULL CONSTRAINT DF_Pedido_FechaPedido DEFAULT (SYSUTCDATETIME()),
        [Estado] NVARCHAR(30) NOT NULL CONSTRAINT DF_Pedido_Estado DEFAULT (N'Pendiente'),
        [Total] DECIMAL(12,2) NOT NULL CONSTRAINT DF_Pedido_Total DEFAULT (0)
    );
END;
GO

IF COL_LENGTH(N'dbo.Pedido', N'IdCliente') IS NULL
    ALTER TABLE dbo.[Pedido] ADD [IdCliente] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.Pedido', N'FechaPedido') IS NULL
    ALTER TABLE dbo.[Pedido] ADD [FechaPedido] DATETIME2(0) NOT NULL CONSTRAINT DF_Pedido_FechaPedido DEFAULT (SYSUTCDATETIME());
GO
IF COL_LENGTH(N'dbo.Pedido', N'Estado') IS NULL
    ALTER TABLE dbo.[Pedido] ADD [Estado] NVARCHAR(30) NOT NULL CONSTRAINT DF_Pedido_Estado DEFAULT (N'Pendiente');
GO
IF COL_LENGTH(N'dbo.Pedido', N'Total') IS NULL
    ALTER TABLE dbo.[Pedido] ADD [Total] DECIMAL(12,2) NOT NULL CONSTRAINT DF_Pedido_Total DEFAULT (0);
GO

IF OBJECT_ID(N'dbo.[DetallePedido]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DetallePedido] (
        [IdDetallePedido] INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DetallePedido PRIMARY KEY,
        [IdPedido] INT NOT NULL,
        [IdProducto] INT NOT NULL,
        [Cantidad] INT NOT NULL CONSTRAINT DF_DetallePedido_Cantidad DEFAULT (1),
        [PrecioUnitario] DECIMAL(10,2) NOT NULL CONSTRAINT DF_DetallePedido_PrecioUnitario DEFAULT (0),
        [Subtotal] DECIMAL(12,2) NOT NULL CONSTRAINT DF_DetallePedido_Subtotal DEFAULT (0)
    );
END;
GO

IF COL_LENGTH(N'dbo.DetallePedido', N'IdPedido') IS NULL
    ALTER TABLE dbo.[DetallePedido] ADD [IdPedido] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.DetallePedido', N'IdProducto') IS NULL
    ALTER TABLE dbo.[DetallePedido] ADD [IdProducto] INT NOT NULL;
GO
IF COL_LENGTH(N'dbo.DetallePedido', N'Cantidad') IS NULL
    ALTER TABLE dbo.[DetallePedido] ADD [Cantidad] INT NOT NULL CONSTRAINT DF_DetallePedido_Cantidad DEFAULT (1);
GO
IF COL_LENGTH(N'dbo.DetallePedido', N'PrecioUnitario') IS NULL
    ALTER TABLE dbo.[DetallePedido] ADD [PrecioUnitario] DECIMAL(10,2) NOT NULL CONSTRAINT DF_DetallePedido_PrecioUnitario DEFAULT (0);
GO
IF COL_LENGTH(N'dbo.DetallePedido', N'Subtotal') IS NULL
    ALTER TABLE dbo.[DetallePedido] ADD [Subtotal] DECIMAL(12,2) NOT NULL CONSTRAINT DF_DetallePedido_Subtotal DEFAULT (0);
GO

-- Relaciones internas e indices de consulta; IdProducto externo sigue siendo referencia logica.
DECLARE @Relaciones TABLE (Tabla sysname, Columna sysname, Padre sysname NULL, ClavePadre sysname NULL, Restriccion sysname NULL);
INSERT INTO @Relaciones VALUES
    (N'Pedido', N'IdCliente', N'Cliente', N'IdCliente', N'FK_Pedido_Cliente'),
    (N'DetallePedido', N'IdPedido', N'Pedido', N'IdPedido', N'FK_DetallePedido_Pedido'),
    (N'DetallePedido', N'IdProducto', NULL, NULL, NULL);
DECLARE @Tabla sysname, @Columna sysname, @Padre sysname, @ClavePadre sysname, @Restriccion sysname;
DECLARE @IdTabla int, @Sql nvarchar(max), @Mensaje nvarchar(2048), @Indice sysname, @Sufijo int;

WHILE EXISTS (SELECT 1 FROM @Relaciones)
BEGIN
    SELECT TOP (1) @Tabla = Tabla, @Columna = Columna, @Padre = Padre, @ClavePadre = ClavePadre, @Restriccion = Restriccion FROM @Relaciones;
    DELETE FROM @Relaciones WHERE Tabla = @Tabla AND Columna = @Columna;
    SET @IdTabla = OBJECT_ID(N'dbo.' + QUOTENAME(@Tabla));

    IF @Padre IS NOT NULL
    BEGIN
        -- Aceptar una FK equivalente con otro nombre, sin crear relaciones duplicadas.
        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
            WHERE fk.parent_object_id = @IdTabla AND fk.referenced_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Padre))
                AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @Columna
                AND COL_NAME(fc.referenced_object_id, fc.referenced_column_id) = @ClavePadre
                AND (SELECT COUNT(*) FROM sys.foreign_key_columns todas WHERE todas.constraint_object_id = fk.object_id) = 1)
        BEGIN
            IF OBJECT_ID(N'dbo.' + QUOTENAME(@Restriccion)) IS NOT NULL
            BEGIN
                SET @Mensaje = CONCAT(DB_NAME(), N': la restriccion ', @Restriccion,
                    N' ya existe con una definicion diferente. Revisa el esquema antes de continuar.');
                THROW 50004, @Mensaje, 1;
            END;
            SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' WITH CHECK ADD CONSTRAINT '
                + QUOTENAME(@Restriccion) + N' FOREIGN KEY (' + QUOTENAME(@Columna) + N') REFERENCES dbo.'
                + QUOTENAME(@Padre) + N' (' + QUOTENAME(@ClavePadre) + N');';
            EXEC sys.sp_executesql @Sql;
        END;

        -- Reactivar y comprobar tambien las FK existentes, incluso si tenian otro nombre.
        SELECT @Sql = STRING_AGG(CAST(N'ALTER TABLE dbo.' + QUOTENAME(@Tabla) + N' WITH CHECK CHECK CONSTRAINT '
                + QUOTENAME(fk.name) + N';' AS nvarchar(max)), NCHAR(10))
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
        WHERE fk.parent_object_id = @IdTabla AND fk.referenced_object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@Padre))
            AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = @Columna
            AND COL_NAME(fc.referenced_object_id, fc.referenced_column_id) = @ClavePadre
            AND (SELECT COUNT(*) FROM sys.foreign_key_columns todas WHERE todas.constraint_object_id = fk.object_id) = 1;
        IF @Sql IS NOT NULL EXEC sys.sp_executesql @Sql;
    END;

    -- Un indice cuyo primer campo sea la referencia tambien satisface esta consulta.
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.object_id = @IdTabla AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.has_filter = 0
            AND ic.key_ordinal = 1 AND COL_NAME(ic.object_id, ic.column_id) = @Columna)
    BEGIN
        SET @Indice = N'IX_' + @Tabla + N'_' + @Columna;
        SET @Sufijo = 0;
        -- Conservar cualquier indice previo con ese nombre y una definicion diferente.
        WHILE EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @IdTabla AND name = @Indice)
        BEGIN
            SET @Sufijo += 1;
            SET @Indice = N'IX_' + @Tabla + N'_' + @Columna + N'_FK' + CONVERT(nvarchar(10), @Sufijo);
        END;
        SET @Sql = N'CREATE INDEX ' + QUOTENAME(@Indice) + N' ON dbo.' + QUOTENAME(@Tabla) + N' (' + QUOTENAME(@Columna) + N');';
        EXEC sys.sp_executesql @Sql;
    END;
END;
GO

-- Comprobar tablas creadas
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME;
GO

