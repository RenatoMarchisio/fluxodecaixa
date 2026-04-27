namespace FluxoDeCaixa.Tests.Application.Validators
{
    public sealed class CreateFluxoDeCaixaCreditoValidatorTests
    {
        private readonly CreateFluxoDeCaixaCreditoValidator _sut = new();

        // ── Cenários válidos ─────────────────────────────────────────────

        [Fact]
        public async Task Validate_ComandoValido_DevePassar()
        {
            var command = ComandoValido();
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(500)]
        [InlineData(9_999_999_999)]
        public async Task Validate_CreditoNosLimites_DevePassar(decimal credito)
        {
            var command = ComandoValido(credito: credito);
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeTrue();
        }

        // ── Descrição ────────────────────────────────────────────────────

        [Fact]
        public async Task Validate_DescricaoVazia_DeveReprovар()
        {
            var command = ComandoValido(descricao: "");
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "descricao");
        }

        [Fact]
        public async Task Validate_DescricaoAcimaDe255Chars_DeveReprovar()
        {
            var command = ComandoValido(descricao: new string('A', 256));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "descricao");
        }

        // ── Crédito ──────────────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task Validate_CreditoMenorQueUm_DeveReprovar(decimal credito)
        {
            var command = ComandoValido(credito: credito);
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "credito");
        }

        [Fact]
        public async Task Validate_CreditoAcimaDoMaximo_DeveReprovar()
        {
            var command = ComandoValido(credito: 10_000_000_000m);
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "credito");
        }

        // ── Data ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Validate_DataAntesDe2020_DeveReprovar()
        {
            var command = ComandoValido(dataFc: new DateOnly(2019, 12, 31));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "dataFC");
        }

        [Fact]
        public async Task Validate_DataDepoisDe2030_DeveReprovar()
        {
            var command = ComandoValido(dataFc: new DateOnly(2031, 1, 1));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "dataFC");
        }

        // ── Helper ───────────────────────────────────────────────────────

        private static CreateFluxoDeCaixaCreditoCommand ComandoValido(
            decimal credito   = 100m,
            string? descricao = "Pagamento cliente",
            DateOnly? dataFc  = null) => new()
        {
            credito   = credito,
            descricao = descricao ?? "Pagamento cliente",
            dataFC    = dataFc ?? new DateOnly(2026, 1, 15)
        };
    }
}
