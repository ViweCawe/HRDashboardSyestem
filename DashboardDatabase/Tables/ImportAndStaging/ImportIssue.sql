CREATE TABLE [hr].[ImportIssue]
(
    [ImportIssueId] bigint IDENTITY(1,1) NOT NULL,
    [ImportBatchId] bigint NOT NULL,
    [SourceRowNumber] int NULL,
    [SourceField] nvarchar(100) NULL,
    [RuleCode] nvarchar(80) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [ResolvedOn] datetime2 NULL,
    CONSTRAINT [PK_ImportIssue] PRIMARY KEY ([ImportIssueId]),
    CONSTRAINT [FK_ImportIssue_0] FOREIGN KEY ([ImportBatchId]) REFERENCES [hr].[ImportBatch] ([ImportBatchId])
);
