/* Advanced ERP modules for the OnyxPro-aligned accounting system.
   Run after 01_OnyxAlignedSchema.sql on SQL Server. Idempotent and intentionally simple. */

/* Fixed assets */
IF OBJECT_ID('dbo.FA_AssetCategories','U') IS NULL
CREATE TABLE dbo.FA_AssetCategories(
 AssetCategoryID int IDENTITY PRIMARY KEY, CompanyID int NOT NULL, CategoryCode nvarchar(50) NOT NULL,
 CategoryNameAr nvarchar(200) NOT NULL, AssetAccountID int NULL, AccumulatedDepreciationAccountID int NULL,
 DepreciationExpenseAccountID int NULL, IsActive bit NOT NULL DEFAULT(1),
 CONSTRAINT UQ_FA_AssetCategories UNIQUE(CompanyID,CategoryCode),
 CONSTRAINT FK_FA_AssetCategories_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
 CONSTRAINT FK_FA_AssetCategories_Asset FOREIGN KEY(AssetAccountID) REFERENCES dbo.GL_Accounts(AccountID),
 CONSTRAINT FK_FA_AssetCategories_Accum FOREIGN KEY(AccumulatedDepreciationAccountID) REFERENCES dbo.GL_Accounts(AccountID),
 CONSTRAINT FK_FA_AssetCategories_Expense FOREIGN KEY(DepreciationExpenseAccountID) REFERENCES dbo.GL_Accounts(AccountID));
IF OBJECT_ID('dbo.FA_Assets','U') IS NULL
CREATE TABLE dbo.FA_Assets(
 AssetID bigint IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, AssetCategoryID int NOT NULL,
 AssetCode nvarchar(50) NOT NULL, AssetNameAr nvarchar(200) NOT NULL, SerialNo nvarchar(100) NULL,
 AcquisitionDate date NOT NULL, InServiceDate date NULL, AcquisitionCost decimal(19,4) NOT NULL,
 ResidualValue decimal(19,4) NOT NULL DEFAULT(0), DepreciationRate decimal(9,6) NOT NULL,
 DepreciationMethod tinyint NOT NULL DEFAULT(1), AccumulatedDepreciation decimal(19,4) NOT NULL DEFAULT(0),
 DisposalDate date NULL, IsActive bit NOT NULL DEFAULT(1), Notes nvarchar(500) NULL,
 CONSTRAINT UQ_FA_Assets UNIQUE(CompanyID,AssetCode),
 CONSTRAINT FK_FA_Assets_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
 CONSTRAINT FK_FA_Assets_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
 CONSTRAINT FK_FA_Assets_Category FOREIGN KEY(AssetCategoryID) REFERENCES dbo.FA_AssetCategories(AssetCategoryID),
 CONSTRAINT CK_FA_Assets_Amounts CHECK(AcquisitionCost>=0 AND ResidualValue>=0 AND ResidualValue<=AcquisitionCost),
 CONSTRAINT CK_FA_Assets_Rate CHECK(DepreciationRate BETWEEN 0 AND 100));
IF OBJECT_ID('dbo.FA_DepreciationEntries','U') IS NULL
CREATE TABLE dbo.FA_DepreciationEntries(
 DepreciationEntryID bigint IDENTITY PRIMARY KEY, AssetID bigint NOT NULL, FiscalPeriodID int NOT NULL,
 EntryDate date NOT NULL, DepreciationAmount decimal(19,4) NOT NULL, VoucherID bigint NULL,
 IsPosted bit NOT NULL DEFAULT(0), Notes nvarchar(500) NULL, CONSTRAINT UQ_FA_Depreciation UNIQUE(AssetID,FiscalPeriodID),
 CONSTRAINT FK_FA_Depreciation_Asset FOREIGN KEY(AssetID) REFERENCES dbo.FA_Assets(AssetID),
 CONSTRAINT FK_FA_Depreciation_Period FOREIGN KEY(FiscalPeriodID) REFERENCES dbo.GL_FiscalPeriods(FiscalPeriodID),
 CONSTRAINT FK_FA_Depreciation_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID));

