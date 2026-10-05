/*
  AccountingDB - مخطط موحد مستند إلى مرجع نظام الأونكس في PDF المرفق.
  الفكرة الأساسية: الشركة/الفرع/الفترة سياق للمستند، والحسابات في GL_Accounts فقط،
  والمستند المرحّل ينشئ قيداً وحركة مخزون في معاملة واحدة.
*/

IF OBJECT_ID('dbo.System_Currencies','U') IS NULL
CREATE TABLE dbo.System_Currencies
(
    CurrencyID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_System_Currencies PRIMARY KEY,
    CurrencyCode nvarchar(10) NOT NULL CONSTRAINT UQ_System_Currencies_Code UNIQUE,
    CurrencyNameAr nvarchar(100) NOT NULL,
    IsBase bit NOT NULL CONSTRAINT DF_System_Currencies_IsBase DEFAULT(0),
    IsActive bit NOT NULL CONSTRAINT DF_System_Currencies_IsActive DEFAULT(1)
);

IF OBJECT_ID('dbo.System_Companies','U') IS NULL
CREATE TABLE dbo.System_Companies
(
    CompanyID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_System_Companies PRIMARY KEY,
    CompanyCode nvarchar(30) NOT NULL CONSTRAINT UQ_System_Companies_Code UNIQUE,
    CompanyNameAr nvarchar(200) NOT NULL,
    BaseCurrencyID int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_System_Companies_IsActive DEFAULT(1),
    CONSTRAINT FK_System_Companies_Currency FOREIGN KEY(BaseCurrencyID) REFERENCES dbo.System_Currencies(CurrencyID)
);

IF OBJECT_ID('dbo.System_Branches','U') IS NULL
CREATE TABLE dbo.System_Branches
(
    BranchID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_System_Branches PRIMARY KEY,
    CompanyID int NOT NULL,
    BranchCode nvarchar(30) NOT NULL,
    BranchNameAr nvarchar(200) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_System_Branches_IsActive DEFAULT(1),
    CONSTRAINT UQ_System_Branches UNIQUE(CompanyID, BranchCode),
    CONSTRAINT FK_System_Branches_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID)
);

IF OBJECT_ID('dbo.GL_FiscalYears','U') IS NULL
CREATE TABLE dbo.GL_FiscalYears
(
    FiscalYearID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GL_FiscalYears PRIMARY KEY,
    CompanyID int NOT NULL,
    YearName nvarchar(20) NOT NULL,
    StartDate date NOT NULL,
    EndDate date NOT NULL,
    IsClosed bit NOT NULL CONSTRAINT DF_GL_FiscalYears_IsClosed DEFAULT(0),
    CONSTRAINT UQ_GL_FiscalYears UNIQUE(CompanyID, YearName),
    CONSTRAINT CK_GL_FiscalYears_Dates CHECK(EndDate >= StartDate),
    CONSTRAINT FK_GL_FiscalYears_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID)
);

IF OBJECT_ID('dbo.GL_FiscalPeriods','U') IS NULL
CREATE TABLE dbo.GL_FiscalPeriods
(
    FiscalPeriodID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GL_FiscalPeriods PRIMARY KEY,
    FiscalYearID int NOT NULL,
    PeriodNo tinyint NOT NULL,
    PeriodName nvarchar(50) NOT NULL,
    StartDate date NOT NULL,
    EndDate date NOT NULL,
    IsClosed bit NOT NULL CONSTRAINT DF_GL_FiscalPeriods_IsClosed DEFAULT(0),
    CONSTRAINT UQ_GL_FiscalPeriods UNIQUE(FiscalYearID, PeriodNo),
    CONSTRAINT CK_GL_FiscalPeriods_Dates CHECK(EndDate >= StartDate),
    CONSTRAINT FK_GL_FiscalPeriods_Year FOREIGN KEY(FiscalYearID) REFERENCES dbo.GL_FiscalYears(FiscalYearID)
);

