CREATE TABLE [hr].[LeaveImportRow]
(
    [StagingRowId] bigint IDENTITY(1,1) NOT NULL,
    [ImportBatchId] bigint NOT NULL,
    [SourceRowNumber] int NOT NULL,
    [RawDataJson] nvarchar(max) NOT NULL,
    [NormalizedDataJson] nvarchar(max) NOT NULL,
    [ValidationStatus] nvarchar(30) NOT NULL,
    [ResolvedEmploymentId] bigint NULL,
    CONSTRAINT [PK_LeaveImportRow] PRIMARY KEY ([StagingRowId]),
    CONSTRAINT [UQ_LeaveImportRow_0] UNIQUE ([ImportBatchId], [SourceRowNumber]),
    CONSTRAINT [CK_LeaveImportRow_1] CHECK (ISJSON([RawDataJson]) = 1 AND ISJSON([NormalizedDataJson]) = 1),
    CONSTRAINT [FK_LeaveImportRow_2] FOREIGN KEY ([ImportBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId]),
    CONSTRAINT [FK_LeaveImportRow_3] FOREIGN KEY ([ResolvedEmploymentId]) REFERENCES [hr].[Employment] ([EmploymentId])
);
