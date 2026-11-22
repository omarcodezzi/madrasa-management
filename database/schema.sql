IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AcademicSessions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [IsCurrent] bit NOT NULL,
        CONSTRAINT [PK_AcademicSessions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AppSettings] (
        [Key] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Key])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [PhotoPath] nvarchar(max) NULL,
        [PreferredLanguage] nvarchar(max) NOT NULL,
        [StaffId] int NULL,
        [GuardianId] int NULL,
        [StudentId] int NULL,
        [IsActive] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(max) NULL,
        [Action] nvarchar(max) NOT NULL,
        [Entity] nvarchar(max) NOT NULL,
        [EntityId] nvarchar(max) NULL,
        [Changes] nvarchar(max) NULL,
        [At] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Books] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [TitleAr] nvarchar(max) NULL,
        [Author] nvarchar(max) NULL,
        [Language] tinyint NOT NULL,
        [Category] nvarchar(max) NULL,
        [ShelfNo] nvarchar(max) NULL,
        [TotalCopies] int NOT NULL,
        CONSTRAINT [PK_Books] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [NameAr] nvarchar(max) NULL,
        [Kind] tinyint NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Donors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        CONSTRAINT [PK_Donors] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [Kind] tinyint NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Exams] (
        [Id] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [StartDate] date NULL,
        [IsPublished] bit NOT NULL,
        CONSTRAINT [PK_Exams] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [FeeHeads] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [Frequency] tinyint NOT NULL,
        [DefaultAmount] decimal(12,2) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_FeeHeads] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Funds] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [Code] nvarchar(450) NOT NULL,
        [IsRestricted] bit NOT NULL,
        CONSTRAINT [PK_Funds] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [GradeRules] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [NameAr] nvarchar(max) NULL,
        [MinAverage] decimal(12,2) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_GradeRules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Guardians] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Relation] nvarchar(max) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [AltPhone] nvarchar(max) NULL,
        [Occupation] nvarchar(max) NULL,
        [Nid] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        CONSTRAINT [PK_Guardians] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [HifzDailyLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [Date] date NOT NULL,
        [SabakJuz] tinyint NULL,
        [SabakFromPage] smallint NULL,
        [SabakToPage] smallint NULL,
        [SabakText] nvarchar(max) NULL,
        [SabqiText] nvarchar(max) NULL,
        [ManzilText] nvarchar(max) NULL,
        [Mistakes] int NOT NULL,
        [Quality] tinyint NULL,
        [Remarks] nvarchar(max) NULL,
        [TeacherId] int NULL,
        CONSTRAINT [PK_HifzDailyLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [HifzJuzProgress] (
        [StudentId] int NOT NULL,
        [JuzNo] tinyint NOT NULL,
        [Status] tinyint NOT NULL,
        [CompletedOn] date NULL,
        CONSTRAINT [PK_HifzJuzProgress] PRIMARY KEY ([StudentId], [JuzNo]),
        CONSTRAINT [CK_Juz] CHECK ([JuzNo] BETWEEN 1 AND 30)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Holidays] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [HostelLeaves] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NULL,
        [Reason] nvarchar(max) NULL,
        [TakenBy] nvarchar(max) NULL,
        [ReturnedOn] date NULL,
        CONSTRAINT [PK_HostelLeaves] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [HostelRooms] (
        [Id] int NOT NULL IDENTITY,
        [Building] nvarchar(max) NULL,
        [RoomNo] nvarchar(max) NOT NULL,
        [Seats] int NOT NULL,
        [ForGender] tinyint NOT NULL,
        CONSTRAINT [PK_HostelRooms] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [LedgerCategories] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [Kind] tinyint NOT NULL,
        CONSTRAINT [PK_LedgerCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [MadrasaProfile] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [NameEn] nvarchar(max) NULL,
        [NameAr] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [LogoPath] nvarchar(max) NULL,
        [EstablishedYear] int NULL,
        [StudentIdPrefix] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_MadrasaProfile] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [MessageLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [Channel] tinyint NOT NULL,
        [ToPhone] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Status] tinyint NOT NULL,
        [SentAt] datetime2 NULL,
        [RelatedNoticeId] int NULL,
        CONSTRAINT [PK_MessageLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Notices] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Date] date NOT NULL,
        [Audience] tinyint NOT NULL,
        [IsPublic] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Notices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Staff] (
        [Id] int NOT NULL IDENTITY,
        [StaffCode] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Designation] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Nid] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [Qualification] nvarchar(max) NULL,
        [JoinDate] date NULL,
        [BasicSalary] decimal(12,2) NULL,
        [PhotoPath] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentDiscounts] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [FeeHeadId] int NOT NULL,
        [Kind] tinyint NOT NULL,
        [Value] decimal(12,2) NOT NULL,
        [Reason] nvarchar(max) NULL,
        [SponsorFundId] int NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        CONSTRAINT [PK_StudentDiscounts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentDocuments] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StudentDocuments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [Id] int NOT NULL IDENTITY,
        [StudentCode] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [NameAr] nvarchar(max) NULL,
        [Gender] tinyint NOT NULL,
        [DateOfBirth] date NULL,
        [BirthRegNo] nvarchar(max) NULL,
        [BloodGroup] nvarchar(max) NULL,
        [FatherName] nvarchar(max) NULL,
        [MotherName] nvarchar(max) NULL,
        [Phone] nvarchar(30) NULL,
        [PresentAddress] nvarchar(max) NULL,
        [PermanentAddress] nvarchar(max) NULL,
        [PhotoPath] nvarchar(max) NULL,
        [AdmissionDate] date NOT NULL,
        [IsOrphan] bit NOT NULL,
        [IsResidential] bit NOT NULL,
        [Status] tinyint NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Subjects] (
        [Id] int NOT NULL IDENTITY,
        [NameBn] nvarchar(max) NOT NULL,
        [NameAr] nvarchar(max) NULL,
        [Code] nvarchar(max) NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [BookIssues] (
        [Id] int NOT NULL IDENTITY,
        [BookId] int NOT NULL,
        [StudentId] int NULL,
        [StaffId] int NULL,
        [IssuedOn] date NOT NULL,
        [DueOn] date NOT NULL,
        [ReturnedOn] date NULL,
        [Fine] decimal(12,2) NOT NULL,
        CONSTRAINT [PK_BookIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookIssues_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Classes] (
        [Id] int NOT NULL IDENTITY,
        [DepartmentId] int NOT NULL,
        [NameBn] nvarchar(max) NOT NULL,
        [NameAr] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [MonthlyFee] decimal(12,2) NULL,
        CONSTRAINT [PK_Classes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Classes_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [FeeInvoices] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [SessionId] int NOT NULL,
        [FeeHeadId] int NOT NULL,
        [Month] nvarchar(450) NULL,
        [Amount] decimal(12,2) NOT NULL,
        [Discount] decimal(12,2) NOT NULL,
        [DueDate] date NULL,
        CONSTRAINT [PK_FeeInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeeInvoices_FeeHeads_FeeHeadId] FOREIGN KEY ([FeeHeadId]) REFERENCES [FeeHeads] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [ExamResults] (
        [ExamId] int NOT NULL,
        [StudentId] int NOT NULL,
        [Total] decimal(12,2) NOT NULL,
        [Average] decimal(12,2) NOT NULL,
        [GradeRuleId] int NULL,
        [FailedSubjects] int NOT NULL,
        [MeritPosition] int NULL,
        CONSTRAINT [PK_ExamResults] PRIMARY KEY ([ExamId], [StudentId]),
        CONSTRAINT [FK_ExamResults_GradeRules_GradeRuleId] FOREIGN KEY ([GradeRuleId]) REFERENCES [GradeRules] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [HostelAllocations] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [RoomId] int NOT NULL,
        [SeatNo] int NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NULL,
        CONSTRAINT [PK_HostelAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HostelAllocations_HostelRooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [HostelRooms] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [LedgerEntries] (
        [Id] bigint NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [FundId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Direction] tinyint NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [Description] nvarchar(max) NULL,
        [VoucherNo] nvarchar(max) NULL,
        [DonorId] int NULL,
        [FeeReceiptId] int NULL,
        [SalaryPaymentId] int NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LedgerEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Ledger_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_LedgerEntries_Funds_FundId] FOREIGN KEY ([FundId]) REFERENCES [Funds] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LedgerEntries_LedgerCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [LedgerCategories] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [SalaryPayments] (
        [Id] int NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [Month] nvarchar(450) NOT NULL,
        [Basic] decimal(12,2) NOT NULL,
        [Allowance] decimal(12,2) NOT NULL,
        [Deduction] decimal(12,2) NOT NULL,
        [NetPaid] decimal(12,2) NOT NULL,
        [PaidOn] date NOT NULL,
        [FundId] int NULL,
        CONSTRAINT [PK_SalaryPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryPayments_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [StaffAttendance] (
        [Id] bigint NOT NULL IDENTITY,
        [StaffId] int NOT NULL,
        [Date] date NOT NULL,
        [Status] tinyint NOT NULL,
        [CheckIn] time NULL,
        CONSTRAINT [PK_StaffAttendance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StaffAttendance_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [FeeReceipts] (
        [Id] int NOT NULL IDENTITY,
        [ReceiptNo] int NOT NULL,
        [StudentId] int NOT NULL,
        [Date] date NOT NULL,
        [TotalPaid] decimal(12,2) NOT NULL,
        [Method] tinyint NOT NULL,
        [Reference] nvarchar(max) NULL,
        [CollectedBy] nvarchar(max) NULL,
        [IsVoided] bit NOT NULL,
        CONSTRAINT [PK_FeeReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeeReceipts_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentAttendance] (
        [Id] bigint NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [Date] date NOT NULL,
        [Status] tinyint NOT NULL,
        [CheckIn] time NULL,
        [Method] tinyint NOT NULL,
        [MarkedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_StudentAttendance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StudentAttendance_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [StudentGuardians] (
        [StudentId] int NOT NULL,
        [GuardianId] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        CONSTRAINT [PK_StudentGuardians] PRIMARY KEY ([StudentId], [GuardianId]),
        CONSTRAINT [FK_StudentGuardians_Guardians_GuardianId] FOREIGN KEY ([GuardianId]) REFERENCES [Guardians] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StudentGuardians_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [AdmissionApplications] (
        [Id] int NOT NULL IDENTITY,
        [ApplicationNo] nvarchar(450) NOT NULL,
        [SessionId] int NOT NULL,
        [ClassId] int NOT NULL,
        [ApplicantName] nvarchar(max) NOT NULL,
        [Gender] tinyint NOT NULL,
        [DateOfBirth] date NULL,
        [FatherName] nvarchar(max) NULL,
        [GuardianPhone] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NULL,
        [PreviousInstitution] nvarchar(max) NULL,
        [PhotoPath] nvarchar(max) NULL,
        [Source] tinyint NOT NULL,
        [Stage] tinyint NOT NULL,
        [TestMarks] decimal(12,2) NULL,
        [Remarks] nvarchar(max) NULL,
        [StudentId] int NULL,
        [AppliedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AdmissionApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AdmissionApplications_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [ClassSubjects] (
        [Id] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [ClassId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [TeacherId] int NULL,
        [FullMarks] int NOT NULL,
        [PassMarks] int NOT NULL,
        CONSTRAINT [PK_ClassSubjects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClassSubjects_AcademicSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AcademicSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClassSubjects_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClassSubjects_Staff_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Staff] ([Id]),
        CONSTRAINT [FK_ClassSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Sections] (
        [Id] int NOT NULL IDENTITY,
        [ClassId] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Capacity] int NULL,
        CONSTRAINT [PK_Sections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sections_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [FeeReceiptLines] (
        [Id] int NOT NULL IDENTITY,
        [ReceiptId] int NOT NULL,
        [InvoiceId] int NULL,
        [FeeHeadId] int NOT NULL,
        [Month] nvarchar(max) NULL,
        [Amount] decimal(12,2) NOT NULL,
        CONSTRAINT [PK_FeeReceiptLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeeReceiptLines_FeeHeads_FeeHeadId] FOREIGN KEY ([FeeHeadId]) REFERENCES [FeeHeads] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FeeReceiptLines_FeeReceipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [FeeReceipts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [ExamMarks] (
        [Id] bigint NOT NULL IDENTITY,
        [ExamId] int NOT NULL,
        [StudentId] int NOT NULL,
        [ClassSubjectId] int NOT NULL,
        [Marks] decimal(12,2) NULL,
        [IsAbsent] bit NOT NULL,
        CONSTRAINT [PK_ExamMarks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExamMarks_ClassSubjects_ClassSubjectId] FOREIGN KEY ([ClassSubjectId]) REFERENCES [ClassSubjects] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [Enrollments] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] int NOT NULL,
        [SessionId] int NOT NULL,
        [ClassId] int NOT NULL,
        [SectionId] int NULL,
        [Roll] int NOT NULL,
        [MonthlyFeeOverride] decimal(12,2) NULL,
        CONSTRAINT [PK_Enrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Enrollments_AcademicSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AcademicSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Enrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE TABLE [RoutineSlots] (
        [Id] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [SectionId] int NOT NULL,
        [DayOfWeek] tinyint NOT NULL,
        [PeriodNo] tinyint NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [ClassSubjectId] int NOT NULL,
        [Room] nvarchar(max) NULL,
        CONSTRAINT [PK_RoutineSlots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoutineSlots_ClassSubjects_ClassSubjectId] FOREIGN KEY ([ClassSubjectId]) REFERENCES [ClassSubjects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RoutineSlots_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'EndDate', N'IsCurrent', N'Name', N'StartDate') AND [object_id] = OBJECT_ID(N'[AcademicSessions]'))
        SET IDENTITY_INSERT [AcademicSessions] ON;
    EXEC(N'INSERT INTO [AcademicSessions] ([Id], [EndDate], [IsCurrent], [Name], [StartDate])
    VALUES (1, ''2026-12-31'', CAST(1 AS bit), N''2026'', ''2026-01-01'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'EndDate', N'IsCurrent', N'Name', N'StartDate') AND [object_id] = OBJECT_ID(N'[AcademicSessions]'))
        SET IDENTITY_INSERT [AcademicSessions] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[AppSettings]'))
        SET IDENTITY_INSERT [AppSettings] ON;
    EXEC(N'INSERT INTO [AppSettings] ([Key], [Value])
    VALUES (N''digits'', N''bn''),
    (N''language'', N''bn''),
    (N''weeklyHoliday'', N''5'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[AppSettings]'))
        SET IDENTITY_INSERT [AppSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Kind', N'NameAr', N'NameBn', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] ON;
    EXEC(N'INSERT INTO [Departments] ([Id], [Kind], [NameAr], [NameBn], [SortOrder])
    VALUES (1, CAST(1 AS tinyint), NULL, N''নূরানী বিভাগ'', 1),
    (2, CAST(2 AS tinyint), NULL, N''নাজেরা বিভাগ'', 2),
    (3, CAST(3 AS tinyint), N''قسم تحفيظ القرآن'', N''হিফজ বিভাগ'', 3),
    (4, CAST(4 AS tinyint), NULL, N''কিতাব বিভাগ'', 4)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Kind', N'NameAr', N'NameBn', N'SortOrder') AND [object_id] = OBJECT_ID(N'[Departments]'))
        SET IDENTITY_INSERT [Departments] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultAmount', N'Frequency', N'IsActive', N'NameBn') AND [object_id] = OBJECT_ID(N'[FeeHeads]'))
        SET IDENTITY_INSERT [FeeHeads] ON;
    EXEC(N'INSERT INTO [FeeHeads] ([Id], [DefaultAmount], [Frequency], [IsActive], [NameBn])
    VALUES (1, 1000.0, CAST(1 AS tinyint), CAST(1 AS bit), N''ভর্তি ফি''),
    (2, 500.0, CAST(2 AS tinyint), CAST(1 AS bit), N''মাসিক বেতন''),
    (3, 200.0, CAST(3 AS tinyint), CAST(1 AS bit), N''পরীক্ষা ফি''),
    (4, 2500.0, CAST(2 AS tinyint), CAST(1 AS bit), N''বোর্ডিং ফি'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DefaultAmount', N'Frequency', N'IsActive', N'NameBn') AND [object_id] = OBJECT_ID(N'[FeeHeads]'))
        SET IDENTITY_INSERT [FeeHeads] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsRestricted', N'NameBn') AND [object_id] = OBJECT_ID(N'[Funds]'))
        SET IDENTITY_INSERT [Funds] ON;
    EXEC(N'INSERT INTO [Funds] ([Id], [Code], [IsRestricted], [NameBn])
    VALUES (1, N''GENERAL'', CAST(0 AS bit), N''সাধারণ ফান্ড''),
    (2, N''ZAKAT'', CAST(1 AS bit), N''যাকাত ফান্ড''),
    (3, N''SADAQAH'', CAST(1 AS bit), N''সদকা ফান্ড''),
    (4, N''LILLAH'', CAST(1 AS bit), N''লিল্লাহ বোর্ডিং ফান্ড'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsRestricted', N'NameBn') AND [object_id] = OBJECT_ID(N'[Funds]'))
        SET IDENTITY_INSERT [Funds] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'MinAverage', N'NameAr', N'NameBn', N'SortOrder') AND [object_id] = OBJECT_ID(N'[GradeRules]'))
        SET IDENTITY_INSERT [GradeRules] ON;
    EXEC(N'INSERT INTO [GradeRules] ([Id], [MinAverage], [NameAr], [NameBn], [SortOrder])
    VALUES (1, 80.0, N''ممتاز'', N''মুমতায'', 1),
    (2, 65.0, N''جيد جدا'', N''জায়্যিদ জিদ্দান'', 2),
    (3, 50.0, N''جيد'', N''জায়্যিদ'', 3),
    (4, 33.0, N''مقبول'', N''মাকবুল'', 4),
    (5, 0.0, N''راسب'', N''রাসিব'', 5)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'MinAverage', N'NameAr', N'NameBn', N'SortOrder') AND [object_id] = OBJECT_ID(N'[GradeRules]'))
        SET IDENTITY_INSERT [GradeRules] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Kind', N'NameBn') AND [object_id] = OBJECT_ID(N'[LedgerCategories]'))
        SET IDENTITY_INSERT [LedgerCategories] ON;
    EXEC(N'INSERT INTO [LedgerCategories] ([Id], [Kind], [NameBn])
    VALUES (1, CAST(1 AS tinyint), N''ছাত্র বেতন ও ফি''),
    (2, CAST(1 AS tinyint), N''দান / অনুদান''),
    (3, CAST(2 AS tinyint), N''শিক্ষক-কর্মচারী বেতন''),
    (4, CAST(2 AS tinyint), N''বোর্ডিং ও খাবার''),
    (5, CAST(2 AS tinyint), N''বিদ্যুৎ ও ইউটিলিটি''),
    (6, CAST(2 AS tinyint), N''নির্মাণ ও মেরামত''),
    (7, CAST(2 AS tinyint), N''অন্যান্য'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Kind', N'NameBn') AND [object_id] = OBJECT_ID(N'[LedgerCategories]'))
        SET IDENTITY_INSERT [LedgerCategories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Email', N'EstablishedYear', N'LogoPath', N'NameAr', N'NameBn', N'NameEn', N'Phone', N'StudentIdPrefix') AND [object_id] = OBJECT_ID(N'[MadrasaProfile]'))
        SET IDENTITY_INSERT [MadrasaProfile] ON;
    EXEC(N'INSERT INTO [MadrasaProfile] ([Id], [Address], [Email], [EstablishedYear], [LogoPath], [NameAr], [NameBn], [NameEn], [Phone], [StudentIdPrefix])
    VALUES (1, N''রামারবাগ, লাকসাম, কুমিল্লা'', NULL, NULL, NULL, N''رماربغ تعمير القرآن مدرسة'', N''রামারবাগ তা''''মীরুল কুরআন মাদরাসা'', NULL, NULL, N''RM'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Email', N'EstablishedYear', N'LogoPath', N'NameAr', N'NameBn', N'NameEn', N'Phone', N'StudentIdPrefix') AND [object_id] = OBJECT_ID(N'[MadrasaProfile]'))
        SET IDENTITY_INSERT [MadrasaProfile] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AdmissionApplications_ApplicationNo] ON [AdmissionApplications] ([ApplicationNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AdmissionApplications_ClassId] ON [AdmissionApplications] ([ClassId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BookIssues_BookId] ON [BookIssues] ([BookId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Classes_DepartmentId] ON [Classes] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClassSubjects_ClassId] ON [ClassSubjects] ([ClassId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClassSubjects_SessionId_ClassId_SubjectId] ON [ClassSubjects] ([SessionId], [ClassId], [SubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClassSubjects_SubjectId] ON [ClassSubjects] ([SubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClassSubjects_TeacherId] ON [ClassSubjects] ([TeacherId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_ClassId] ON [Enrollments] ([ClassId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Enrollments_SectionId] ON [Enrollments] ([SectionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Enrollments_SessionId_ClassId_SectionId_Roll] ON [Enrollments] ([SessionId], [ClassId], [SectionId], [Roll]) WHERE [SectionId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Enrollments_StudentId_SessionId] ON [Enrollments] ([StudentId], [SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExamMarks_ClassSubjectId] ON [ExamMarks] ([ClassSubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExamMarks_ExamId_StudentId_ClassSubjectId] ON [ExamMarks] ([ExamId], [StudentId], [ClassSubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ExamResults_GradeRuleId] ON [ExamResults] ([GradeRuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FeeInvoices_FeeHeadId] ON [FeeInvoices] ([FeeHeadId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FeeInvoices_StudentId_FeeHeadId_Month] ON [FeeInvoices] ([StudentId], [FeeHeadId], [Month]) WHERE [Month] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FeeReceiptLines_FeeHeadId] ON [FeeReceiptLines] ([FeeHeadId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FeeReceiptLines_ReceiptId] ON [FeeReceiptLines] ([ReceiptId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FeeReceipts_ReceiptNo] ON [FeeReceipts] ([ReceiptNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FeeReceipts_StudentId] ON [FeeReceipts] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Funds_Code] ON [Funds] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Guardians_Phone] ON [Guardians] ([Phone]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HifzDailyLogs_StudentId_Date] ON [HifzDailyLogs] ([StudentId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Holidays_Date] ON [Holidays] ([Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HostelAllocations_RoomId] ON [HostelAllocations] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LedgerEntries_CategoryId] ON [LedgerEntries] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LedgerEntries_FundId_Date] ON [LedgerEntries] ([FundId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoutineSlots_ClassSubjectId] ON [RoutineSlots] ([ClassSubjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoutineSlots_SectionId] ON [RoutineSlots] ([SectionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SalaryPayments_StaffId_Month] ON [SalaryPayments] ([StaffId], [Month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sections_ClassId] ON [Sections] ([ClassId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Staff_StaffCode] ON [Staff] ([StaffCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaffAttendance_StaffId_Date] ON [StaffAttendance] ([StaffId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentAttendance_Date] ON [StudentAttendance] ([Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StudentAttendance_StudentId_Date] ON [StudentAttendance] ([StudentId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StudentGuardians_GuardianId] ON [StudentGuardians] ([GuardianId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_Name] ON [Students] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_Phone] ON [Students] ([Phone]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Students_StudentCode] ON [Students] ([StudentCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121191250_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121191250_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121204226_SeedProfileContact'
)
BEGIN
    EXEC(N'UPDATE [MadrasaProfile] SET [Address] = N''রামারবাগ কেন্দ্রিয় ঈদগা, রামারবাগ, উত্তরদা, লাকসাম, কুমিল্লা'', [Phone] = N''01975448094''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121204226_SeedProfileContact'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121204226_SeedProfileContact', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    DROP INDEX [IX_FeeReceipts_StudentId] ON [FeeReceipts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_StudentDocuments_StudentId] ON [StudentDocuments] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_StudentDiscounts_FeeHeadId] ON [StudentDiscounts] ([FeeHeadId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_StudentDiscounts_SponsorFundId] ON [StudentDiscounts] ([SponsorFundId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_StudentDiscounts_StudentId] ON [StudentDiscounts] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_SalaryPayments_FundId] ON [SalaryPayments] ([FundId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_RoutineSlots_SessionId] ON [RoutineSlots] ([SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_MessageLogs_RelatedNoticeId] ON [MessageLogs] ([RelatedNoticeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_LedgerEntries_DonorId] ON [LedgerEntries] ([DonorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_LedgerEntries_FeeReceiptId] ON [LedgerEntries] ([FeeReceiptId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_LedgerEntries_SalaryPaymentId] ON [LedgerEntries] ([SalaryPaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_HostelLeaves_StudentId] ON [HostelLeaves] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_HostelAllocations_StudentId] ON [HostelAllocations] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_HifzDailyLogs_TeacherId] ON [HifzDailyLogs] ([TeacherId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_FeeReceipts_StudentId_Date] ON [FeeReceipts] ([StudentId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_FeeReceiptLines_InvoiceId] ON [FeeReceiptLines] ([InvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_FeeInvoices_Month] ON [FeeInvoices] ([Month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_FeeInvoices_SessionId] ON [FeeInvoices] ([SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_Exams_SessionId] ON [Exams] ([SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_ExamResults_StudentId] ON [ExamResults] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_ExamMarks_StudentId] ON [ExamMarks] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_BookIssues_StaffId] ON [BookIssues] ([StaffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_BookIssues_StudentId] ON [BookIssues] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_AdmissionApplications_SessionId] ON [AdmissionApplications] ([SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    CREATE INDEX [IX_AdmissionApplications_StudentId] ON [AdmissionApplications] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [AdmissionApplications] ADD CONSTRAINT [FK_AdmissionApplications_AcademicSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AcademicSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [AdmissionApplications] ADD CONSTRAINT [FK_AdmissionApplications_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [BookIssues] ADD CONSTRAINT [FK_BookIssues_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [BookIssues] ADD CONSTRAINT [FK_BookIssues_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [ExamMarks] ADD CONSTRAINT [FK_ExamMarks_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [ExamMarks] ADD CONSTRAINT [FK_ExamMarks_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [ExamResults] ADD CONSTRAINT [FK_ExamResults_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [ExamResults] ADD CONSTRAINT [FK_ExamResults_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [Exams] ADD CONSTRAINT [FK_Exams_AcademicSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AcademicSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [FeeInvoices] ADD CONSTRAINT [FK_FeeInvoices_AcademicSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AcademicSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [FeeInvoices] ADD CONSTRAINT [FK_FeeInvoices_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [FeeReceiptLines] ADD CONSTRAINT [FK_FeeReceiptLines_FeeInvoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [FeeInvoices] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [HifzDailyLogs] ADD CONSTRAINT [FK_HifzDailyLogs_Staff_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Staff] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [HifzDailyLogs] ADD CONSTRAINT [FK_HifzDailyLogs_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [HifzJuzProgress] ADD CONSTRAINT [FK_HifzJuzProgress_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [HostelAllocations] ADD CONSTRAINT [FK_HostelAllocations_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [HostelLeaves] ADD CONSTRAINT [FK_HostelLeaves_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [LedgerEntries] ADD CONSTRAINT [FK_LedgerEntries_Donors_DonorId] FOREIGN KEY ([DonorId]) REFERENCES [Donors] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [LedgerEntries] ADD CONSTRAINT [FK_LedgerEntries_FeeReceipts_FeeReceiptId] FOREIGN KEY ([FeeReceiptId]) REFERENCES [FeeReceipts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [LedgerEntries] ADD CONSTRAINT [FK_LedgerEntries_SalaryPayments_SalaryPaymentId] FOREIGN KEY ([SalaryPaymentId]) REFERENCES [SalaryPayments] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [MessageLogs] ADD CONSTRAINT [FK_MessageLogs_Notices_RelatedNoticeId] FOREIGN KEY ([RelatedNoticeId]) REFERENCES [Notices] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [RoutineSlots] ADD CONSTRAINT [FK_RoutineSlots_AcademicSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [AcademicSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [SalaryPayments] ADD CONSTRAINT [FK_SalaryPayments_Funds_FundId] FOREIGN KEY ([FundId]) REFERENCES [Funds] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [StudentDiscounts] ADD CONSTRAINT [FK_StudentDiscounts_FeeHeads_FeeHeadId] FOREIGN KEY ([FeeHeadId]) REFERENCES [FeeHeads] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [StudentDiscounts] ADD CONSTRAINT [FK_StudentDiscounts_Funds_SponsorFundId] FOREIGN KEY ([SponsorFundId]) REFERENCES [Funds] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [StudentDiscounts] ADD CONSTRAINT [FK_StudentDiscounts_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    ALTER TABLE [StudentDocuments] ADD CONSTRAINT [FK_StudentDocuments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121205736_AddMissingForeignKeys'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121205736_AddMissingForeignKeys', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    ALTER TABLE [LedgerEntries] ADD [BookIssueId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    ALTER TABLE [BookIssues] ADD [FinePaid] decimal(12,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    ALTER TABLE [BookIssues] ADD [FineWaived] decimal(12,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Kind', N'NameBn') AND [object_id] = OBJECT_ID(N'[LedgerCategories]'))
        SET IDENTITY_INSERT [LedgerCategories] ON;
    EXEC(N'INSERT INTO [LedgerCategories] ([Id], [Kind], [NameBn])
    VALUES (8, CAST(1 AS tinyint), N''লাইব্রেরি জরিমানা'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Kind', N'NameBn') AND [object_id] = OBJECT_ID(N'[LedgerCategories]'))
        SET IDENTITY_INSERT [LedgerCategories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    CREATE INDEX [IX_LedgerEntries_BookIssueId] ON [LedgerEntries] ([BookIssueId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    ALTER TABLE [LedgerEntries] ADD CONSTRAINT [FK_LedgerEntries_BookIssues_BookIssueId] FOREIGN KEY ([BookIssueId]) REFERENCES [BookIssues] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122180305_LibraryFines'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122180305_LibraryFines', N'8.0.11');
END;
GO

COMMIT;
GO