IF OBJECT_ID('dbo.GL_Accounts','U') IS NULL
CREATE TABLE dbo.GL_Accounts
(
    AccountID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GL_Accounts PRIMARY KEY,
    AccountCode nvarchar(50) NOT NULL CONSTRAINT UQ_GL_Accounts_Code UNIQUE,
    AccountNameAr nvarchar(200) NOT NULL,
    AccountNameEn nvarchar(200) NULL,
    ParentAccountCode nvarchar(50) NULL,
    AccountLevel int NOT NULL CONSTRAINT DF_GL_Accounts_Level DEFAULT(1),
    AccountType int NOT NULL CONSTRAINT DF_GL_Accounts_Type DEFAULT(1),
    IsPostable bit NOT NULL CONSTRAINT DF_GL_Accounts_Postable DEFAULT(0),
    ReportType int NOT NULL CONSTRAINT DF_GL_Accounts_ReportType DEFAULT(1),
    AccountGroup int NOT NULL CONSTRAINT DF_GL_Accounts_Group DEFAULT(1),
    AccountNature int NOT NULL CONSTRAINT DF_GL_Accounts_Nature DEFAULT(1),
    CloseType int NOT NULL CONSTRAINT DF_GL_Accounts_CloseType DEFAULT(0),
    AccountAnalysis int NOT NULL CONSTRAINT DF_GL_Accounts_Analysis DEFAULT(0),
    CurrencyCode nvarchar(10) NOT NULL CONSTRAINT DF_GL_Accounts_Currency DEFAULT('YER'),
    IsActive bit NOT NULL CONSTRAINT DF_GL_Accounts_IsActive DEFAULT(1),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_GL_Accounts_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT FK_GL_Accounts_Parent FOREIGN KEY(ParentAccountCode) REFERENCES dbo.GL_Accounts(AccountCode),
    CONSTRAINT CK_GL_Accounts_Level CHECK(AccountLevel >= 1),
    CONSTRAINT CK_GL_Accounts_Type CHECK(AccountType IN(1,2))
);
CREATE INDEX IX_GL_Accounts_Parent ON dbo.GL_Accounts(ParentAccountCode);

IF OBJECT_ID('dbo.CostCenterTypes','U') IS NULL
CREATE TABLE dbo.CostCenterTypes
(
    CostCenterTypeID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CostCenterTypes PRIMARY KEY,
    TypeNameAr nvarchar(100) NOT NULL CONSTRAINT UQ_CostCenterTypes_Name UNIQUE,
    IsActive bit NOT NULL CONSTRAINT DF_CostCenterTypes_IsActive DEFAULT(1)
);

IF OBJECT_ID('dbo.CostCenters','U') IS NULL
CREATE TABLE dbo.CostCenters
(
    CostCenterID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CostCenters PRIMARY KEY,
    CompanyID int NOT NULL,
    CostCenterTypeID int NULL,
    CostCenterCode nvarchar(50) NOT NULL,
    CostCenterNameAr nvarchar(200) NOT NULL,
    ParentCostCenterID int NULL,
    IsActive bit NOT NULL CONSTRAINT DF_CostCenters_IsActive DEFAULT(1),
    CONSTRAINT UQ_CostCenters UNIQUE(CompanyID, CostCenterCode),
    CONSTRAINT FK_CostCenters_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
    CONSTRAINT FK_CostCenters_Type FOREIGN KEY(CostCenterTypeID) REFERENCES dbo.CostCenterTypes(CostCenterTypeID),
    CONSTRAINT FK_CostCenters_Parent FOREIGN KEY(ParentCostCenterID) REFERENCES dbo.CostCenters(CostCenterID)
);

IF OBJECT_ID('dbo.GL_VouchersHeader','U') IS NULL
CREATE TABLE dbo.GL_VouchersHeader
(
    VoucherID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_GL_VouchersHeader PRIMARY KEY,
    CompanyID int NOT NULL,
    BranchID int NOT NULL,
    FiscalYearID int NOT NULL,
    FiscalPeriodID int NOT NULL,
    VoucherNo bigint NOT NULL,
    VoucherType tinyint NOT NULL,
    VoucherDate datetime2(0) NOT NULL,
    TreasuryAccountCode nvarchar(50) NULL,
    PaymentType tinyint NOT NULL CONSTRAINT DF_GL_VouchersHeader_PaymentType DEFAULT(0),
    CurrencyID int NOT NULL,
    ExchangeRate decimal(19,6) NOT NULL CONSTRAINT DF_GL_VouchersHeader_Rate DEFAULT(1),
    TotalAmount decimal(19,4) NOT NULL,
    LocalTotalAmount decimal(19,4) NOT NULL,
    SourceType nvarchar(50) NULL,
    SourceID bigint NULL,
    Notes nvarchar(500) NULL,
    CreatedBy int NOT NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_GL_VouchersHeader_CreatedAt DEFAULT(SYSUTCDATETIME()),
    IsPosted bit NOT NULL CONSTRAINT DF_GL_VouchersHeader_IsPosted DEFAULT(1),
    CONSTRAINT UQ_GL_VouchersHeader_No UNIQUE(CompanyID, BranchID, FiscalYearID, VoucherType, VoucherNo),
    CONSTRAINT FK_GL_VouchersHeader_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
    CONSTRAINT FK_GL_VouchersHeader_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
    CONSTRAINT FK_GL_VouchersHeader_Year FOREIGN KEY(FiscalYearID) REFERENCES dbo.GL_FiscalYears(FiscalYearID),
    CONSTRAINT FK_GL_VouchersHeader_Period FOREIGN KEY(FiscalPeriodID) REFERENCES dbo.GL_FiscalPeriods(FiscalPeriodID),
    CONSTRAINT FK_GL_VouchersHeader_Currency FOREIGN KEY(CurrencyID) REFERENCES dbo.System_Currencies(CurrencyID)
);

