using FluxoDeCaixa.Application.UseCases.FluxoDeCaixaRelatorio.Queries.GetRelatorioQuery;

namespace FluxoDeCaixa.Tests.Application.Handlers
{
    public sealed class GetRelatorioHandlerTests
    {
        private readonly IUnitOfWorkFluxoDeCaixa      _uow    = Substitute.For<IUnitOfWorkFluxoDeCaixa>();
        private readonly IMapper                      _mapper = Substitute.For<IMapper>();
        private readonly GetRelatorioByDateFCInicioFimQueryHandler _sut;

        public GetRelatorioHandlerTests()
        {
            _sut = new GetRelatorioByDateFCInicioFimQueryHandler(_uow, _mapper);
        }

        [Fact]
        public async Task Handle_ComDadosNoPeriodo_RetornaListaESuccessoTrue()
        {
            // Arrange
            var inicio = new DateTime(2026, 4, 1);
            var fim    = new DateTime(2026, 4, 30);
            var dados  = new List<FluxoDeCaixaRelatorioDto>
            {
                new() { DataFC = new DateOnly(2026, 4, 10), Credito = 1000m, Debito = 200m },
                new() { DataFC = new DateOnly(2026, 4, 20), Credito = 500m,  Debito = 100m }
            };

            _uow.FluxoDeCaixaRelatorio
                .GetFluxoDeCaixaRelatorioAsync(inicio, fim)
                .Returns(dados);

            var query = new GetRelatorioByDateFCInicioFimQuery { Inicio = inicio, Fim = fim };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.succcess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Message.Should().Be("Query executada!");
        }

        [Fact]
        public async Task Handle_SemDadosNoPeriodo_RetornaListaVaziaESuccessoTrue()
        {
            // Arrange
            var inicio = new DateTime(2026, 1, 1);
            var fim    = new DateTime(2026, 1, 31);

            _uow.FluxoDeCaixaRelatorio
                .GetFluxoDeCaixaRelatorioAsync(inicio, fim)
                .Returns(Enumerable.Empty<FluxoDeCaixaRelatorioDto>());

            var query = new GetRelatorioByDateFCInicioFimQuery { Inicio = inicio, Fim = fim };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.succcess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_RepositorioLancaExcecao_SuccessoFalseComMensagem()
        {
            // Arrange
            var inicio = new DateTime(2026, 2, 1);
            var fim    = new DateTime(2026, 2, 28);

            _uow.FluxoDeCaixaRelatorio
                .GetFluxoDeCaixaRelatorioAsync(inicio, fim)
                .ThrowsAsync(new Exception("Falha na consulta"));

            var query = new GetRelatorioByDateFCInicioFimQuery { Inicio = inicio, Fim = fim };

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.succcess.Should().BeFalse();
            result.Message.Should().Contain("Falha na consulta");
        }

        [Fact]
        public async Task Handle_ChamaRepositorioComParametrosCorretos()
        {
            // Arrange
            var inicio = new DateTime(2026, 3, 1);
            var fim    = new DateTime(2026, 3, 31);

            _uow.FluxoDeCaixaRelatorio
                .GetFluxoDeCaixaRelatorioAsync(inicio, fim)
                .Returns(new List<FluxoDeCaixaRelatorioDto>());

            var query = new GetRelatorioByDateFCInicioFimQuery { Inicio = inicio, Fim = fim };

            // Act
            await _sut.Handle(query, CancellationToken.None);

            // Assert
            await _uow.FluxoDeCaixaRelatorio
                .Received(1)
                .GetFluxoDeCaixaRelatorioAsync(inicio, fim);
        }

        [Fact]
        public void Constructor_UoWNulo_LancaArgumentNullException()
        {
            Action act = () => new GetRelatorioByDateFCInicioFimQueryHandler(null!, _mapper);
            act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
        }

        [Fact]
        public void Constructor_MapperNulo_LancaArgumentNullException()
        {
            Action act = () => new GetRelatorioByDateFCInicioFimQueryHandler(_uow, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
        }
    }
}