/* Notes receivable/payable and intermediary accounts */
IF OBJECT_ID('dbo.GL_NotesReceivable','U') IS NULL
CREATE TABLE dbo.GL_NotesReceivable(
 NoteReceivableID bigint IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, CustomerID int NULL,
 NoteNo nvarchar(100) NOT NULL, DrawerName nvarchar(200) NULL, BankName nvarchar(200) NULL,
 IssueDate date NULL, DueDate date NOT NULL, Amount decimal(19,4) NOT NULL, CurrencyID int NOT NULL,
 Status tinyint NOT NULL DEFAULT(1), ReceivableAccountID int NULL, IntermediaryAccountID int NULL,
 BankAccountID int NULL, VoucherID bigint NULL, CollectedDate date NULL, Notes nvarchar(500) NULL,
 CONSTRAINT UQ_GL_NotesReceivable UNIQUE(CompanyID,NoteNo),
 CONSTRAINT FK_GL_NR_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
 CONSTRAINT FK_GL_NR_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
 CONSTRAINT FK_GL_NR_Customer FOREIGN KEY(CustomerID) REFERENCES dbo.Customers(CustomerID),
 CONSTRAINT FK_GL_NR_Currency FOREIGN KEY(CurrencyID) REFERENCES dbo.System_Currencies(CurrencyID),
 CONSTRAINT FK_GL_NR_Account FOREIGN KEY(ReceivableAccountID) REFERENCES dbo.GL_Accounts(AccountID),
 CONSTRAINT FK_GL_NR_Intermediary FOREIGN KEY(IntermediaryAccountID) REFERENCES dbo.GL_Accounts(AccountID),
 CONSTRAINT FK_GL_NR_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID), CONSTRAINT CK_GL_NR_Amount CHECK(Amount>0));
IF OBJECT_ID('dbo.GL_NotesPayable','U') IS NULL
CREATE TABLE dbo.GL_NotesPayable(
 NotePayableID bigint IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, SupplierID int NULL,
 NoteNo nvarchar(100) NOT NULL, PayeeName nvarchar(200) NULL, BankName nvarchar(200) NULL,
 IssueDate date NULL, DueDate date NOT NULL, Amount decimal(19,4) NOT NULL, CurrencyID int NOT NULL,
 Status tinyint NOT NULL DEFAULT(1), PayableAccountID int NULL, IntermediaryAccountID int NULL,
 BankAccountID int NULL, VoucherID bigint NULL, PaidDate date NULL, Notes nvarchar(500) NULL,
 CONSTRAINT UQ_GL_NotesPayable UNIQUE(CompanyID,NoteNo),
 CONSTRAINT FK_GL_NP_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID),
 CONSTRAINT FK_GL_NP_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID),
 CONSTRAINT FK_GL_NP_Supplier FOREIGN KEY(SupplierID) REFERENCES dbo.Suppliers(SupplierID),
 CONSTRAINT FK_GL_NP_Currency FOREIGN KEY(CurrencyID) REFERENCES dbo.System_Currencies(CurrencyID),
 CONSTRAINT FK_GL_NP_Account FOREIGN KEY(PayableAccountID) REFERENCES dbo.GL_Accounts(AccountID),
 CONSTRAINT FK_GL_NP_Intermediary FOREIGN KEY(IntermediaryAccountID) REFERENCES dbo.GL_Accounts(AccountID),
 CONSTRAINT FK_GL_NP_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID), CONSTRAINT CK_GL_NP_Amount CHECK(Amount>0));