IF OBJECT_ID('dbo.GL_VoucherDetails','U') IS NULL
CREATE TABLE dbo.GL_VoucherDetails
(
    VoucherDetailID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_GL_VoucherDetails PRIMARY KEY,
    VoucherID bigint NOT NULL,
    VoucherLineNo int NOT NULL,
    AccountID int NOT NULL,
    DebitAmount decimal(19,4) NOT NULL CONSTRAINT DF_GL_VoucherDetails_Debit DEFAULT(0),
    CreditAmount decimal(19,4) NOT NULL CONSTRAINT DF_GL_VoucherDetails_Credit DEFAULT(0),
    ForeignAmount decimal(19,4) NOT NULL CONSTRAINT DF_GL_VoucherDetails_Foreign DEFAULT(0),
    LocalAmount decimal(19,4) NOT NULL CONSTRAINT DF_GL_VoucherDetails_Local DEFAULT(0),
    CostCenterID int NULL,
    ReferenceNo nvarchar(100) NULL,
    Notes nvarchar(500) NULL,
    CONSTRAINT UQ_GL_VoucherDetails_Line UNIQUE(VoucherID, VoucherLineNo),
    CONSTRAINT FK_GL_VoucherDetails_Header FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID),
    CONSTRAINT FK_GL_VoucherDetails_Account FOREIGN KEY(AccountID) REFERENCES dbo.GL_Accounts(AccountID),
    CONSTRAINT FK_GL_VoucherDetails_CostCenter FOREIGN KEY(CostCenterID) REFERENCES dbo.CostCenters(CostCenterID),
    CONSTRAINT CK_GL_VoucherDetails_OneSide CHECK((DebitAmount >= 0 AND CreditAmount >= 0) AND NOT(DebitAmount > 0 AND CreditAmount > 0))
);
CREATE INDEX IX_GL_VoucherDetails_Account ON dbo.GL_VoucherDetails(AccountID);

IF OBJECT_ID('dbo.GL_VoucherSequences','U') IS NULL
CREATE TABLE dbo.GL_VoucherSequences
(
    CompanyID int NOT NULL,
    BranchID int NOT NULL,
    FiscalYearID int NOT NULL,
    VoucherType tinyint NOT NULL,
    LastNo bigint NOT NULL CONSTRAINT DF_GL_VoucherSequences_LastNo DEFAULT(0),
    CONSTRAINT PK_GL_VoucherSequences PRIMARY KEY(CompanyID, BranchID, FiscalYearID, VoucherType)
);

IF OBJECT_ID('dbo.Items','U') IS NULL
CREATE TABLE dbo.Items
(
    ItemID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Items PRIMARY KEY,
    ItemCode nvarchar(50) NOT NULL CONSTRAINT UQ_Items_Code UNIQUE,
    ItemName nvarchar(200) NOT NULL,
    Barcode nvarchar(100) NULL,
    UnitName nvarchar(50) NULL,
    StockQuantity decimal(19,4) NOT NULL CONSTRAINT DF_Items_Stock DEFAULT(0),
    InventoryAccountCode nvarchar(50) NULL,
    SalesAccountCode nvarchar(50) NULL,
    CostOfSalesAccountCode nvarchar(50) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Items_IsActive DEFAULT(1)
);

IF OBJECT_ID('dbo.ItemBatches','U') IS NULL
CREATE TABLE dbo.ItemBatches
(
    BatchID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ItemBatches PRIMARY KEY,
    ItemID int NOT NULL,
    BranchID int NOT NULL,
    BatchNo nvarchar(100) NULL,
    ExpiryDate date NULL,
    CurrentQuantity decimal(19,4) NOT NULL CONSTRAINT DF_ItemBatches_Qty DEFAULT(0),
    CostPrice decimal(19,6) NOT NULL CONSTRAINT DF_ItemBatches_Cost DEFAULT(0),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_ItemBatches_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT FK_ItemBatches_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID),
    CONSTRAINT FK_ItemBatches_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID)
);
CREATE INDEX IX_ItemBatches_Fifo ON dbo.ItemBatches(ItemID, BranchID, ExpiryDate, BatchID);

