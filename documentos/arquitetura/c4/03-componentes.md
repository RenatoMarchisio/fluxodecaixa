# C4 | Nível 3: Componentes

> Cada container é "aberto" para mostrar seus **componentes lógicos internos** (assemblies/módulos .NET) e como eles colaboram.

---

## 1. Componentes do **Serviço de Lançamentos** (`FluxoDeCaixa.WebApi`)

```mermaid
flowchart TB
  HTTP([HTTP / JSON]) --> EP

  subgraph WL[FluxoDeCaixa.WebApi]
    direction TB
    EP[FluxoDeCaixaEndpoints<br/><i>POST /InsertCredito, /InsertDebito</i>]
    MW[ValidationMiddleware<br/><i>traduz exceções → 400</i>]
    PUB[IRabbitMqPublisher<br/><i>RabbitMqPublisher — Singleton</i>]
    CON[FluxoDeCaixaMainConsumer<br/><i>BackgroundService — consume fila principal</i>]
    EP --> MW
    EP --> PUB
    CON --> UOW
  end

  PUB -- AMQPS publish --> MQ[(RabbitMQ<br/>fluxodecaixa.queue)]
  MQ  -- consume --> CON

  subgraph UC[Application.UseCases]
    direction TB
    VAL[FluentValidation<br/>CreateFluxoDeCaixaCredito/DebitoValidator]
  end

  subgraph PS[Persistence]
    direction TB
    UOW[IUnitOfWorkFluxoDeCaixa]
    REPO_C[FluxoDeCaixaCreditoRepository<br/><i>INSERT FluxoDeCaixa</i>]
    REPO_D[FluxoDeCaixaDebitoRepository<br/><i>INSERT FluxoDeCaixa</i>]
    REPO_CONS[FluxoDeCaixaConsolidadoRepository<br/><i>MERGE UPSERT FluxoDeCaixaConsolidado</i>]
    CTX[DapperContextFC<br/><i>SqlConnection factory</i>]
    UOW --> REPO_C & REPO_D & REPO_CONS --> CTX
  end

  EP --> VAL
  CTX --> DB[(SQL Server)]

  classDef api  fill:#1168bd,stroke:#fff,color:#fff;
  classDef app  fill:#0d3a82,stroke:#fff,color:#fff;
  classDef mq   fill:#d97706,stroke:#fff,color:#fff;
  classDef inf  fill:#475569,stroke:#fff,color:#fff;
  classDef db   fill:#16a34a,stroke:#fff,color:#fff;
  class EP,MW api;
  class PUB,CON,VAL app;
  class UOW,REPO_C,REPO_D,REPO_CONS,CTX inf;
  class DB db;
```

### Tabela de componentes — Lançamentos

| Componente | Camada | Responsabilidade |
|---|---|---|
| `FluxoDeCaixaEndpoints` | Apresentação | Mapeia `POST /api/FluxoDeCaixa/InsertCredito` e `/InsertDebito`. Valida inline com `IValidator<T>` e **publica `TransacaoMessage` no RabbitMQ** via `IRabbitMqPublisher`. Retorna 200 imediatamente. |
| `ValidationMiddleware` | Apresentação | Captura `ValidationExceptionCustom` e responde 400. |
| `IRabbitMqPublisher` / `RabbitMqPublisher` | Infraestrutura (Singleton) | Publica `TransacaoMessage` serializado em JSON na fila `fluxodecaixa.queue` via AMQPS. |
| `FluxoDeCaixaMainConsumer` | Infraestrutura (BackgroundService) | Consome a fila principal; decodifica `TransacaoMessage`; chama `IUnitOfWorkFluxoDeCaixa` para INSERT e UPSERT. Em caso de falha após MaxRetries → Nack → DLQ. |
| `CreateFluxoDeCaixaCreditoValidator` / `...DebitoValidator` | Aplicação | Regras FluentValidation: data 2020–2030, descrição 1–255 chars, valor ≥ 1. |
| `IUnitOfWorkFluxoDeCaixa` | Aplicação (interface) | Agrega todos os repositórios — ponto único de acesso ao Persistence. |
| `FluxoDeCaixaCreditoRepository` / `...DebitoRepository` | Infraestrutura | `INSERT INTO [dbo].[FluxoDeCaixa]` com Dapper framework ORM de alta desempenho |
| `FluxoDeCaixaConsolidadoRepository` | Infraestrutura | **`MERGE` (UPSERT)** atômico em `[dbo].[FluxoDeCaixaConsolidado]` — acumula crédito e débito por data sem race-condition. |
| `DapperContextFC` | Infraestrutura | Fábrica de `SqlConnection` (Singleton). |
| `DateOnlyTypeHandler` | Infraestrutura | Mapeia `DateOnly` ↔ `DATE` SQL Server. |

