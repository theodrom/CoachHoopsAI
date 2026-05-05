CREATE TABLE [Analyses] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [Level] nvarchar(32) NOT NULL,
    [RequestedRulesProfile] nvarchar(128) NULL,
    [AppliedRulesProfile] nvarchar(128) NOT NULL,
    [GameDate] datetime2 NULL,
    [TeamName] nvarchar(128) NULL,
    [OpponentName] nvarchar(128) NULL,
    [Season] nvarchar(max) NULL,
    [Location] nvarchar(max) NULL,
    [RulesetVersion] nvarchar(32) NOT NULL,
    [PromptVersion] nvarchar(32) NOT NULL,
    [AiModel] nvarchar(64) NOT NULL,
    [InputJson] nvarchar(max) NOT NULL,
    [ProblemTagsJson] nvarchar(max) NOT NULL,
    [DiagnosticsJson] nvarchar(max) NOT NULL,
    [SuggestionsJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Analyses] PRIMARY KEY ([Id])
);
GO


CREATE INDEX [IX_Analyses_CreatedUtc] ON [Analyses] ([CreatedUtc]);
GO


CREATE INDEX [IX_Analyses_GameDate] ON [Analyses] ([GameDate]);
GO


CREATE INDEX [IX_Analyses_TeamName] ON [Analyses] ([TeamName]);
GO