IF OBJECT_ID('dbo.InventoryMovements','U') IS NULL
CREATE TABLE dbo.InventoryMovements
(
    InventoryMovementID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryMovements PRIMARY KEY,
    ItemID int NOT NULL,
    BatchID bigint NULL,
    BranchID int NOT NULL,
    MovementDate datetime2(0) NOT NULL,
    MovementType tinyint NOT NULL,
    QuantityIn decimal(19,4) NOT NULL CONSTRAINT DF_InventoryMovements_In DEFAULT(0),
    QuantityOut decimal(19,4) NOT NULL CONSTRAINT DF_InventoryMovements_Out DEFAULT(0),
    UnitCost decimal(19,6) NOT NULL CONSTRAINT DF_InventoryMovements_Cost DEFAULT(0),
    SourceType nvarchar(50) NULL,
    SourceID bigint NULL,
    VoucherID bigint NULL,
    Notes nvarchar(500) NULL,
    CONSTRAINT FK_InventoryMovements_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID),
    CONSTRAINT FK_InventoryMovements_Batch FOREIGN KEY(BatchID) REFERENCES dbo.ItemBatches(BatchID),
    CONSTRAINT FK_InventoryMovements_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
    CONSTRAINT FK_InventoryMovements_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID),
    CONSTRAINT CK_InventoryMovements_Side CHECK((QuantityIn >= 0 AND QuantityOut >= 0) AND NOT(QuantityIn > 0 AND QuantityOut > 0))
);

IF TYPE_ID(N'dbo.GL_VoucherDetailType') IS NULL
    EXEC(N'CREATE TYPE dbo.GL_VoucherDetailType AS TABLE
    (
        VoucherLineNo int NOT NULL,
        AccountCode nvarchar(50) NOT NULL,
        DebitAmount decimal(19,4) NOT NULL,
        CreditAmount decimal(19,4) NOT NULL,
        ForeignAmount decimal(19,4) NOT NULL,
        LocalAmount decimal(19,4) NOT NULL,
        CostCenterID int NULL,
        ReferenceNo nvarchar(100) NULL,
        Notes nvarchar(500) NULL
    )');
GO

CREATE OR ALTER PROCEDURE dbo.sp_GL_GetNextVoucherNo
    @VoucherType tinyint,
    @FiscalYearID int,
    @CompanyID int = 1,
    @BranchID int = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
    UPDATE dbo.GL_VoucherSequences WITH (UPDLOCK, HOLDLOCK)
       SET LastNo = LastNo + 1
     WHERE CompanyID=@CompanyID AND BranchID=@BranchID AND FiscalYearID=@FiscalYearID AND VoucherType=@VoucherType;
    IF @@ROWCOUNT = 0
    BEGIN
        INSERT dbo.GL_VoucherSequences(CompanyID,BranchID,FiscalYearID,VoucherType,LastNo)
        VALUES(@CompanyID,@BranchID,@FiscalYearID,@VoucherType,1);
    END
    SELECT LastNo FROM dbo.GL_VoucherSequences
     WHERE CompanyID=@CompanyID AND BranchID=@BranchID AND FiscalYearID=@FiscalYearID AND VoucherType=@VoucherType;
    COMMIT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GL_Vouchers_Save
    @VoucherNo bigint,
    @VoucherType tinyint,
    @VoucherDate datetime2(0),
    @CompanyID int,
    @BranchID int,
    @FiscalYearID int,
    @FiscalPeriodID int,
    @TreasuryAccountCode nvarchar(50) = NULL,
    @PaymentType tinyint = 0,
    @CurrencyID int,
    @ExchangeRate decimal(19,6),
    @TotalAmount decimal(19,4),
    @LocalTotalAmount decimal(19,4),
    @SourceType nvarchar(50) = NULL,
    @SourceID bigint = NULL,
    @Notes nvarchar(500) = NULL,
    @CreatedBy int,
    @Details dbo.GL_VoucherDetailType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM @Details) THROW 51000, N'لا توجد تفاصيل للقيد.', 1;
    IF ABS((SELECT ISNULL(SUM(DebitAmount),0)-ISNULL(SUM(CreditAmount),0) FROM @Details)) > 0.01
        THROW 51001, N'القيد غير متوازن.', 1;
    IF EXISTS(SELECT 1 FROM @Details d LEFT JOIN dbo.GL_Accounts a ON a.AccountCode=d.AccountCode
              WHERE a.AccountID IS NULL OR a.IsActive=0 OR a.IsPostable=0)
        THROW 51002, N'يوجد حساب غير موجود أو غير قابل للترحيل.', 1;
    IF EXISTS(SELECT 1 FROM dbo.GL_FiscalYears WHERE FiscalYearID=@FiscalYearID AND IsClosed=1)
        THROW 51003, N'السنة المالية مغلقة.', 1;
    IF EXISTS(SELECT 1 FROM dbo.GL_FiscalPeriods WHERE FiscalPeriodID=@FiscalPeriodID AND IsClosed=1)
        THROW 51004, N'الفترة المالية مغلقة.', 1;

    BEGIN TRAN;
    DECLARE @VoucherID bigint;
    INSERT dbo.GL_VouchersHeader(CompanyID,BranchID,FiscalYearID,FiscalPeriodID,VoucherNo,VoucherType,VoucherDate,
        TreasuryAccountCode,PaymentType,CurrencyID,ExchangeRate,TotalAmount,LocalTotalAmount,SourceType,SourceID,Notes,CreatedBy)
    VALUES(@CompanyID,@BranchID,@FiscalYearID,@FiscalPeriodID,@VoucherNo,@VoucherType,@VoucherDate,
        @TreasuryAccountCode,@PaymentType,@CurrencyID,@ExchangeRate,@TotalAmount,@LocalTotalAmount,@SourceType,@SourceID,@Notes,@CreatedBy);
    SET @VoucherID = SCOPE_IDENTITY();

    INSERT dbo.GL_VoucherDetails(VoucherID,VoucherLineNo,AccountID,DebitAmount,CreditAmount,ForeignAmount,LocalAmount,CostCenterID,ReferenceNo,Notes)
    SELECT @VoucherID,d.VoucherLineNo,a.AccountID,d.DebitAmount,d.CreditAmount,d.ForeignAmount,d.LocalAmount,d.CostCenterID,d.ReferenceNo,d.Notes
      FROM @Details d INNER JOIN dbo.GL_Accounts a ON a.AccountCode=d.AccountCode;
    COMMIT;
    SELECT @VoucherID;
