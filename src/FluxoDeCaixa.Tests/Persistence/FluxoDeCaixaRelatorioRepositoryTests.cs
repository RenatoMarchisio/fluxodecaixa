using Dapper;
using FluxoDeCaixa.Tests.Persistence.Fixtures;
using System.Data.SqlClient;

namespace FluxoDeCaixa.Tests.Persistence
{
    [Collection(DatabaseCollection.Name)]
    public sealed class FluxoDeCaixaRelatorioRepositoryTests : IAsyncLifetime
    {
        private readonly SqlServerFixture _fixture;
        private readonly FluxoDeCaixaRelatorioRepository _sut;

        public FluxoDeCaixaRelatorioRepositoryTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
            _sut = new FluxoDeCaixaRelatorioRepository(_fixture.CriarDapperContext());
        }

        public Task InitializeAsync() => _fixture.LimparTabelasAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        // ── GetFluxoDeCaixaRelatorioAsync ────────────────────────────────

        [Fact]
        public async Task GetRelatorio_SemDadosNoPeriodo_RetornaListaVazia()
        {
            var result = await _sut.GetFluxoDeCaixaRelatorioAsync(
                new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRelatorio_ComDadosNoPeriodo_RetornaTodosOsRegistros()
        {
            await InserirConsolidadoAsync(new DateOnly(2026, 2, 10), 1000m, 200m);
            await InserirConsolidadoAsync(new DateOnly(2026, 2, 20), 500m,  100m);

            var result = await _sut.GetFluxoDeCaixaRelatorioAsync(
                new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetRelatorio_FiltraApenasOPeriodo()
        {
            // dados dentro
            await InserirConsolidadoAsync(new DateOnly(2026, 3, 15), 300m, 50m);
            // dados fora
            await InserirConsolidadoAsync(new DateOnly(2026, 2, 28), 999m, 0m);
            await InserirConsolidadoAsync(new DateOnly(2026, 4, 1),  0m,   999m);

            var result = await _sut.GetFluxoDeCaixaRelatorioAsync(
                new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            result.Should().HaveCount(1);
            result.First().Credito.Should().Be(300m);
        }

        [Fact]
        public async Task GetRelatorio_RetornaValoresCorretamente()
        {
            var dia = new DateOnly(2026, 4, 25);
            await InserirConsolidadoAsync(dia, 1234.56m, 789.01m);

            var result = await _sut.GetFluxoDeCaixaRelatorioAsync(
                new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

            var linha = result.Single();
            linha.Credito.Should().Be(1234.56m);
            linha.Debito.Should().Be(789.01m);
            linha.DataFC.Should().Be(dia);
        }

        [Fact]
        public async Task GetRelatorio_IniciasEFimSaoInclusivos()
        {
            var inicio = new DateOnly(2026, 5, 1);
            var fim    = new DateOnly(2026, 5, 31);

            await InserirConsolidadoAsync(inicio, 100m, 0m);
            await InserirConsolidadoAsync(fim,    200m, 0m);

            var result = await _sut.GetFluxoDeCaixaRelatorioAsync(
                inicio.ToDateTime(TimeOnly.MinValue),
                fim.ToDateTime(TimeOnly.MinValue));

            result.Should().HaveCount(2);
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private async Task InserirConsolidadoAsync(DateOnly dia, decimal credito, decimal debito)
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            await conn.ExecuteAsync(
                "INSERT INTO FluxoDeCaixaConsolidado (dataFC, credito, debito, criadoEm) " +
                "VALUES (@dia, @credito, @debito, SYSUTCDATETIME())",
                new { dia = dia.ToDateTime(TimeOnly.MinValue), credito, debito });
        }
    }
}
