using Dapper;
using FluxoDeCaixa.Infrastructure.Dapper;
using Testcontainers.MsSql;

namespace FluxoDeCaixa.Tests.Persistence.Fixtures
{
    /// <summary>
    /// Sobe um container SQL Server (via Testcontainers) uma única vez
    /// para todos os testes de repositório da coleção <see cref="DatabaseCollection"/>.
    /// </summary>
    public sealed class SqlServerFixture : IAsyncLifetime
    {
        private readonly MsSqlContainer _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        public string ConnectionString { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            // Registra o TypeHandler para DateOnly uma única vez
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            await CriarEsquemaAsync();
        }

        public async Task DisposeAsync() => await _container.DisposeAsync();

        // ----------------------------------------------------------------
        // Cria as tabelas necessárias para os testes
        // ----------------------------------------------------------------
        private async Task CriarEsquemaAsync()
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sysobjects WHERE name='FluxoDeCaixa' AND xtype='U')
CREATE TABLE [dbo].[FluxoDeCaixa]
(
    [ID]        UNIQUEIDENTIFIER NOT NULL,
    [dataFC]    DATE             NOT NULL,
    [credito]   MONEY            NOT NULL DEFAULT 0,
    [debito]    MONEY            NOT NULL DEFAULT 0,
    [criadoEm]  DATETIME2(7)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [descricao] NVARCHAR(255)    NOT NULL,
    CONSTRAINT PK_FluxoDeCaixa PRIMARY KEY CLUSTERED ([ID] ASC)
);

IF NOT EXISTS (SELECT 1 FROM sysobjects WHERE name='FluxoDeCaixaConsolidado' AND xtype='U')
CREATE TABLE [dbo].[FluxoDeCaixaConsolidado]
(
    [dataFC]   DATE         NOT NULL,
    [credito]  MONEY        NOT NULL DEFAULT 0,
    [debito]   MONEY        NOT NULL DEFAULT 0,
    [criadoEm] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Consolidado PRIMARY KEY CLUSTERED ([dataFC] DESC)
);";

            using var conn = new System.Data.SqlClient.SqlConnection(ConnectionString);
            await conn.ExecuteAsync(sql);
        }

        /// <summary>
        /// Limpa todas as tabelas entre os testes para garantir isolamento.
        /// </summary>
        public async Task LimparTabelasAsync()
        {
            const string sql = @"
                DELETE FROM [dbo].[FluxoDeCaixaConsolidado];
                DELETE FROM [dbo].[FluxoDeCaixa];";

            using var conn = new System.Data.SqlClient.SqlConnection(ConnectionString);
            await conn.ExecuteAsync(sql);
        }

        /// <summary>
        /// Cria um <see cref="DapperContextFC"/> apontando para o container.
        /// </summary>
        public DapperContextFC CriarDapperContext()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:FluxoDeCaixaConnection", ConnectionString }
                })
                .Build();

            return new DapperContextFC(config);
        }
    }
}
