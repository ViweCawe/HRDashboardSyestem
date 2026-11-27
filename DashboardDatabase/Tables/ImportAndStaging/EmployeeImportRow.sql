CREATE TABLE [hr].[EmployeeImportRow]
(
    [StagingRowId] bigint IDENTITY(1,1) NOT NULL,
    [ImportBatchId] bigint NOT NULL,
    [SourceRowNumber] int NOT NULL,
    [RawDataJson] nvarchar(max) NOT NULL,
    [NormalizedDataJson] nvarchar(max) NOT NULL,
    [ValidationStatus] nvarchar(30) NOT NULL,
    [ResolvedEmploymentId] bigint NULL,
    CONSTRAINT [PK_EmployeeImportRow] PRIMARY KEY ([StagingRowId]),
    CONSTRAINT [UQ_EmployeeImportRow_0] UNIQUE ([ImportBatchId], [SourceRowNumber]),
    CONSTRAINT [CK_EmployeeImportRow_1] CHECK (ISJSON([RawDataJson]) = 1 AND ISJSON([NormalizedDataJson]) = 1),
    CONSTRAINT [FK_EmployeeImportRow_2] FOREIGN KEY ([ImportBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId]),
    CONSTRAINT [FK_EmployeeImportRow_3] FOREIGN KEY ([ResolvedEmploymentId]) REFERENCES [hr].[Employment] ([EmploymentId])
);
