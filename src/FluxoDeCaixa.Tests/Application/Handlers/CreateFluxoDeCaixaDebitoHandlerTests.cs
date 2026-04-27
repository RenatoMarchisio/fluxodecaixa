using FluxoDeCaixa.Application.UseCases.FluxoDeCaixa.Commands.CreateFluxoDeCaixaCommand;

namespace FluxoDeCaixa.Tests.Application.Handlers
{
    public sealed class CreateFluxoDeCaixaDebitoHandlerTests
    {
        private readonly IUnitOfWorkFluxoDeCaixa _uow    = Substitute.For<IUnitOfWorkFluxoDeCaixa>();
        private readonly IMapper                 _mapper = Substitute.For<IMapper>();
        private readonly CreateFluxoDeCaixaDebitoHandler _sut;

        private static readonly FluxoDeCaixaDebito EntidadeValida = new()
        {
            ID        = Guid.NewGuid(),
            dataFC    = new DateOnly(2026, 5, 10),
            descricao = "Aluguel",
            debito    = 1200m
        };

        public CreateFluxoDeCaixaDebitoHandlerTests()
        {
            _sut = new CreateFluxoDeCaixaDebitoHandler(_uow, _mapper);
        }

        [Fact]
        public async Task Handle_InsertComSucesso_RetornaSuccessoTrue()
        {
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaDebito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaDebito.InsertAsync(EntidadeValida).Returns(true);

            var result = await _sut.Handle(command, CancellationToken.None);

            result.succcess.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Criado com sucesso!");
        }

        [Fact]
        public async Task Handle_InsertRetornaFalse_SuccessoFalse()
        {
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaDebito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaDebito.InsertAsync(EntidadeValida).Returns(false);

            var result = await _sut.Handle(command, CancellationToken.None);

            result.succcess.Should().BeFalse();
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_RepositorioLancaExcecao_SuccessoFalseComMensagem()
        {
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaDebito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaDebito
                .InsertAsync(Arg.Any<FluxoDeCaixaDebito>())
                .ThrowsAsync(new Exception("Timeout de banco"));

            var result = await _sut.Handle(command, CancellationToken.None);

            result.succcess.Should().BeFalse();
            result.Message.Should().Contain("Timeout de banco");
        }

        [Fact]
        public async Task Handle_ChamaInsertUmaVez()
        {
            var command = CriarCommand();
            _mapper.Map<FluxoDeCaixaDebito>(command).Returns(EntidadeValida);
            _uow.FluxoDeCaixaDebito.InsertAsync(EntidadeValida).Returns(true);

            await _sut.Handle(command, CancellationToken.None);

            await _uow.FluxoDeCaixaDebito.Received(1).InsertAsync(EntidadeValida);
        }

        [Fact]
        public void Constructor_UoWNulo_LancaArgumentNullException()
        {
            Action act = () => new CreateFluxoDeCaixaDebitoHandler(null!, _mapper);
            act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
        }

        [Fact]
        public void Constructor_MapperNulo_LancaArgumentNullException()
        {
            Action act = () => new CreateFluxoDeCaixaDebitoHandler(_uow, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
        }

        private static CreateFluxoDeCaixaDebitoCommand CriarCommand() => new()
        {
            dataFC    = new DateOnly(2026, 5, 10),
            descricao = "Aluguel",
            debito    = 1200m
        };
    }
}
