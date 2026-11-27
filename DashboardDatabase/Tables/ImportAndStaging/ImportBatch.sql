CREATE TABLE [hr].[ImportBatch]
(
    [ImportBatchId] bigint IDENTITY(1,1) NOT NULL,
    [SourceSystem] nvarchar(50) NOT NULL,
    [SourceType] nvarchar(30) NOT NULL,
    [FileName] nvarchar(260) NOT NULL,
    [FileHash] char(64) NOT NULL,
    [SourceAsOfDate] date NOT NULL,
    [ReportingPeriodId] int NULL,
    [ImportedOn] datetime2 NOT NULL,
    [ImportedBy] nvarchar(200) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [RowCount] int NOT NULL,
    CONSTRAINT [PK_ImportBatch] PRIMARY KEY ([ImportBatchId]),
    CONSTRAINT [UQ_ImportBatch_0] UNIQUE ([SourceType], [SourceAsOfDate], [FileHash]),
    CONSTRAINT [CK_ImportBatch_1] CHECK ([Status] IN ('Staged','Review','Published')),
    CONSTRAINT [CK_ImportBatch_2] CHECK ([SourceType] IN ('Employees','Hours','LeaveBalances')),
    CONSTRAINT [FK_ImportBatch_3] FOREIGN KEY ([ReportingPeriodId]) REFERENCES [hr].[ReportingPeriod] ([ReportingPeriodId])
);