---

## 2. Componentes do **Consumer DLQ** (`FluxoDeCaixa.DLQ`)

```mermaid
flowchart TB
  MQ[(RabbitMQ<br/>fluxodecaixa.queue.dlq)] -- consume --> DLQC

  subgraph DLQP[FluxoDeCaixa.DLQ]
    direction TB
    BASE[RabbitMqConsumerBase<br/><i>BackgroundService abstrato</i>]
    DLQC[FluxoDeCaixaDlqConsumer<br/><i>RequeueOnError = false</i>]
    BASE --> DLQC
    DLQC --> UOW2
  end

  subgraph PS2[Persistence]
    direction TB
    UOW2[IUnitOfWorkFluxoDeCaixa]
    R2[FluxoDeCaixaCreditoRepository<br/>FluxoDeCaixaDebitoRepository<br/>FluxoDeCaixaConsolidadoRepository]
    UOW2 --> R2
  end

  R2 --> DB2[(SQL Server)]

  classDef dlq  fill:#7c3aed,stroke:#fff,color:#fff;
  classDef inf  fill:#475569,stroke:#fff,color:#fff;
  classDef db   fill:#16a34a,stroke:#fff,color:#fff;
  class BASE,DLQC dlq;
  class UOW2,R2 inf;
  class DB2 db;
```

### Tabela de componentes — DLQ

| Componente | Camada | Responsabilidade |
|---|---|---|
| `RabbitMqConsumerBase` | Infraestrutura (abstrato) | `BackgroundService` genérico; gerencia conexão AMQPS, `BasicQos`, loop de consumo, ack/nack. |
| `FluxoDeCaixaDlqConsumer` | Infraestrutura | Herda de `RabbitMqConsumerBase`; consome `fluxodecaixa.queue.dlq`; `RequeueOnError = false` — em falha apenas loga, **nunca devolve à fila** (evita loop). Persiste via `IUnitOfWorkFluxoDeCaixa`. |

---

## 3. Componentes do **Serviço de Relatório** (`FluxoDeCaixaRelatorio.WebApi`)

```mermaid
flowchart TB
  HTTP([HTTP / JSON]) --> EP

  subgraph WR[FluxoDeCaixaRelatorio.WebApi]
    direction TB
    EP[FluxoDeCaixaRelatorioEndpoints<br/><i>GET /Relatorio?inicio=&fim=</i>]
    MW[ValidationMiddleware]
    EP --> MW
  end

  EP --> REDIS[(Redis<br/>IDistributedCache)]
  REDIS -- cache HIT --> EP

  MW --> MED

  subgraph UC[Application.UseCases]
    direction TB
    MED[IMediator.Send]
    PL{{Pipeline Behaviours<br/>Validation / Logging / Performance}}
    QH[GetRelatorioByDateFCInicioFim<br/>QueryHandler]
    MED --> PL --> QH
  end

  QH --> RR[FluxoDeCaixaRelatorioRepository]
  RR --> CTX[DapperContextFC]
  CTX --> DB[(SQL Server<br/>FluxoDeCaixaConsolidado)]

  classDef api   fill:#1168bd,stroke:#fff,color:#fff;
  classDef cache fill:#0891b2,stroke:#fff,color:#fff;
  classDef app   fill:#0d3a82,stroke:#fff,color:#fff;
  classDef inf   fill:#475569,stroke:#fff,color:#fff;
  classDef db    fill:#16a34a,stroke:#fff,color:#fff;
  class EP,MW api;
  class REDIS cache;
  class MED,PL,QH app;
  class RR,CTX inf;
  class DB db;
```

### Tabela de componentes — Relatório

| Componente | Camada | Responsabilidade |
|---|---|---|
| `FluxoDeCaixaRelatorioEndpoints` | Apresentação | `GET /api/FluxoDeCaixaRelatorio/Relatorio?inicio=&fim=`. Verifica Redis antes de acionar MediatR. Se cache HIT → retorna sem tocar no SQL. Se MISS → MediatR → SQL → popula cache com TTL inteligente. |
| `IDistributedCache` (Redis) | Infraestrutura | Cache-on-First-Hit; chave = `relatorio:{inicio}:{fim}`; TTL longo para datas passadas; TTL até meia-noite para período atual. |
| `GetFluxoDeCaixaRelatorioByInicioFimValidator` | Aplicação | `Inicio` obrigatório; `Fim > Inicio`. |
| `GetRelatorioByDateFCInicioFimQueryHandler` | Aplicação | Chama `FluxoDeCaixaRelatorioRepository`, monta `BaseResponse`. |
| `FluxoDeCaixaRelatorioRepository` | Infraestrutura | `SELECT * FROM FluxoDeCaixaConsolidado WHERE dataFC BETWEEN @inicio AND @fim` — range-scan no índice clustered. |

