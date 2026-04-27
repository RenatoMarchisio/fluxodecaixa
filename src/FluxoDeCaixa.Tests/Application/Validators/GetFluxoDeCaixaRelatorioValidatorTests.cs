namespace FluxoDeCaixa.Tests.Application.Validators
{
    public sealed class GetFluxoDeCaixaRelatorioValidatorTests
    {
        private readonly GetFluxoDeCaixaRelatorioByInicioFimValidator _sut = new();

        [Fact]
        public async Task Validate_QueryValida_DevePassar()
        {
            var query = new GetRelatorioByDateFCInicioFimQuery
            {
                Inicio = new DateTime(2026, 1, 1),
                Fim    = new DateTime(2026, 1, 31)
            };

            var result = await _sut.ValidateAsync(query);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_FimIgualAoInicio_DeveReprovar()
        {
            var data = new DateTime(2026, 4, 10);
            var query = new GetRelatorioByDateFCInicioFimQuery { Inicio = data, Fim = data };

            var result = await _sut.ValidateAsync(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Fim");
        }

        [Fact]
        public async Task Validate_FimAntesDeInicio_DeveReprovar()
        {
            var query = new GetRelatorioByDateFCInicioFimQuery
            {
                Inicio = new DateTime(2026, 4, 30),
                Fim    = new DateTime(2026, 4, 1)
            };

            var result = await _sut.ValidateAsync(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == "Fim" &&
                e.ErrorMessage.Contains("maior"));
        }

        [Fact]
        public async Task Validate_InicioVazio_DeveReprovar()
        {
            var query = new GetRelatorioByDateFCInicioFimQuery
            {
                Inicio = default,
                Fim    = new DateTime(2026, 1, 31)
            };

            var result = await _sut.ValidateAsync(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Inicio");
        }

        [Fact]
        public async Task Validate_FimVazio_DeveReprovar()
        {
            var query = new GetRelatorioByDateFCInicioFimQuery
            {
                Inicio = new DateTime(2026, 1, 1),
                Fim    = default
            };

            var result = await _sut.ValidateAsync(query);
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Fim");
        }
    }
}
