-- =====================================
-- Notes Application - SQL Server Schema
-- =====================================

IF DB_ID(N'techbodiaSQL') IS NULL
BEGIN
    PRINT 'CREATE DATABASE';
END
GO

-- ============
-- Table: users
-- ============
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE dbo.users (
        id             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        email          NVARCHAR(255)    NOT NULL,
        display_name   NVARCHAR(150)    NOT NULL,
        password_hash  NVARCHAR(MAX)    NOT NULL,
        created_at     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_users_email UNIQUE (email)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_users_email')
BEGIN
    CREATE INDEX idx_users_email ON dbo.users (email);
END
GO

-- ============
-- Table: notes
-- ============
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'notes')
BEGIN
    CREATE TABLE dbo.notes (
        id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        user_id     UNIQUEIDENTIFIER NOT NULL,
        title       NVARCHAR(255)    NOT NULL,
        content     NVARCHAR(MAX)    NOT NULL DEFAULT '',
        is_pinned   BIT              NOT NULL DEFAULT 0,
        created_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_notes_users FOREIGN KEY (user_id)
            REFERENCES dbo.users (id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_notes_user_id')
BEGIN
    CREATE INDEX idx_notes_user_id ON dbo.notes (user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_notes_user_id_updated_at')
BEGIN
    CREATE INDEX idx_notes_user_id_updated_at ON dbo.notes (user_id, updated_at DESC);
END
GO

-- ========
-- Trigger
-- ========
IF OBJECT_ID('dbo.trg_users_updated_at', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_users_updated_at;
GO

CREATE TRIGGER dbo.trg_users_updated_at
ON dbo.users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE u
    SET u.updated_at = SYSUTCDATETIME()
    FROM dbo.users u
    INNER JOIN inserted i ON u.id = i.id;
END
GO
