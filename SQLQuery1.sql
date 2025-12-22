/* =========================================================
   BareORM - SQL Server Demo Objects (Enterprise-ready)
   - Stored procedures for testing:
     - Execute / ExecuteScalar / DataTable / DataSet
     - Multi result sets
     - Output params + Return Value
     - TVP (table-valued parameter)
     - Timeout
   ========================================================= */

USE [TuBaseDeDatos];
GO

/* ----------------------------
   1) Tablas demo
---------------------------- */

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Email         NVARCHAR(200) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
        DisplayName   NVARCHAR(200) NOT NULL,
        IsActive      BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
        CreatedAt     DATETIME2(3) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        UserId        INT NOT NULL,
        OrderNumber   NVARCHAR(30) NOT NULL CONSTRAINT UQ_Orders_OrderNumber UNIQUE,
        TotalAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Total DEFAULT(0),
        CreatedAt     DATETIME2(3) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF OBJECT_ID('dbo.OrderItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
        OrderId       INT NOT NULL,
        SKU           NVARCHAR(80) NOT NULL,
        Qty           INT NOT NULL,
        UnitPrice     DECIMAL(18,2) NOT NULL,
        LineTotal     AS (CONVERT(DECIMAL(18,2), Qty * UnitPrice)) PERSISTED,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId)
    );

    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
END
GO

/* ----------------------------
   2) TVP Type (OrderItems)
---------------------------- */
IF TYPE_ID('dbo.TvpOrderItem') IS NOT NULL
    DROP TYPE dbo.TvpOrderItem;
GO

CREATE TYPE dbo.TvpOrderItem AS TABLE
(
    SKU       NVARCHAR(80) NOT NULL,
    Qty       INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);
GO

/* =========================================================
   STORED PROCEDURES
   ========================================================= */

/* ----------------------------
   A) NonQuery + Output + ReturnValue
   - Crea usuario si no existe
   - Output: @UserId
   - Return: 0 OK, 1 ya existía
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_User_Upsert
    @Email       NVARCHAR(200),
    @DisplayName NVARCHAR(200),
    @UserId      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReturnCode INT = 0;

    SELECT @UserId = UserId
    FROM dbo.Users
    WHERE Email = @Email;

    IF @UserId IS NULL
    BEGIN
        INSERT INTO dbo.Users (Email, DisplayName, IsActive)
        VALUES (@Email, @DisplayName, 1);

        SET @UserId = SCOPE_IDENTITY();
        SET @ReturnCode = 0;
    END
    ELSE
    BEGIN
        UPDATE dbo.Users
        SET DisplayName = @DisplayName
        WHERE UserId = @UserId;

        SET @ReturnCode = 1; -- ya existía
    END

    RETURN @ReturnCode;
END
GO

/* ----------------------------
   B) 1 ResultSet (DataTable)
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_User_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.Email,
        u.DisplayName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    WHERE u.UserId = @UserId;
END
GO

/* ----------------------------
   C) List (DataTable)
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_User_List
    @OnlyActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.Email,
        u.DisplayName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    WHERE (@OnlyActive = 0 OR u.IsActive = 1)
    ORDER BY u.UserId DESC;
END
GO

/* ----------------------------
   D) Scalar (ExecuteScalar)
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_User_Count
    @OnlyActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TotalUsers
    FROM dbo.Users
    WHERE (@OnlyActive = 0 OR IsActive = 1);
END
GO

/* ----------------------------
   E) Multi Result Set (DataSet / Reader NextResult)
   1) Usuario
   2) Ordenes del usuario
   3) Items de esas ordenes
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_User_OrderSnapshot
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- (1) User
    SELECT
        u.UserId, u.Email, u.DisplayName, u.IsActive, u.CreatedAt
    FROM dbo.Users u
    WHERE u.UserId = @UserId;

    -- (2) Orders
    SELECT
        o.OrderId, o.OrderNumber, o.TotalAmount, o.CreatedAt
    FROM dbo.Orders o
    WHERE o.UserId = @UserId
    ORDER BY o.OrderId DESC;

    -- (3) OrderItems (para todas las ordenes del usuario)
    SELECT
        oi.OrderId, oi.OrderItemId, oi.SKU, oi.Qty, oi.UnitPrice, oi.LineTotal
    FROM dbo.OrderItems oi
    INNER JOIN dbo.Orders o ON o.OrderId = oi.OrderId
    WHERE o.UserId = @UserId
    ORDER BY oi.OrderId DESC, oi.OrderItemId ASC;
END
GO

/* ----------------------------
   F) TVP + Transaction + Output
   - Crea una orden con sus items
   - Output: @OrderId, @Total
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_Order_CreateWithItems
    @UserId      INT,
    @OrderNumber NVARCHAR(30),
    @Items       dbo.TvpOrderItem READONLY,
    @OrderId     INT OUTPUT,
    @Total       DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        -- Validar user
        IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UserId)
        BEGIN
            RAISERROR('UserId does not exist.', 16, 1);
        END

        INSERT INTO dbo.Orders (UserId, OrderNumber, TotalAmount)
        VALUES (@UserId, @OrderNumber, 0);

        SET @OrderId = SCOPE_IDENTITY();

        INSERT INTO dbo.OrderItems (OrderId, SKU, Qty, UnitPrice)
        SELECT @OrderId, SKU, Qty, UnitPrice
        FROM @Items;

        SELECT @Total = COALESCE(SUM(CONVERT(DECIMAL(18,2), Qty * UnitPrice)), 0)
        FROM @Items;

        UPDATE dbo.Orders
        SET TotalAmount = @Total
        WHERE OrderId = @OrderId;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

/* ----------------------------
   G) Timeout test (para probar timeoutSeconds)
---------------------------- */
CREATE OR ALTER PROCEDURE dbo.spDemo_Timeout
    @Seconds INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    -- Validación (evita valores absurdos)
    IF @Seconds IS NULL OR @Seconds < 0 OR @Seconds > 3600
    BEGIN
        RAISERROR('Seconds must be between 0 and 3600.', 16, 1);
        RETURN;
    END

    DECLARE @Delay time(0) = DATEADD(SECOND, @Seconds, CONVERT(time(0), '00:00:00'));

    WAITFOR DELAY @Delay;

    SELECT
        'OK' AS Status,
        @Seconds AS WaitedSeconds,
        @Delay AS DelayValue;
END
GO

