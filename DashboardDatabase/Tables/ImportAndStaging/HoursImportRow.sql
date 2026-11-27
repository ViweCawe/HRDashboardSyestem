CREATE TABLE [hr].[HoursImportRow]
(
    [StagingRowId] bigint IDENTITY(1,1) NOT NULL,
    [ImportBatchId] bigint NOT NULL,
    [SourceRowNumber] int NOT NULL,
    [RawDataJson] nvarchar(max) NOT NULL,
    [NormalizedDataJson] nvarchar(max) NOT NULL,
    [ValidationStatus] nvarchar(30) NOT NULL,
    [ResolvedEmploymentId] bigint NULL,
    CONSTRAINT [PK_HoursImportRow] PRIMARY KEY ([StagingRowId]),
    CONSTRAINT [UQ_HoursImportRow_0] UNIQUE ([ImportBatchId], [SourceRowNumber]),
    CONSTRAINT [CK_HoursImportRow_1] CHECK (ISJSON([RawDataJson]) = 1 AND ISJSON([NormalizedDataJson]) = 1),
    CONSTRAINT [FK_HoursImportRow_2] FOREIGN KEY ([ImportBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId]),
    CONSTRAINT [FK_HoursImportRow_3] FOREIGN KEY ([ResolvedEmploymentId]) REFERENCES [hr].[Employment] ([EmploymentId])
);