/* HR, payroll, advances and custody */
IF OBJECT_ID('dbo.HR_Departments','U') IS NULL
CREATE TABLE dbo.HR_Departments(DepartmentID int IDENTITY PRIMARY KEY, CompanyID int NOT NULL, DepartmentCode nvarchar(50) NOT NULL, DepartmentNameAr nvarchar(200) NOT NULL, IsActive bit NOT NULL DEFAULT(1), CONSTRAINT UQ_HR_Departments UNIQUE(CompanyID,DepartmentCode), CONSTRAINT FK_HR_Departments_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID));
IF OBJECT_ID('dbo.HR_Employees','U') IS NULL
CREATE TABLE dbo.HR_Employees(EmployeeID int IDENTITY PRIMARY KEY, CompanyID int NOT NULL, EmployeeCode nvarchar(50) NOT NULL, FullNameAr nvarchar(200) NOT NULL, DepartmentID int NULL, JobTitle nvarchar(150) NULL, HireDate date NULL, BasicSalary decimal(19,4) NOT NULL DEFAULT(0), SalaryAccountID int NULL, AdvanceAccountID int NULL, IsActive bit NOT NULL DEFAULT(1), CONSTRAINT UQ_HR_Employees UNIQUE(CompanyID,EmployeeCode), CONSTRAINT FK_HR_Employees_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID), CONSTRAINT FK_HR_Employees_Department FOREIGN KEY(DepartmentID) REFERENCES dbo.HR_Departments(DepartmentID), CONSTRAINT FK_HR_Employees_Salary FOREIGN KEY(SalaryAccountID) REFERENCES dbo.GL_Accounts(AccountID), CONSTRAINT FK_HR_Employees_Advance FOREIGN KEY(AdvanceAccountID) REFERENCES dbo.GL_Accounts(AccountID));
IF OBJECT_ID('dbo.HR_SalariesHeader','U') IS NULL
CREATE TABLE dbo.HR_SalariesHeader(SalaryHeaderID bigint IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, FiscalPeriodID int NOT NULL, SalaryMonth date NOT NULL, TotalEarnings decimal(19,4) NOT NULL DEFAULT(0), TotalDeductions decimal(19,4) NOT NULL DEFAULT(0), NetAmount AS(TotalEarnings-TotalDeductions) PERSISTED, VoucherID bigint NULL, Status tinyint NOT NULL DEFAULT(1), CONSTRAINT UQ_HR_SalariesHeader UNIQUE(CompanyID,SalaryMonth), CONSTRAINT FK_HR_SH_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID), CONSTRAINT FK_HR_SH_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID), CONSTRAINT FK_HR_SH_Period FOREIGN KEY(FiscalPeriodID) REFERENCES dbo.GL_FiscalPeriods(FiscalPeriodID), CONSTRAINT FK_HR_SH_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID));
IF OBJECT_ID('dbo.HR_SalariesDetails','U') IS NULL
CREATE TABLE dbo.HR_SalariesDetails(SalaryDetailID bigint IDENTITY PRIMARY KEY, SalaryHeaderID bigint NOT NULL, EmployeeID int NOT NULL, BasicSalary decimal(19,4) NOT NULL DEFAULT(0), Allowances decimal(19,4) NOT NULL DEFAULT(0), AbsenceDeduction decimal(19,4) NOT NULL DEFAULT(0), AdvanceDeduction decimal(19,4) NOT NULL DEFAULT(0), CustodyDeduction decimal(19,4) NOT NULL DEFAULT(0), OtherDeductions decimal(19,4) NOT NULL DEFAULT(0), NetAmount AS(BasicSalary+Allowances-AbsenceDeduction-AdvanceDeduction-CustodyDeduction-OtherDeductions) PERSISTED, CONSTRAINT UQ_HR_SD UNIQUE(SalaryHeaderID,EmployeeID), CONSTRAINT FK_HR_SD_Header FOREIGN KEY(SalaryHeaderID) REFERENCES dbo.HR_SalariesHeader(SalaryHeaderID) ON DELETE CASCADE, CONSTRAINT FK_HR_SD_Employee FOREIGN KEY(EmployeeID) REFERENCES dbo.HR_Employees(EmployeeID));
IF OBJECT_ID('dbo.HR_Advances','U') IS NULL
CREATE TABLE dbo.HR_Advances(AdvanceID bigint IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, EmployeeID int NOT NULL, AdvanceType tinyint NOT NULL, RequestDate date NOT NULL, Amount decimal(19,4) NOT NULL, SettledAmount decimal(19,4) NOT NULL DEFAULT(0), RemainingAmount AS(Amount-SettledAmount) PERSISTED, Status tinyint NOT NULL DEFAULT(1), VoucherID bigint NULL, Notes nvarchar(500) NULL, CONSTRAINT FK_HR_Advances_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID), CONSTRAINT FK_HR_Advances_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID), CONSTRAINT FK_HR_Advances_Employee FOREIGN KEY(EmployeeID) REFERENCES dbo.HR_Employees(EmployeeID), CONSTRAINT FK_HR_Advances_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID), CONSTRAINT CK_HR_Advances_Amounts CHECK(Amount>0 AND SettledAmount BETWEEN 0 AND Amount));