END;
GO

DECLARE @BaseCurrencyID int, @DefaultCompanyID int, @MainBranchID int, @FiscalYearID int;
SELECT @BaseCurrencyID=CurrencyID FROM dbo.System_Currencies WHERE CurrencyCode=N'YER';
IF @BaseCurrencyID IS NULL
BEGIN
    INSERT dbo.System_Currencies(CurrencyCode,CurrencyNameAr,IsBase) VALUES(N'YER',N'ريال يمني',1);
    SET @BaseCurrencyID=CONVERT(int,SCOPE_IDENTITY());
END;
SELECT @DefaultCompanyID=CompanyID FROM dbo.System_Companies WHERE CompanyCode=N'DEFAULT';
IF @DefaultCompanyID IS NULL
BEGIN
    INSERT dbo.System_Companies(CompanyCode,CompanyNameAr,BaseCurrencyID) VALUES(N'DEFAULT',N'الشركة الافتراضية',@BaseCurrencyID);
    SET @DefaultCompanyID=CONVERT(int,SCOPE_IDENTITY());
END;
SELECT @MainBranchID=BranchID FROM dbo.System_Branches WHERE CompanyID=@DefaultCompanyID AND BranchCode=N'MAIN';
IF @MainBranchID IS NULL
BEGIN
    INSERT dbo.System_Branches(CompanyID,BranchCode,BranchNameAr) VALUES(@DefaultCompanyID,N'MAIN',N'الفرع الرئيسي');
    SET @MainBranchID=CONVERT(int,SCOPE_IDENTITY());
END;
SELECT @FiscalYearID=FiscalYearID FROM dbo.GL_FiscalYears WHERE CompanyID=@DefaultCompanyID AND YearName=N'2026';
IF @FiscalYearID IS NULL
BEGIN
    INSERT dbo.GL_FiscalYears(CompanyID,YearName,StartDate,EndDate) VALUES(@DefaultCompanyID,N'2026','2026-01-01','2026-12-31');
    SET @FiscalYearID=CONVERT(int,SCOPE_IDENTITY());
END;
IF NOT EXISTS(SELECT 1 FROM dbo.GL_FiscalPeriods WHERE FiscalYearID=@FiscalYearID AND PeriodNo=1)
    INSERT dbo.GL_FiscalPeriods(FiscalYearID,PeriodNo,PeriodName,StartDate,EndDate) VALUES(@FiscalYearID,1,N'يناير','2026-01-01','2026-01-31');

/* المستندات التشغيلية: المصدر التشغيلي للحركة والقيد */
IF OBJECT_ID('dbo.Suppliers','U') IS NULL
CREATE TABLE dbo.Suppliers
(
    SupplierID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    SupplierCode nvarchar(50) NOT NULL CONSTRAINT UQ_Suppliers_Code UNIQUE,
    SupplierNameAr nvarchar(200) NOT NULL,
    PayableAccountCode nvarchar(50) NULL,
    Balance decimal(19,4) NOT NULL CONSTRAINT DF_Suppliers_Balance DEFAULT(0),
    IsActive bit NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT(1)
);

IF OBJECT_ID('dbo.Customers','U') IS NULL
CREATE TABLE dbo.Customers
(
    CustomerID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerCode nvarchar(50) NOT NULL CONSTRAINT UQ_Customers_Code UNIQUE,
    CustomerNameAr nvarchar(200) NOT NULL,
    ReceivableAccountCode nvarchar(50) NULL,
    Balance decimal(19,4) NOT NULL CONSTRAINT DF_Customers_Balance DEFAULT(0),
    IsActive bit NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT(1)
);

