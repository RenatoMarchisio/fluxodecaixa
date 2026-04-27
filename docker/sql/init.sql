-- =====================================================================
-- init.sql — Bootstrap do banco FluxoCaixa
-- Executado automaticamente pelo container 'sql-init' do docker-compose
-- após o SQL Server ficar pronto.
-- =====================================================================

-- Cria o database se não existir
IF DB_ID('FluxoCaixa') IS NULL
BEGIN
    PRINT 'Criando database FluxoCaixa...';
    CREATE DATABASE FluxoCaixa;
END
GO

USE FluxoCaixa;
GO

-- ---------------------------------------------------------------------
-- Tabela: FluxoDeCaixa  (write model)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.FluxoDeCaixa', 'U') IS NULL
BEGIN
    PRINT 'Criando tabela FluxoDeCaixa...';
    CREATE TABLE dbo.FluxoDeCaixa(
        ID         uniqueidentifier NOT NULL,
        dataFC     date             NOT NULL,
        credito    money            NOT NULL,
        debito     money            NOT NULL,
        descricao  varchar(255)     NOT NULL,
        criadoEm   datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_FluxoDeCaixa PRIMARY KEY CLUSTERED (ID)
    );

    CREATE NONCLUSTERED INDEX IX_FluxoDeCaixa_dataFC
        ON dbo.FluxoDeCaixa (dataFC DESC) INCLUDE (credito, debito);

    ALTER TABLE [dbo].[FluxoDeCaixa] ADD  DEFAULT ((0)) FOR [credito];
    ALTER TABLE [dbo].[FluxoDeCaixa] ADD  DEFAULT ((0)) FOR [debito];
    
END
GO


-- ---------------------------------------------------------------------
-- Tabela: FluxoDeCaixaConsolidado  (read model — pré-agregada por dia)
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.FluxoDeCaixaConsolidado', 'U') IS NULL
BEGIN
    PRINT 'Criando tabela FluxoDeCaixaConsolidado...';
    CREATE TABLE dbo.FluxoDeCaixaConsolidado(
        dataFC    date      NOT NULL,
        credito   money     NOT NULL,
        debito    money     NOT NULL,
        criadoEm  datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_FluxoDeCaixaConsolidado PRIMARY KEY CLUSTERED (dataFC DESC)
    );
    
    ALTER TABLE [dbo].[FluxoDeCaixaConsolidado] ADD  DEFAULT ((0)) FOR [credito]
    ALTER TABLE [dbo].[FluxoDeCaixaConsolidado] ADD  DEFAULT ((0)) FOR [debito]

END
GO

PRINT 'tabelas criadas com sucesso.';
GO

-- ============================================================
-- Stored Procedure: sp_CriarConsolidadoMesSeguinte
--
-- Objetivo: Pré-criar os registros do mês seguinte na tabela
--           FluxoDeCaixaConsolidado (um row por dia), com
--           credito=0 e debito=0.
--
-- Execução: rodar no último dia de cada mês via SQL Server Agent.
--
-- Estratégia UPSERT: usa MERGE para não duplicar caso a SP
-- seja chamada mais de uma vez no mesmo dia.
-- ============================================================

CREATE OR ALTER PROCEDURE [dbo].[sp_CriarConsolidadoMesSeguinte]
AS
BEGIN
    SET NOCOUNT ON;

    -- Primeiro dia do mês seguinte
    DECLARE @PrimeiroDia DATE = DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));

    -- Último dia do mês seguinte
    DECLARE @UltimoDia  DATE = EOMONTH(@PrimeiroDia);

    -- Tabela temporária com todos os dias do mês seguinte
    DECLARE @Dias TABLE (dataFC DATE);

    DECLARE @Dia DATE = @PrimeiroDia;
    WHILE @Dia <= @UltimoDia
    BEGIN
        INSERT INTO @Dias (dataFC) VALUES (@Dia);
        SET @Dia = DATEADD(DAY, 1, @Dia);
    END

    -- UPSERT: insere somente os dias que ainda não existem
    MERGE INTO [dbo].[FluxoDeCaixaConsolidado] WITH (HOLDLOCK) AS target
    USING @Dias AS source ON target.dataFC = source.dataFC
    WHEN NOT MATCHED THEN
        INSERT (dataFC, credito, debito, criadoEm)
        VALUES (source.dataFC, 0, 0, SYSUTCDATETIME());

    DECLARE @Inseridos INT = @@ROWCOUNT;

    PRINT CONCAT(
        'sp_CriarConsolidadoMesSeguinte concluída. ',
        @Inseridos, ' dia(s) pré-criado(s) para ',
        FORMAT(@PrimeiroDia, 'MMMM/yyyy', 'pt-BR'),
        '.');
END
GO

-- ============================================================
-- SQL Server Agent Job: FluxoDeCaixa_CriarConsolidadoMensal
--
-- Roda no último dia de cada mês às 23:00 (hora local do servidor).
-- Cria os registros do mês seguinte em FluxoDeCaixaConsolidado.
-- na instancia do SQL Server Visual Studio 2022 não há Sql Agents 
-- para isso vamos usar a criação de um scheduled no windows
-- incluido no setup.md    
-- ============================================================

USE [msdb]
GO

-- Remove o job se já existir (idempotente)
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'FluxoDeCaixa_CriarConsolidadoMensal')
    EXEC msdb.dbo.sp_delete_job @job_name = N'FluxoDeCaixa_CriarConsolidadoMensal';
GO

EXEC msdb.dbo.sp_add_job
    @job_name = N'FluxoDeCaixa_CriarConsolidadoMensal',
    @enabled   = 1,
    @description = N'Pré-cria os registros do mês seguinte em FluxoDeCaixaConsolidado. Roda no último dia de cada mês.';
GO

EXEC msdb.dbo.sp_add_jobstep
    @job_name      = N'FluxoDeCaixa_CriarConsolidadoMensal',
    @step_name     = N'Executar sp_CriarConsolidadoMesSeguinte',
    @command       = N'EXEC [FluxoCaixa].[dbo].[sp_CriarConsolidadoMesSeguinte]',
    @database_name = N'msdb';
GO

-- Schedule: último dia de cada mês às 23:00
-- freq_type=16 → mensal | freq_interval=1 → dia 1 | freq_relative_interval=16 → último dia
EXEC msdb.dbo.sp_add_schedule
    @schedule_name          = N'FluxoDeCaixa_UltimoDiaMes_23h',
    @freq_type              = 16,       -- mensal
    @freq_interval          = 1,        -- qualquer dia válido (ignorado com relative)
    @freq_relative_interval = 16,       -- último
    @active_start_time      = 230000,   -- 23:00:00
    @freq_recurrence_factor = 1;
GO

EXEC msdb.dbo.sp_attach_schedule
    @job_name      = N'FluxoDeCaixa_CriarConsolidadoMensal',
    @schedule_name = N'FluxoDeCaixa_UltimoDiaMes_23h';
GO

EXEC msdb.dbo.sp_add_jobserver
    @job_name   = N'FluxoDeCaixa_CriarConsolidadoMensal',
    @server_name = N'(local)';
GO


PRINT 'Job FluxoDeCaixa_CriarConsolidadoMensal criado com sucesso.';
GO

