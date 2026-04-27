using Dapper;
using FluxoDeCaixa.Tests.Persistence.Fixtures;
using System.Data.SqlClient;

namespace FluxoDeCaixa.Tests.Persistence
{
    [Collection(DatabaseCollection.Name)]
    public sealed class FluxoDeCaixaDebitoRepositoryTests : IAsyncLifetime
    {
        private readonly SqlServerFixture _fixture;
        private readonly FluxoDeCaixaDebitoRepository _sut;

        public FluxoDeCaixaDebitoRepositoryTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
            _sut = new FluxoDeCaixaDebitoRepository(_fixture.CriarDapperContext());
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
        public async Task InsertAsync_GravaNoBancoDeDados()
        {
            var entidade = CriarEntidade(debito: 1500m, descricao: "Fornecedor X");
            await _sut.InsertAsync(entidade);

            var gravado = await BuscarPorIdAsync(entidade.ID);
            gravado.Should().NotBeNull();
            gravado!.debito.Should().Be(1500m);
            gravado.descricao.Should().Be("Fornecedor X");
        }

        [Fact]
        public async Task InsertAsync_MultiplosDebitos_GravaTodos()
        {
            await _sut.InsertAsync(CriarEntidade(debito: 100m));
            await _sut.InsertAsync(CriarEntidade(debito: 200m, dataFc: new DateOnly(2026, 5, 2)));
            await _sut.InsertAsync(CriarEntidade(debito: 300m, dataFc: new DateOnly(2026, 5, 3)));

            var total = await ContarRegistrosAsync();
            total.Should().Be(3);
        }

        // ── UpdateAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_RegistroExistente_AtualizaValores()
        {
            var entidade = CriarEntidade(debito: 400m, descricao: "Original");
            await _sut.InsertAsync(entidade);

            entidade.debito    = 800m;
            entidade.descricao = "Corrigido";
            var updated = await _sut.UpdateAsync(entidade);

            updated.Should().BeTrue();
            var gravado = await BuscarPorIdAsync(entidade.ID);
            gravado!.debito.Should().Be(800m);
            gravado.descricao.Should().Be("Corrigido");
        }

        [Fact]
        public async Task UpdateAsync_RegistroInexistente_RetornaFalse()
        {
            var entidade = CriarEntidade();
            var result = await _sut.UpdateAsync(entidade);
            result.Should().BeFalse();
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static FluxoDeCaixaDebito CriarEntidade(
            decimal debito      = 250m,
            string? descricao   = "Despesa teste",
            DateOnly? dataFc    = null) => new()
        {
            ID        = Guid.NewGuid(),
            dataFC    = dataFc ?? new DateOnly(2026, 5, 1),
            debito    = debito,
            descricao = descricao ?? "Despesa teste"
        };

        private async Task<FluxoDeCaixaDebito?> BuscarPorIdAsync(Guid id)
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            return await conn.QueryFirstOrDefaultAsync<FluxoDeCaixaDebito>(
                "SELECT * FROM FluxoDeCaixa WHERE ID = @id", new { id });
        }

        private async Task<int> ContarRegistrosAsync()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM FluxoDeCaixa");
        }
    }
}