---

## 4. Componentes do **API Gateway** (`FluxoDeCaixa.Gateway`)

```mermaid
flowchart LR
  Cli([Cliente]) --> Kestrel
  subgraph GW[FluxoDeCaixa.Gateway]
    direction TB
    Kestrel[Kestrel<br/>HTTPS:5000]
    YARP[YARP ReverseProxy<br/>mainApiCluster / relatorioApiCluster]
    Swagger[Swagger UI<br/>agregado]
    Kestrel --> Swagger
    Kestrel --> YARP
  end
  YARP -- /api/FluxoDeCaixa/* --> SVCL[Lançamentos:8000]
  YARP -- /api/FluxoDeCaixaRelatorio/* --> SVCR[Relatório:8500]
  classDef gw fill:#dc2626,stroke:#fff,color:#fff;
  class Kestrel,YARP,Swagger gw;
```

| Rota | Cluster | Destino |
|---|---|---|
| `/fluxodecaixa/swagger/{**catch-all}` | `mainApiCluster` | `http://lancamentos:8000` |
| `/relatorio/swagger/{**catch-all}` | `relatorioApiCluster` | `http://relatorio:8500` |
| `/api/FluxoDeCaixaRelatorio/{**catch-all}` | `relatorioApiCluster` | `http://relatorio:8500` |
| `/{**catch-all}` *(order=200)* | `mainApiCluster` | `http://lancamentos:8000` |

---

## 5. Assemblies compartilhados

| Assembly | Quem usa | O que provê |
|---|---|---|
| `FluxoDeCaixa.Domain` | Lançamentos, Relatório, DLQ | Entidades + Eventos de domínio |
| `FluxoDeCaixa.Application.Dto` | Lançamentos, Relatório | DTOs de leitura |
| `FluxoDeCaixa.Application.Interface` | Lançamentos, Relatório, DLQ | Contratos de Repositórios e UoW |
| `FluxoDeCaixa.Application.UseCases` | Lançamentos, Relatório | Commands, Queries, Handlers, Behaviours, Validators |
| `FluxoDeCaixa.Persistence` | Lançamentos, Relatório, DLQ | Implementação Dapper de Repositórios + UoW (inclui `FluxoDeCaixaConsolidadoRepository`) |
| `FluxoDeCaixa.Infrastructure` | Lançamentos, DLQ | `RabbitMqPublisher`, `RabbitMqConsumerBase`, `RabbitMqSettings`, `TransacaoMessage`, `DateOnlyTypeHandler` |

---

## 6. Suíte de Testes (`FluxoDeCaixa.Tests`)

```mermaid
flowchart LR
  subgraph TT[FluxoDeCaixa.Tests — xUnit + FluentAssertions + NSubstitute]
    direction TB
    V[Application/Validators<br/>CreateCreditoValidatorTests<br/>CreateDebitoValidatorTests<br/>GetRelatorioValidatorTests]
    H[Application/Handlers<br/>CreateCreditoHandlerTests<br/>CreateDebitoHandlerTests<br/>GetRelatorioHandlerTests]
    P[Persistence<br/>CreditoRepositoryTests<br/>DebitoRepositoryTests<br/>ConsolidadoRepositoryTests<br/>RelatorioRepositoryTests]
    MQT[Infrastructure<br/>RabbitMqPublisherTests]
  end

  P --> SQLS[(SQL Server<br/>Testcontainers)]
  MQT --> MQS[(RabbitMQ<br/>Testcontainers)]
```

| Grupo | Framework | Cobertura |
|---|---|---|
| Validators | xUnit + FluentAssertions | Regras de negócio (data, valor, descrição) |
| Handlers | xUnit + NSubstitute (mocks) | Fluxo de comando/query sem dependências reais |
| Repositories | xUnit + Testcontainers (SQL Server) | INSERT, UPSERT, SELECT em banco efêmero |
| RabbitMq Publisher | xUnit + Testcontainers (RabbitMQ) | Publicação e consumo end-to-end |