/* Bank accounts and reconciliation */
IF OBJECT_ID('dbo.Bank_Accounts','U') IS NULL
CREATE TABLE dbo.Bank_Accounts(BankAccountID int IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, BankCode nvarchar(50) NOT NULL, BankNameAr nvarchar(200) NOT NULL, AccountNo nvarchar(100) NULL, IBAN nvarchar(100) NULL, CurrencyID int NOT NULL, GLAccountID int NOT NULL, IntermediaryReceivableAccountID int NULL, IntermediaryPayableAccountID int NULL, IsActive bit NOT NULL DEFAULT(1), CONSTRAINT UQ_Bank_Accounts UNIQUE(CompanyID,BankCode), CONSTRAINT FK_Bank_Accounts_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID), CONSTRAINT FK_Bank_Accounts_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID), CONSTRAINT FK_Bank_Accounts_Currency FOREIGN KEY(CurrencyID) REFERENCES dbo.System_Currencies(CurrencyID), CONSTRAINT FK_Bank_Accounts_GL FOREIGN KEY(GLAccountID) REFERENCES dbo.GL_Accounts(AccountID), CONSTRAINT FK_Bank_Accounts_Recv FOREIGN KEY(IntermediaryReceivableAccountID) REFERENCES dbo.GL_Accounts(AccountID), CONSTRAINT FK_Bank_Accounts_Pay FOREIGN KEY(IntermediaryPayableAccountID) REFERENCES dbo.GL_Accounts(AccountID));
IF OBJECT_ID('dbo.Bank_Reconciliation','U') IS NULL
CREATE TABLE dbo.Bank_Reconciliation(ReconciliationID bigint IDENTITY PRIMARY KEY, BankAccountID int NOT NULL, StatementDate date NOT NULL, StatementReference nvarchar(100) NULL, OpeningBalance decimal(19,4) NOT NULL, ClosingBalance decimal(19,4) NOT NULL, BookBalance decimal(19,4) NOT NULL, DifferenceAmount AS(ClosingBalance-BookBalance) PERSISTED, Status tinyint NOT NULL DEFAULT(1), PreparedBy int NULL, ApprovedAt datetime2(0) NULL, Notes nvarchar(500) NULL, CONSTRAINT FK_Bank_Reconciliation_Account FOREIGN KEY(BankAccountID) REFERENCES dbo.Bank_Accounts(BankAccountID));
IF OBJECT_ID('dbo.Bank_ReconciliationLines','U') IS NULL
CREATE TABLE dbo.Bank_ReconciliationLines(ReconciliationLineID bigint IDENTITY PRIMARY KEY, ReconciliationID bigint NOT NULL, TransactionDate date NOT NULL, Description nvarchar(300) NULL, ReferenceNo nvarchar(100) NULL, Amount decimal(19,4) NOT NULL, LineType tinyint NOT NULL, VoucherID bigint NULL, IsMatched bit NOT NULL DEFAULT(0), CONSTRAINT FK_Bank_RL_Header FOREIGN KEY(ReconciliationID) REFERENCES dbo.Bank_Reconciliation(ReconciliationID) ON DELETE CASCADE, CONSTRAINT FK_Bank_RL_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID));

