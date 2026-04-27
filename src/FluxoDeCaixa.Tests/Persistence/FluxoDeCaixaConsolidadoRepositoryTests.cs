using Dapper;
using FluxoDeCaixa.Tests.Persistence.Fixtures;
using System.Data.SqlClient;

namespace FluxoDeCaixa.Tests.Persistence
{
    [Collection(DatabaseCollection.Name)]
    public sealed class FluxoDeCaixaConsolidadoRepositoryTests : IAsyncLifetime
    {
        private readonly SqlServerFixture _fixture;
        private readonly FluxoDeCaixaConsolidadoRepository _sut;

        public FluxoDeCaixaConsolidadoRepositoryTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
            _sut = new FluxoDeCaixaConsolidadoRepository(_fixture.CriarDapperContext());
        }

        public Task InitializeAsync() => _fixture.LimparTabelasAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        // ── UPSERT — Insert (dia não existe) ─────────────────────────────

        [Fact]
        public async Task UpsertAsync_DiaNaoExiste_CriaLinha()
        {
            var dia = new DateOnly(2026, 6, 1);

            await _sut.UpsertAsync(dia, credito: 1000m, debito: 0m);

            var linha = await BuscarConsolidadoAsync(dia);
            linha.Should().NotBeNull();
            linha!.Credito.Should().Be(1000m);
            linha.Debito.Should().Be(0m);
        }

        [Fact]
        public async Task UpsertAsync_DiaNaoExisteDebito_CriaLinhaComDebito()
        {
            var dia = new DateOnly(2026, 6, 2);

            await _sut.UpsertAsync(dia, credito: 0m, debito: 500m);

            var linha = await BuscarConsolidadoAsync(dia);
            linha!.Credito.Should().Be(0m);
            linha.Debito.Should().Be(500m);
        }

        // ── UPSERT — Update (dia já existe) ──────────────────────────────

        [Fact]
        public async Task UpsertAsync_DiaSobrepostoComCredito_AcumulaCredito()
        {
            var dia = new DateOnly(2026, 6, 3);

            await _sut.UpsertAsync(dia, credito: 300m, debito: 0m);
            await _sut.UpsertAsync(dia, credito: 200m, debito: 0m);

            var linha = await BuscarConsolidadoAsync(dia);
            linha!.Credito.Should().Be(500m);
            linha.Debito.Should().Be(0m);
        }

        [Fact]
        public async Task UpsertAsync_DiaSobrepostoComDebito_AcumulaDebito()
        {
            var dia = new DateOnly(2026, 6, 4);

            await _sut.UpsertAsync(dia, credito: 0m, debito: 100m);
            await _sut.UpsertAsync(dia, credito: 0m, debito: 150m);

            var linha = await BuscarConsolidadoAsync(dia);
            linha!.Debito.Should().Be(250m);
        }

        [Fact]
        public async Task UpsertAsync_MultiplosCreditosEDebitos_AcumulaCorreto()
        {
            var dia = new DateOnly(2026, 6, 5);

            await _sut.UpsertAsync(dia, credito: 1000m, debito: 0m);
            await _sut.UpsertAsync(dia, credito: 0m,    debito: 300m);
            await _sut.UpsertAsync(dia, credito: 500m,  debito: 0m);
            await _sut.UpsertAsync(dia, credito: 0m,    debito: 200m);

            var linha = await BuscarConsolidadoAsync(dia);
            linha!.Credito.Should().Be(1500m);
            linha.Debito.Should().Be(500m);
        }

        [Fact]
        public async Task UpsertAsync_TresDiasDistintos_CriaLinhasSeparadas()
        {
            var dias = new[]
            {
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 7, 2),
                new DateOnly(2026, 7, 3)
            };

            foreach (var d in dias)
                await _sut.UpsertAsync(d, credito: 100m, debito: 50m);

            var total = await ContarConsolidadoAsync();
            total.Should().Be(3);
        }

        [Fact]
        public async Task UpsertAsync_ConcorrenteSameDia_AcumulaCorretamente()
        {
            var dia = new DateOnly(2026, 8, 1);
            var tarefas = Enumerable.Range(1, 5)
                .Select(_ => _sut.UpsertAsync(dia, credito: 100m, debito: 0m));

            await Task.WhenAll(tarefas);

            var linha = await BuscarConsolidadoAsync(dia);
            linha!.Credito.Should().Be(500m);
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private async Task<FluxoDeCaixaRelatorioDto?> BuscarConsolidadoAsync(DateOnly dia)
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            return await conn.QueryFirstOrDefaultAsync<FluxoDeCaixaRelatorioDto>(
                "SELECT dataFC AS DataFC, credito AS Credito, debito AS Debito, criadoEm AS CriadoEm " +
                "FROM FluxoDeCaixaConsolidado WHERE dataFC = @dia",
                new { dia = dia.ToDateTime(TimeOnly.MinValue) });
        }

        private async Task<int> ContarConsolidadoAsync()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM FluxoDeCaixaConsolidado");
        }
    }
}
