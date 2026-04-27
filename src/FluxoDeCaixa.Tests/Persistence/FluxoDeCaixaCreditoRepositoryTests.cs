using Dapper;
using FluxoDeCaixa.Tests.Persistence.Fixtures;
using System.Data.SqlClient;

namespace FluxoDeCaixa.Tests.Persistence
{
    [Collection(DatabaseCollection.Name)]
    public sealed class FluxoDeCaixaCreditoRepositoryTests : IAsyncLifetime
    {
        private readonly SqlServerFixture _fixture;
        private readonly FluxoDeCaixaCreditoRepository _sut;

        public FluxoDeCaixaCreditoRepositoryTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
            _sut = new FluxoDeCaixaCreditoRepository(_fixture.CriarDapperContext());
        }

        public Task InitializeAsync() => _fixture.LimparTabelasAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        // ── InsertAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task InsertAsync_EntidadeValida_RetornaTrue()
        {
            var entidade = CriarEntidade();
            var result = await _sut.InsertAsync(entidade);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task InsertAsync_EntidadeValida_GravaNoBancoDeDados()
        {
            var entidade = CriarEntidade(credito: 750m);
            await _sut.InsertAsync(entidade);

            var gravado = await BuscarPorIdAsync(entidade.ID);
            gravado.Should().NotBeNull();
            gravado!.credito.Should().Be(750m);
            gravado.descricao.Should().Be(entidade.descricao);
        }

        [Fact]
        public async Task InsertAsync_DuasEntidadesDistintas_AmbasGravadas()
        {
            var e1 = CriarEntidade(credito: 100m);
            var e2 = CriarEntidade(credito: 200m, dataFc: new DateOnly(2026, 4, 2));

            await _sut.InsertAsync(e1);
            await _sut.InsertAsync(e2);

            var total = await ContarRegistrosAsync();
            total.Should().Be(2);
        }

        // ── UpdateAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_RegistroExistente_AtualizaValores()
        {
            var entidade = CriarEntidade(credito: 300m);
            await _sut.InsertAsync(entidade);

            entidade.credito   = 999m;
            entidade.descricao = "Atualizado";
            var updated = await _sut.UpdateAsync(entidade);

            updated.Should().BeTrue();
            var gravado = await BuscarPorIdAsync(entidade.ID);
            gravado!.credito.Should().Be(999m);
            gravado.descricao.Should().Be("Atualizado");
        }

        [Fact]
        public async Task UpdateAsync_RegistroInexistente_RetornaFalse()
        {
            var entidade = CriarEntidade();
            var result = await _sut.UpdateAsync(entidade);
            result.Should().BeFalse();
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static FluxoDeCaixaCredito CriarEntidade(
            decimal credito     = 500m,
            string? descricao   = "Receita teste",
            DateOnly? dataFc    = null) => new()
        {
            ID        = Guid.NewGuid(),
            dataFC    = dataFc ?? new DateOnly(2026, 4, 1),
            credito   = credito,
            descricao = descricao ?? "Receita teste"
        };

        private async Task<FluxoDeCaixaCredito?> BuscarPorIdAsync(Guid id)
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            return await conn.QueryFirstOrDefaultAsync<FluxoDeCaixaCredito>(
                "SELECT * FROM FluxoDeCaixa WHERE ID = @id", new { id });
        }

        private async Task<int> ContarRegistrosAsync()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM FluxoDeCaixa");
        }
    }
}
