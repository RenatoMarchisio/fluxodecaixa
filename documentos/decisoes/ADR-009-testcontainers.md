# ADR-009 | Estratégia de Testes com xUnit, NSubstitute e Testcontainers

- **Status:** Aceita
- **Data:** 2026-04-27
- **Decisores:** Tech Lead
- **Tags:** testes, qualidade, testcontainers, xunit

## Contexto

O projeto usa Dapper com SQL Server e RabbitMQ como infraestruturas críticas. Testes unitários com mocks de repositórios não cobrem o risco de bugs em queries SQL ou na configuração do broker. Ao mesmo tempo, testes de integração com banco e broker reais dificultam a execução em CI/CD (estado compartilhado, limpeza manual, etc.).

## Opções consideradas

| Abordagem | Pró | Contra |
|---|---|---|
| **Apenas testes unitários com mocks** | Rápidos, sem infra | Não detecta bugs em SQL, MERGE, conexão RabbitMQ |
| **Testes de integração com banco real** | Detecta bugs reais | Estado compartilhado; difícil em CI; custo |
| **Testcontainers (escolhida)** | Banco/broker efêmeros por teste; CI-friendly; sem estado compartilhado | Requer Docker no ambiente de CI |

## Decisão

**Estratégia em camadas** com `FluxoDeCaixa.Tests`:

### Camada 1 — Testes unitários (sem I/O)
- **Validators** (`CreateFluxoDeCaixaCreditoValidatorTests`, `...DebitoValidatorTests`, `GetRelatorioValidatorTests`): FluentAssertions para verificar mensagens e códigos de erro.
- **Handlers** (`CreateCreditoHandlerTests`, `CreateDebitoHandlerTests`, `GetRelatorioHandlerTests`): NSubstitute para mockar `IUnitOfWorkFluxoDeCaixa` e `IMapper`; verifica orquestração do handler sem I/O.

### Camada 2 — Testes de integração com Testcontainers
- **Repositories** (`FluxoDeCaixaCreditoRepositoryTests`, `...DebitoRepositoryTests`, `FluxoDeCaixaConsolidadoRepositoryTests`, `FluxoDeCaixaRelatorioRepositoryTests`): container SQL Server efêmero (`SqlServerFixture` com `IClassFixture`); aplica DDL do schema antes dos testes; verifica INSERT, UPSERT (MERGE), SELECT com dados reais.
- **RabbitMQ Publisher** (`RabbitMqPublisherTests`): container RabbitMQ efêmero (`RabbitMqFixture` com `ICollectionFixture`); verifica publicação e consumo de `TransacaoMessage` end-to-end.

### Fixtures compartilhadas
- `DatabaseCollection` (`[CollectionDefinition]`) → `SqlServerFixture` compartilhado entre testes de repositório.
- `RabbitMqCollection` → `RabbitMqFixture` compartilhado entre testes de mensageria.

## Pacotes

| Pacote | Versão | Uso |
|---|---|---|
| `xunit` | 2.x | Framework de testes |
| `FluentAssertions` | 6.x | Asserções expressivas |
| `NSubstitute` | 5.x | Mocks de interfaces |
| `Testcontainers.MsSql` | 3.x | Container SQL Server efêmero |
| `Testcontainers.RabbitMq` | 3.x | Container RabbitMQ efêmero |
| `Microsoft.NET.Test.Sdk` | — | Runner xUnit |

## Consequências

### Positivas
- Detecta bugs reais em queries Dapper (MERGE, BETWEEN, DateOnly mapping).
- CI/CD pode rodar `dotnet test` sem pré-configurar banco ou broker (Testcontainers cuida disso).
- Handlers são testados de forma isolada (NSubstitute) sem acoplamento a infra.
- Fixtures com `IClassFixture` / `ICollectionFixture` reutilizam containers entre testes do mesmo grupo → performance.

### Negativas / Mitigações
- **Docker obrigatório em CI**: padrão em GitHub Actions / Azure DevOps. Mitigado com imagem do runner que já inclui Docker.
- **Tempo de startup dos containers** (~10-30s para SQL Server): mitigado com `IClassFixture` (1 container por classe, não por teste).