IF OBJECT_ID('dbo.PurchaseInvoices','U') IS NULL
CREATE TABLE dbo.PurchaseInvoices
(
    PurchaseInvoiceID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoices PRIMARY KEY,
    CompanyID int NOT NULL,
    BranchID int NOT NULL,
    SupplierID int NOT NULL,
    InvoiceNumber nvarchar(100) NOT NULL,
    InvoiceDate datetime2(0) NOT NULL,
    FiscalYearID int NOT NULL,
    FiscalPeriodID int NOT NULL,
    TotalAmount decimal(19,4) NOT NULL,
    PaidAmount decimal(19,4) NOT NULL CONSTRAINT DF_PurchaseInvoices_Paid DEFAULT(0),
    RemainingAmount AS (TotalAmount - PaidAmount) PERSISTED,
    VoucherID bigint NULL,
    IsPosted bit NOT NULL CONSTRAINT DF_PurchaseInvoices_Posted DEFAULT(0),
    Notes nvarchar(500) NULL,
    CONSTRAINT UQ_PurchaseInvoices_Number UNIQUE(CompanyID, BranchID, InvoiceNumber),
    CONSTRAINT FK_PurchaseInvoices_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
    CONSTRAINT FK_PurchaseInvoices_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
    CONSTRAINT FK_PurchaseInvoices_Supplier FOREIGN KEY(SupplierID) REFERENCES dbo.Suppliers(SupplierID),
    CONSTRAINT FK_PurchaseInvoices_Year FOREIGN KEY(FiscalYearID) REFERENCES dbo.GL_FiscalYears(FiscalYearID),
    CONSTRAINT FK_PurchaseInvoices_Period FOREIGN KEY(FiscalPeriodID) REFERENCES dbo.GL_FiscalPeriods(FiscalPeriodID),
    CONSTRAINT FK_PurchaseInvoices_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID)
);

IF OBJECT_ID('dbo.PurchaseInvoiceDetails','U') IS NULL
CREATE TABLE dbo.PurchaseInvoiceDetails
(
    PurchaseInvoiceDetailID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoiceDetails PRIMARY KEY,
    PurchaseInvoiceID bigint NOT NULL,
    ItemID int NOT NULL,
    Quantity decimal(19,4) NOT NULL,
    UnitPrice decimal(19,6) NOT NULL,
    BatchNumber nvarchar(100) NULL,
    ExpiryDate date NULL,
    CONSTRAINT FK_PurchaseInvoiceDetails_Header FOREIGN KEY(PurchaseInvoiceID) REFERENCES dbo.PurchaseInvoices(PurchaseInvoiceID),
    CONSTRAINT FK_PurchaseInvoiceDetails_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID),
    CONSTRAINT CK_PurchaseInvoiceDetails_Qty CHECK(Quantity > 0),
    CONSTRAINT CK_PurchaseInvoiceDetails_Price CHECK(UnitPrice >= 0)
);

IF OBJECT_ID('dbo.SalesInvoices','U') IS NULL
CREATE TABLE dbo.SalesInvoices
(
    SalesInvoiceID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoices PRIMARY KEY,
    CompanyID int NOT NULL,
    BranchID int NOT NULL,
    CustomerID int NULL,
    InvoiceNumber nvarchar(100) NOT NULL,
    InvoiceDate datetime2(0) NOT NULL,
    FiscalYearID int NOT NULL,
    FiscalPeriodID int NOT NULL,
    TotalAmount decimal(19,4) NOT NULL,
    PaidAmount decimal(19,4) NOT NULL CONSTRAINT DF_SalesInvoices_Paid DEFAULT(0),
    RemainingAmount AS (TotalAmount - PaidAmount) PERSISTED,
    VoucherID bigint NULL,
    IsPosted bit NOT NULL CONSTRAINT DF_SalesInvoices_Posted DEFAULT(0),
    CONSTRAINT UQ_SalesInvoices_Number UNIQUE(CompanyID, BranchID, InvoiceNumber),
    CONSTRAINT FK_SalesInvoices_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
    CONSTRAINT FK_SalesInvoices_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
    CONSTRAINT FK_SalesInvoices_Customer FOREIGN KEY(CustomerID) REFERENCES dbo.Customers(CustomerID),
    CONSTRAINT FK_SalesInvoices_Year FOREIGN KEY(FiscalYearID) REFERENCES dbo.GL_FiscalYears(FiscalYearID),
    CONSTRAINT FK_SalesInvoices_Period FOREIGN KEY(FiscalPeriodID) REFERENCES dbo.GL_FiscalPeriods(FiscalPeriodID),
    CONSTRAINT FK_SalesInvoices_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID)
);