/* Import / letters of credit and landed-cost allocation */
IF OBJECT_ID('dbo.SC_LettersOfCredit','U') IS NULL
CREATE TABLE dbo.SC_LettersOfCredit(LetterOfCreditID bigint IDENTITY PRIMARY KEY, CompanyID int NOT NULL, BranchID int NOT NULL, SupplierID int NULL, LCNumber nvarchar(100) NOT NULL, OpeningDate date NOT NULL, ExpectedArrivalDate date NULL, CurrencyID int NOT NULL, LCAmount decimal(19,4) NOT NULL, BankAccountID int NULL, Status tinyint NOT NULL DEFAULT(1), VoucherID bigint NULL, Notes nvarchar(500) NULL, CONSTRAINT UQ_SC_LC UNIQUE(CompanyID,LCNumber), CONSTRAINT FK_SC_LC_Company FOREIGN KEY(CompanyID) REFERENCES dbo.System_Companies(CompanyID), CONSTRAINT FK_SC_LC_Branch FOREIGN KEY(BranchID) REFERENCES dbo.System_Branches(BranchID), CONSTRAINT FK_SC_LC_Supplier FOREIGN KEY(SupplierID) REFERENCES dbo.Suppliers(SupplierID), CONSTRAINT FK_SC_LC_Currency FOREIGN KEY(CurrencyID) REFERENCES dbo.System_Currencies(CurrencyID), CONSTRAINT FK_SC_LC_Bank FOREIGN KEY(BankAccountID) REFERENCES dbo.Bank_Accounts(BankAccountID), CONSTRAINT FK_SC_LC_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID), CONSTRAINT CK_SC_LC_Amount CHECK(LCAmount>0));
IF OBJECT_ID('dbo.SC_ImportExpenses','U') IS NULL
CREATE TABLE dbo.SC_ImportExpenses(ImportExpenseID bigint IDENTITY PRIMARY KEY, LetterOfCreditID bigint NOT NULL, ExpenseDate date NOT NULL, ExpenseType tinyint NOT NULL, Description nvarchar(300) NOT NULL, Amount decimal(19,4) NOT NULL, CurrencyID int NOT NULL, AllocationMethod tinyint NOT NULL DEFAULT(1), VoucherID bigint NULL, IsAllocated bit NOT NULL DEFAULT(0), Notes nvarchar(500) NULL, CONSTRAINT FK_SC_IE_LC FOREIGN KEY(LetterOfCreditID) REFERENCES dbo.SC_LettersOfCredit(LetterOfCreditID) ON DELETE CASCADE, CONSTRAINT FK_SC_IE_Currency FOREIGN KEY(CurrencyID) REFERENCES dbo.System_Currencies(CurrencyID), CONSTRAINT FK_SC_IE_Voucher FOREIGN KEY(VoucherID) REFERENCES dbo.GL_VouchersHeader(VoucherID));
IF OBJECT_ID('dbo.SC_ImportExpenseAllocations','U') IS NULL
CREATE TABLE dbo.SC_ImportExpenseAllocations(AllocationID bigint IDENTITY PRIMARY KEY, ImportExpenseID bigint NOT NULL, PurchaseInvoiceDetailID bigint NULL, ItemID int NOT NULL, AllocationAmount decimal(19,4) NOT NULL, CONSTRAINT FK_SC_IA_Expense FOREIGN KEY(ImportExpenseID) REFERENCES dbo.SC_ImportExpenses(ImportExpenseID) ON DELETE CASCADE, CONSTRAINT FK_SC_IA_Detail FOREIGN KEY(PurchaseInvoiceDetailID) REFERENCES dbo.PurchaseInvoiceDetails(PurchaseInvoiceDetailID), CONSTRAINT FK_SC_IA_Item FOREIGN KEY(ItemID) REFERENCES dbo.Items(ItemID));

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_FA_Assets_Status' AND object_id=OBJECT_ID(N'dbo.FA_Assets')) CREATE INDEX IX_FA_Assets_Status ON dbo.FA_Assets(CompanyID,IsActive);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_GL_NR_Due' AND object_id=OBJECT_ID(N'dbo.GL_NotesReceivable')) CREATE INDEX IX_GL_NR_Due ON dbo.GL_NotesReceivable(CompanyID,Status,DueDate);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_GL_NP_Due' AND object_id=OBJECT_ID(N'dbo.GL_NotesPayable')) CREATE INDEX IX_GL_NP_Due ON dbo.GL_NotesPayable(CompanyID,Status,DueDate);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_HR_Advances_Employee' AND object_id=OBJECT_ID(N'dbo.HR_Advances')) CREATE INDEX IX_HR_Advances_Employee ON dbo.HR_Advances(EmployeeID,Status);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Bank_Reconciliation' AND object_id=OBJECT_ID(N'dbo.Bank_Reconciliation')) CREATE INDEX IX_Bank_Reconciliation ON dbo.Bank_Reconciliation(BankAccountID,StatementDate);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_SC_ImportExpenses' AND object_id=OBJECT_ID(N'dbo.SC_ImportExpenses')) CREATE INDEX IX_SC_ImportExpenses ON dbo.SC_ImportExpenses(LetterOfCreditID,IsAllocated);
GO
