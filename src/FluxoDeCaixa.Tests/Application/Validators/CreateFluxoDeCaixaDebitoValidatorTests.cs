namespace FluxoDeCaixa.Tests.Application.Validators
{
    public sealed class CreateFluxoDeCaixaDebitoValidatorTests
    {
        private readonly CreateFluxoDeCaixaDebitoValidator _sut = new();

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
        [InlineData(250.50)]
        [InlineData(9_999_999_999)]
        public async Task Validate_DebitoNosLimites_DevePassar(decimal debito)
        {
            var command = ComandoValido(debito: debito);
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeTrue();
        }

        // ── Descrição ────────────────────────────────────────────────────

        [Fact]
        public async Task Validate_DescricaoVazia_DeveReprovar()
        {
            var command = ComandoValido(descricao: "");
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "descricao");
        }

        [Fact]
        public async Task Validate_DescricaoComExatamente255Chars_DevePassar()
        {
            var command = ComandoValido(descricao: new string('X', 255));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_DescricaoComExatamente256Chars_DeveReprovar()
        {
            var command = ComandoValido(descricao: new string('X', 256));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
        }

        // ── Débito ───────────────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-0.01)]
        [InlineData(-9999)]
        public async Task Validate_DebitoMenorQueUm_DeveReprovar(decimal debito)
        {
            var command = ComandoValido(debito: debito);
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "debito");
        }

        // ── Data ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(2020, 1, 1)]
        [InlineData(2025, 6, 15)]
        [InlineData(2030, 12, 31)]
        public async Task Validate_DataDentroDoIntervalo_DevePassar(int ano, int mes, int dia)
        {
            var command = ComandoValido(dataFc: new DateOnly(ano, mes, dia));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_DataInvalida_DeveReprovar()
        {
            var command = ComandoValido(dataFc: new DateOnly(2019, 1, 1));
            var result = await _sut.ValidateAsync(command);
            result.IsValid.Should().BeFalse();
        }

        // ── Helper ───────────────────────────────────────────────────────

        private static CreateFluxoDeCaixaDebitoCommand ComandoValido(
            decimal debito    = 50m,
            string? descricao = "Aluguel",
            DateOnly? dataFc  = null) => new()
        {
            debito    = debito,
            descricao = descricao ?? "Aluguel",
            dataFC    = dataFc ?? new DateOnly(2026, 3, 10)
        };
    }
}