IF OBJECT_ID('dbo.SalesInvoiceDetails','U') IS NULL
CREATE TABLE dbo.SalesInvoiceDetails
(
    SalesInvoiceDetailID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoiceDetails PRIMARY KEY,
    SalesInvoiceID bigint NOT NULL,
    ItemID int NOT NULL,
    Quantity decimal(19,4) NOT NULL,
    UnitPrice decimal(19,6) NOT NULL,
    BatchID bigint NULL,
    CostPrice decimal(19,6) NOT NULL CONSTRAINT DF_SalesInvoiceDetails_Cost DEFAULT(0),
    CONSTRAINT FK_SalesInvoiceDetails_Header FOREIGN KEY(SalesInvoiceID) REFERENCES dbo.SalesInvoices(SalesInvoiceID),
    CONSTRAINT FK_SalesInvoiceDetails_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID),
    CONSTRAINT FK_SalesInvoiceDetails_Batch FOREIGN KEY(BatchID) REFERENCES dbo.ItemBatches(BatchID),
    CONSTRAINT CK_SalesInvoiceDetails_Qty CHECK(Quantity > 0),
    CONSTRAINT CK_SalesInvoiceDetails_Price CHECK(UnitPrice >= 0)
);

IF OBJECT_ID('dbo.SalesReturns','U') IS NULL
CREATE TABLE dbo.SalesReturns
(
    SalesReturnID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturns PRIMARY KEY,
    SalesInvoiceID bigint NOT NULL,
    ReturnDate datetime2(0) NOT NULL,
    TotalAmount decimal(19,4) NOT NULL,
    VoucherID bigint NULL,
    Notes nvarchar(500) NULL,
    CONSTRAINT FK_SalesReturns_Invoice FOREIGN KEY(SalesInvoiceID) REFERENCES dbo.SalesInvoices(SalesInvoiceID),
    CONSTRAINT FK_SalesReturns_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID)
);

IF OBJECT_ID('dbo.SalesReturnDetails','U') IS NULL
CREATE TABLE dbo.SalesReturnDetails
(
    SalesReturnDetailID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturnDetails PRIMARY KEY,
    SalesReturnID bigint NOT NULL,
    ItemID int NOT NULL,
    BatchID bigint NULL,
    Quantity decimal(19,4) NOT NULL,
    UnitPrice decimal(19,6) NOT NULL,
    CostPrice decimal(19,6) NOT NULL CONSTRAINT DF_SalesReturnDetails_Cost DEFAULT(0),
    CONSTRAINT FK_SalesReturnDetails_Return FOREIGN KEY(SalesReturnID) REFERENCES dbo.SalesReturns(SalesReturnID),
    CONSTRAINT FK_SalesReturnDetails_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID),
    CONSTRAINT FK_SalesReturnDetails_Batch FOREIGN KEY(BatchID) REFERENCES dbo.ItemBatches(BatchID),
    CONSTRAINT CK_SalesReturnDetails_Qty CHECK(Quantity > 0)
);

/* توافق بطاقة الصنف مع دورة المبيعات ونقطة البيع: أسعار افتراضية وحد إعادة الطلب. */
IF COL_LENGTH('dbo.Items','CostPrice') IS NULL
    ALTER TABLE dbo.Items ADD CostPrice decimal(19,6) NOT NULL CONSTRAINT DF_Items_CostPrice DEFAULT(0);
IF COL_LENGTH('dbo.Items','UnitPrice') IS NULL
    ALTER TABLE dbo.Items ADD UnitPrice decimal(19,6) NOT NULL CONSTRAINT DF_Items_UnitPrice DEFAULT(0);
IF COL_LENGTH('dbo.Items','ItemType') IS NULL
    ALTER TABLE dbo.Items ADD ItemType tinyint NOT NULL CONSTRAINT DF_Items_ItemType DEFAULT(1);
IF COL_LENGTH('dbo.Items','ReorderLevel') IS NULL
    ALTER TABLE dbo.Items ADD ReorderLevel decimal(19,4) NOT NULL CONSTRAINT DF_Items_ReorderLevel DEFAULT(0);
GO

/* حقول إضافية لدورة العملات في مرجع الأونكس مع الاحتفاظ بالاسم الموحد للعملة. */
IF COL_LENGTH('dbo.System_Currencies','ForeignName') IS NULL
    ALTER TABLE dbo.System_Currencies ADD ForeignName nvarchar(100) NULL;
IF COL_LENGTH('dbo.System_Currencies','ExchangeRate') IS NULL
    ALTER TABLE dbo.System_Currencies ADD ExchangeRate decimal(19,6) NOT NULL CONSTRAINT DF_System_Currencies_ExchangeRate DEFAULT(1);
