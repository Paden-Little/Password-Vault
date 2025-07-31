IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'smw_hack_tracker')
BEGIN
    CREATE DATABASE smw_hack_tracker;
END
GO

USE smw_hack_tracker;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE Users (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Username NVARCHAR(255) UNIQUE NOT NULL,
        Email NVARCHAR(255) UNIQUE NOT NULL,
        Password NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_Users_Username ON Users(Username);
    CREATE INDEX IX_Users_Email ON Users(Email);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Passwords' AND xtype='U')
BEGIN
    CREATE TABLE Passwords (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Platform NVARCHAR(255) NOT NULL,
        Username NVARCHAR(255) NOT NULL,
        EncryptedPassword NVARCHAR(MAX) NOT NULL,
        Comment NVARCHAR(MAX),
        UserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Passwords_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_Passwords_User_Platform ON Passwords(UserId, Platform);
    CREATE INDEX IX_Passwords_User_Username ON Passwords(UserId, Username);
    CREATE INDEX IX_Passwords_CreatedAt ON Passwords(CreatedAt);
END
GO

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Users_UpdatedAt')
BEGIN
    EXEC('
    CREATE TRIGGER TR_Users_UpdatedAt
    ON Users
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE Users 
        SET UpdatedAt = GETUTCDATE() 
        FROM Users u
        INNER JOIN inserted i ON u.Id = i.Id;
    END
    ');
END
GO

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Passwords_UpdatedAt')
BEGIN
    EXEC('
    CREATE TRIGGER TR_Passwords_UpdatedAt
    ON Passwords
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        UPDATE Passwords 
        SET UpdatedAt = GETUTCDATE() 
        FROM Passwords p
        INNER JOIN inserted i ON p.Id = i.Id;
    END
    ');
END
GO

-- Testing data for testing runs
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'testuser')
BEGIN
    INSERT INTO Users (Id, Username, Email, Password) VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', 'testuser', 'test@example.com', 'hashed_password_here');
END
GO

-- More testing rows
IF NOT EXISTS (SELECT * FROM Passwords WHERE Platform = 'Gmail' AND UserId = '550e8400-e29b-41d4-a716-446655440000')
BEGIN
    INSERT INTO Passwords (Id, Platform, Username, EncryptedPassword, Comment, UserId) VALUES 
    ('650e8400-e29b-41d4-a716-446655440001', 'Gmail', 'testuser@gmail.com', 'encrypted_password_here', 'Primary email account', '550e8400-e29b-41d4-a716-446655440000'),
    ('650e8400-e29b-41d4-a716-446655440002', 'GitHub', 'testuser', 'encrypted_password_here', 'Development account', '550e8400-e29b-41d4-a716-446655440000');
END
GO

