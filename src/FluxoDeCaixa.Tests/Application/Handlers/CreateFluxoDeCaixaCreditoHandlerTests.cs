
namespace FluxoDeCaixa.Tests.Application.Handlers
{
    public sealed class CreateFluxoDeCaixaCreditoHandlerTests
    {
        private readonly IUnitOfWorkFluxoDeCaixa _uow        = Substitute.For<IUnitOfWorkFluxoDeCaixa>();
        private readonly IMapper                 _mapper     = Substitute.For<IMapper>();
        private readonly CreateFluxoDeCaixaCreditoHandler _sut;

        private static readonly FluxoDeCaixaCredito EntidadeValida = new()
        {
            ID        = Guid.NewGuid(),
            dataFC    = new DateOnly(2026, 4, 1),
            descricao = "Venda",
            credito   = 500m
        };

        public CreateFluxoDeCaixaCreditoHandlerTests()
        {
            _sut = new CreateFluxoDeCaixaCreditoHandler(_uow, _mapper);
        }

        [Fact]
        public async Task Handle_InsertComSucesso_RetornaSuccessoTrue()
        {
            // Arrange
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaCredito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaCredito.InsertAsync(EntidadeValida).Returns(true);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.succcess.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Criado com sucesso!");
        }

        [Fact]
        public async Task Handle_InsertRetornaFalse_RetornaSuccessoFalse()
        {
            // Arrange
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaCredito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaCredito.InsertAsync(EntidadeValida).Returns(false);

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.succcess.Should().BeFalse();
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_RepositorioLancaExcecao_RetornaMensagemDeErro()
        {
            // Arrange
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaCredito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaCredito
                .InsertAsync(Arg.Any<FluxoDeCaixaCredito>())
                .ThrowsAsync(new Exception("Conexão recusada"));

            // Act
            var result = await _sut.Handle(command, CancellationToken.None);

            // Assert
            result.succcess.Should().BeFalse();
            result.Message.Should().Contain("Conexão recusada");
        }

        [Fact]
        public async Task Handle_ChamamaInsertUmaVez()
        {
            // Arrange
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaCredito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaCredito.InsertAsync(EntidadeValida).Returns(true);

            // Act
            await _sut.Handle(command, CancellationToken.None);

            // Assert
            await _uow.FluxoDeCaixaCredito.Received(1).InsertAsync(EntidadeValida);
        }

        [Fact]
        public void Constructor_UoWNulo_LancaArgumentNullException()
        {
            Action act = () => new CreateFluxoDeCaixaCreditoHandler(null!, _mapper);
            act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
        }

        [Fact]
        public void Constructor_MapperNulo_LancaArgumentNullException()
        {
            Action act = () => new CreateFluxoDeCaixaCreditoHandler(_uow, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
        }

        private static CreateFluxoDeCaixaCreditoCommand CriarCommand() => new()
        {
            dataFC    = new DateOnly(2026, 4, 1),
            descricao = "Venda",
            credito   = 500m
        };
    }
}