IF COL_LENGTH('dbo.System_Currencies','MinExchangeRate') IS NULL
    ALTER TABLE dbo.System_Currencies ADD MinExchangeRate decimal(19,6) NULL;
IF COL_LENGTH('dbo.System_Currencies','MaxExchangeRate') IS NULL
    ALTER TABLE dbo.System_Currencies ADD MaxExchangeRate decimal(19,6) NULL;
IF COL_LENGTH('dbo.System_Currencies','FractionsCount') IS NULL
    ALTER TABLE dbo.System_Currencies ADD FractionsCount tinyint NOT NULL CONSTRAINT DF_System_Currencies_Fractions DEFAULT(2);
GO

/* دورة نقطة البيع: وردية الصندوق والفواتير المعلقة وحقول البيع الإضافية. */
IF COL_LENGTH('dbo.SalesInvoices','ShiftID') IS NULL
    ALTER TABLE dbo.SalesInvoices ADD ShiftID int NULL;
IF COL_LENGTH('dbo.SalesInvoices','DiscountAmount') IS NULL
    ALTER TABLE dbo.SalesInvoices ADD DiscountAmount decimal(19,4) NOT NULL CONSTRAINT DF_SalesInvoices_Discount DEFAULT(0);
IF COL_LENGTH('dbo.SalesInvoices','NetAmount') IS NULL
    ALTER TABLE dbo.SalesInvoices ADD NetAmount decimal(19,4) NOT NULL CONSTRAINT DF_SalesInvoices_Net DEFAULT(0);
IF COL_LENGTH('dbo.SalesInvoices','PaymentType') IS NULL
    ALTER TABLE dbo.SalesInvoices ADD PaymentType tinyint NOT NULL CONSTRAINT DF_SalesInvoices_PaymentType DEFAULT(1);

IF OBJECT_ID('dbo.Shifts','U') IS NULL
CREATE TABLE dbo.Shifts
(
    ShiftID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shifts PRIMARY KEY,
    CompanyID int NOT NULL,
    BranchID int NOT NULL,
    CashierName nvarchar(200) NOT NULL,
    StartTime datetime2(0) NOT NULL CONSTRAINT DF_Shifts_Start DEFAULT(SYSUTCDATETIME()),
    EndTime datetime2(0) NULL,
    OpeningBalance decimal(19,4) NOT NULL CONSTRAINT DF_Shifts_Opening DEFAULT(0),
    CashSales decimal(19,4) NOT NULL CONSTRAINT DF_Shifts_Cash DEFAULT(0),
    CardSales decimal(19,4) NOT NULL CONSTRAINT DF_Shifts_Card DEFAULT(0),
    ReturnsAmount decimal(19,4) NOT NULL CONSTRAINT DF_Shifts_Returns DEFAULT(0),
    ExpectedCash decimal(19,4) NOT NULL CONSTRAINT DF_Shifts_Expected DEFAULT(0),
    ActualCash decimal(19,4) NULL,
    DifferenceAmount decimal(19,4) NULL,
    IsClosed bit NOT NULL CONSTRAINT DF_Shifts_Closed DEFAULT(0),
    CONSTRAINT FK_Shifts_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
    CONSTRAINT FK_Shifts_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID)
);
CREATE INDEX IX_Shifts_Open ON dbo.Shifts(BranchID,IsClosed,StartTime);

IF OBJECT_ID('dbo.PendingInvoices','U') IS NULL
CREATE TABLE dbo.PendingInvoices
(
    PendingInvoiceID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PendingInvoices PRIMARY KEY,
    HoldDate datetime2(0) NOT NULL CONSTRAINT DF_PendingInvoices_Date DEFAULT(SYSUTCDATETIME()),
    CustomerName nvarchar(200) NOT NULL,
    TotalAmount decimal(19,4) NOT NULL,
    ShiftID int NULL,
    CONSTRAINT FK_PendingInvoices_Shift FOREIGN KEY(ShiftID) REFERENCES dbo.Shifts(ShiftID)
);
IF OBJECT_ID('dbo.PendingInvoiceDetails','U') IS NULL
CREATE TABLE dbo.PendingInvoiceDetails
(
    PendingInvoiceDetailID bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PendingInvoiceDetails PRIMARY KEY,
    PendingInvoiceID bigint NOT NULL,
    ItemID int NOT NULL,
    Quantity decimal(19,4) NOT NULL,
    UnitPrice decimal(19,6) NOT NULL,
    CONSTRAINT FK_PendingInvoiceDetails_Header FOREIGN KEY(PendingInvoiceID) REFERENCES dbo.PendingInvoices(PendingInvoiceID) ON DELETE CASCADE,
    CONSTRAINT FK_PendingInvoiceDetails_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID)
);
GO
