-- ============================================================
-- setup.sql  –  ItemProcessingApp  Database Setup Script
-- ============================================================
-- Run this in SQL Server Management Studio (SSMS) or sqlcmd
-- if you prefer NOT to use EF migrations.
--
-- Usage (sqlcmd):
--   sqlcmd -S . -E -i setup.sql
-- ============================================================

-- ── 1. Create the database ───────────────────────────────────
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ItemDB')
BEGIN
    CREATE DATABASE ItemDB;
    PRINT 'Database ItemDB created.';
END
ELSE
BEGIN
    PRINT 'Database ItemDB already exists – skipping creation.';
END
GO

USE ItemDB;
GO

-- ── 2. Create the EF Migrations history table ────────────────
-- (Required so that EF won't try to re-run the migration)
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = '__EFMigrationsHistory'
)
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId]    NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT 'Migration history table created.';
END
GO

-- ── 3. Create the Items table ────────────────────────────────
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'Items'
)
BEGIN
    CREATE TABLE [Items] (
        -- Primary key (auto-increment)
        [Id]       INT             NOT NULL IDENTITY(1,1),

        -- Item name, required, max 100 chars
        [Name]     NVARCHAR(100)   NOT NULL,

        -- Weight, stored with 3 decimal places; must be > 0 (enforced in app)
        [Weight]   DECIMAL(18, 3)  NOT NULL,

        -- Self-referencing FK: NULL means this is a root item
        [ParentId] INT             NULL,

        CONSTRAINT [PK_Items] PRIMARY KEY ([Id]),

        CONSTRAINT [FK_Items_Items_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [Items] ([Id])
            ON DELETE NO ACTION   -- children must be removed before parent
            ON UPDATE NO ACTION
    );

    -- Index for fast parent → children lookups
    CREATE INDEX [IX_Items_ParentId] ON [Items] ([ParentId]);

    PRINT 'Items table created.';
END
ELSE
BEGIN
    PRINT 'Items table already exists – skipping creation.';
END
GO

-- ── 4. Record the migration so EF won't duplicate it ─────────
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = '20240101000000_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20240101000000_InitialCreate', '7.0.20');
    PRINT 'Migration recorded.';
END
GO

-- ── 5. Seed sample data (optional) ───────────────────────────
-- Uncomment this block to pre-populate the database with a
-- small sample tree for testing purposes.

/*
-- Root items
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Root Assembly A', 10.500, NULL);
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Root Assembly B', 8.250, NULL);

-- Children of Root Assembly A (Id = 1)
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Sub-Part A1', 3.000, 1);
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Sub-Part A2', 4.200, 1);
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Sub-Part A3', 2.800, 1);

-- Children of Root Assembly B (Id = 2)
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Sub-Part B1', 1.500, 2);
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Sub-Part B2', 3.750, 2);

-- Grandchildren of Sub-Part A1 (Id = 3)
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Component A1-i',  0.500, 3);
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Component A1-ii', 0.800, 3);

-- Grandchild of Sub-Part B1 (Id = 6)
INSERT INTO [Items] ([Name], [Weight], [ParentId]) VALUES ('Component B1-i', 0.250, 6);
*/

PRINT '=== Setup complete. ItemDB is ready. ===';
GO
