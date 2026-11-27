CREATE TABLE [hr].[TerminationReason]
(
    [TerminationReasonId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_TerminationReason] PRIMARY KEY ([TerminationReasonId]),
    CONSTRAINT [UQ_TerminationReason_0] UNIQUE ([Code])
);
