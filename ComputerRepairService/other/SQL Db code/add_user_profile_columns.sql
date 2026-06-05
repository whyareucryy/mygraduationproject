-- Добавление полей аватара и описания профиля для всех пользователей
USE ComputerRepairService;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AspNetUsers') AND name = N'ProfileImagePath'
)
BEGIN
    ALTER TABLE AspNetUsers ADD ProfileImagePath NVARCHAR(500) NULL;
    PRINT 'Колонка ProfileImagePath добавлена.';
END
ELSE
    PRINT 'Колонка ProfileImagePath уже существует.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'AspNetUsers') AND name = N'Bio'
)
BEGIN
    ALTER TABLE AspNetUsers ADD Bio NVARCHAR(1000) NULL;
    PRINT 'Колонка Bio добавлена.';
END
ELSE
    PRINT 'Колонка Bio уже существует.';
GO
