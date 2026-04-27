# UML | Diagrama de Classes

Representa as classes principais do **Domínio** e da **Aplicação** com seus relacionamentos.

```mermaid
classDiagram
  direction LR

  class FluxoDeCaixaBase {
    <<abstract>>
    +Guid ID
    +DateOnly dataFC
    +string descricao
  }
  class FluxoDeCaixaCredito {
    +decimal credito
  }
  class FluxoDeCaixaDebito {
    +decimal debito
  }
  class FluxoDeCaixaRelatorio {
    +DateOnly dataFC
    +decimal credito
    +decimal debito
    +DateTime criadoEm
  }
  FluxoDeCaixaBase <|-- FluxoDeCaixaCredito
  FluxoDeCaixaBase <|-- FluxoDeCaixaDebito

  class CreateFluxoDeCaixaBaseCommand {
    <<abstract, IRequest~BaseResponse~bool~~>>
    +Guid ID  ⟵ UUIDv7
    +DateOnly dataFC
    +string descricao
  }
  class CreateFluxoDeCaixaCreditoCommand {
    +decimal credito
  }
  class CreateFluxoDeCaixaDebitoCommand {
    +decimal debito
  }
  CreateFluxoDeCaixaBaseCommand <|-- CreateFluxoDeCaixaCreditoCommand
  CreateFluxoDeCaixaBaseCommand <|-- CreateFluxoDeCaixaDebitoCommand

  class GetRelatorioByDateFCInicioFimQuery {
    <<IRequest~BaseResponse~IEnumerable~Dto~~~>>
    +DateTime Inicio
    +DateTime Fim
  }

  class IUnitOfWorkFluxoDeCaixa {
    <<interface>>
    +IFluxoDeCaixaCreditoRepository FluxoDeCaixaCredito
    +IFluxoDeCaixaDebitoRepository  FluxoDeCaixaDebito
    +IFluxoDeCaixaRelatorioRepository FluxoDeCaixaRelatorio
  }
  class IGenericRepository~T~ {
    <<interface>>
    +InsertAsync(T) Task~bool~
    +UpdateAsync(T) Task~bool~
    +DeleteAsync(string) Task~bool~
    +GetAsync(string) Task~T~
    +GetAllAsync() Task~IEnumerable~T~~
  }
  class IFluxoDeCaixaCreditoRepository {
    <<interface>>
  }
  class IFluxoDeCaixaDebitoRepository {
    <<interface>>
  }
  class IFluxoDeCaixaRelatorioRepository {
    <<interface>>
    +GetFluxoDeCaixaRelatorioAsync(DateTime, DateTime) Task~IEnumerable~Dto~~
  }
  IGenericRepository <|-- IFluxoDeCaixaCreditoRepository
  IGenericRepository <|-- IFluxoDeCaixaDebitoRepository
  IGenericRepository <|-- IFluxoDeCaixaRelatorioRepository

  class FluxoDeCaixaCreditoRepository
  class FluxoDeCaixaDebitoRepository
  class FluxoDeCaixaRelatorioRepository
  IFluxoDeCaixaCreditoRepository    <|.. FluxoDeCaixaCreditoRepository
  IFluxoDeCaixaDebitoRepository     <|.. FluxoDeCaixaDebitoRepository
  IFluxoDeCaixaRelatorioRepository  <|.. FluxoDeCaixaRelatorioRepository

  class DapperContextFC {
    +CreateConnection() IDbConnection
  }
  FluxoDeCaixaCreditoRepository    --> DapperContextFC
  FluxoDeCaixaDebitoRepository     --> DapperContextFC
  FluxoDeCaixaRelatorioRepository  --> DapperContextFC

  class CreateFluxoDeCaixaCreditoHandler {
    -IUnitOfWorkFluxoDeCaixa _uow
    -IMapper _mapper
    +Handle(cmd) Task~BaseResponse~bool~~
  }
  class GetRelatorioByDateFCInicioFimQueryHandler {
    -IUnitOfWorkFluxoDeCaixa _uow
    +Handle(qry) Task~BaseResponse~IEnumerable~~
  }
  CreateFluxoDeCaixaCreditoHandler ..> CreateFluxoDeCaixaCreditoCommand
  GetRelatorioByDateFCInicioFimQueryHandler ..> GetRelatorioByDateFCInicioFimQuery
  CreateFluxoDeCaixaCreditoHandler --> IUnitOfWorkFluxoDeCaixa
  GetRelatorioByDateFCInicioFimQueryHandler --> IUnitOfWorkFluxoDeCaixa

  class ValidationBehaviour {
    <<IPipelineBehavior~T,R~>>
  }
  class LoggingBehaviour {
    <<IPipelineBehavior~T,R~>>
  }
  class PerformanceBehaviour {
    <<IPipelineBehavior~T,R~>>
  }

  class BaseResponse~T~ {
    +bool succcess
    +T? Data
    +string? Message
    +IEnumerable~BaseError~? Errors
  }
```

## Pontos a destacar

- **Herança** em `FluxoDeCaixaBase` (Credito/Debito) facilita a criação de validators e mappings comuns sem duplicação.
- **Comandos espelham as entidades** mas com **`ID` gerado server-side** (UUIDv7) controle total sobre o identificador, evitando IDs maliciosos do cliente.
- **`IGenericRepository<T>`** dá CRUD básico; cada repositório especializado adiciona regras específicas (ex.: `GetFluxoDeCaixaRelatorioAsync(inicio, fim)`).
- **`IUnitOfWorkFluxoDeCaixa`** agrega os repositórios futuramente pode envelopar uma transação SQL (`BeginTransaction` / `Commit` / `Rollback`).
- **Handlers dependem só de interfaces** (UoW, IMapper) testáveis com `Moq`/`NSubstitute`.
- **`BaseResponse<T>`** é o **envelope padrão** das APIs desacopla o domínio do shape HTTP.
