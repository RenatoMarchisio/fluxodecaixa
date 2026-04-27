# C4 — Nível 2: Containers

> Cada caixa abaixo é um **processo executável** ou **datastore** (um "container" no sentido C4 — não confundir com container Docker, embora coincidam aqui).

---

## 1. Diagrama de Containers

```mermaid
flowchart TB
  Cli([Cliente<br/>Browser / Mobile / POS])

  subgraph FC[Sistema Fluxo de Caixa]
    direction TB

    GW["<b>API Gateway</b><br/>FluxoDeCaixa.Gateway<br/>.NET 8 + YARP + OAuth2<br/>:5000 HTTPS"]:::gw

    SVCL["<b>Serviço de Lançamentos</b><br/>FluxoDeCaixa.WebApi<br/>.NET 8 Minimal API + BackgroundService<br/>:8000"]:::svc

    SVCR["<b>Serviço de Relatório</b><br/>FluxoDeCaixaRelatorio.WebApi<br/>.NET 8 Minimal API<br/>:8500"]:::svc

    DLQ["<b>Consumer DLQ</b><br/>FluxoDeCaixa.DLQ<br/>.NET 8 BackgroundService<br/>(sem porta HTTP)"]:::dlq

    MQ[(<b>RabbitMQ</b><br/>CloudAMQP AMQPS<br/>fluxodecaixa.queue<br/>fluxodecaixa.queue.dlq)]:::mq

    REDIS[(<b>Redis</b><br/>Cache distribuído<br/>TTL inteligente por janela de data)]:::cache

    DB[(<b>SQL Server</b><br/>FluxoCaixa<br/>FluxoDeCaixa<br/>FluxoDeCaixaConsolidado)]:::db
  end

  Cli       -- HTTPS / JSON --> GW
  GW        -- HTTP / JSON  --> SVCL
  GW        -- HTTP / JSON  --> SVCR
  SVCL      -- AMQPS publish --> MQ
  MQ        -- consume fila principal --> SVCL
  MQ        -- DLX route --> DLQ
  SVCL      -- INSERT + UPSERT --> DB
  DLQ       -- INSERT + UPSERT --> DB
  SVCR      -- SELECT consolidado --> DB
  SVCR      <--> REDIS

  classDef gw    fill:#dc2626,stroke:#fff,color:#fff;
  classDef svc   fill:#1168bd,stroke:#fff,color:#fff;
  classDef dlq   fill:#7c3aed,stroke:#fff,color:#fff;
  classDef mq    fill:#d97706,stroke:#fff,color:#fff;
  classDef cache fill:#0891b2,stroke:#fff,color:#fff;
  classDef db    fill:#16a34a,stroke:#fff,color:#fff;
```

---

## 2. Tabela de Containers

| Container | Responsabilidade | Tecnologia | Porta | Imagem Docker |
|---|---|---|---|---|
| **API Gateway** | Roteamento HTTP, agregação de Swagger, ponto único de entrada. Base para hardening futuro (JWT, rate-limit, circuit-breaker). | .NET 8 + YARP 2.3 | 5000 | `fluxodecaixa/gateway` |
| **Serviço de Lançamentos** | (1) Valida e **publica** lançamentos no RabbitMQ via `IRabbitMqPublisher`; (2) `FluxoDeCaixaMainConsumer` (BackgroundService) consome a fila → INSERT em `FluxoDeCaixa` + UPSERT em `FluxoDeCaixaConsolidado`. | .NET 8 + MediatR + Dapper + RabbitMQ.Client | 8000 | `fluxodecaixa/lancamentos` |
| **Serviço de Relatório** | `GET /Relatorio` com **Cache-on-First-Hit Redis** (TTL: 365d para passado, meia-noite para período atual). Cache MISS → MediatR → SQL. | .NET 8 + MediatR + Dapper + StackExchange.Redis | 8500 | `fluxodecaixa/relatorio` |
| **Consumer DLQ** | Consome `fluxodecaixa.queue.dlq`; tenta persistir o lançamento sem requeue (evita loop infinito). Separação de processo garante auditabilidade independente. | .NET 8 BackgroundService + RabbitMQ.Client + Dapper | — | `fluxodecaixa/dlq` |
| **RabbitMQ** | Broker de mensagens; Dead-Letter Exchange (`fluxodecaixa.dlx`) redireciona mensagens rejeitadas após MaxRetries para a DLQ automaticamente. | CloudAMQP AMQPS | 5672 / 15672 | `rabbitmq:3.13-management` |
| **Redis** | Cache distribuído para o endpoint de Relatório. Estratégia de TTL inteligente baseada na janela de datas da consulta. | StackExchange.Redis 2.7 | 6379 | `redis:7-alpine` |
| **SQL Server** | Persistência durável; índice clustered por `ID` (FluxoDeCaixa) e `dataFC DESC` (FluxoDeCaixaConsolidado — range-scan). | SQL Server 2022 | 1433 | `mcr.microsoft.com/mssql/server:2022-latest` |

---

## 3. Comunicação entre containers

| De → Para | Protocolo | Sincronia | Comentário |
|---|---|---|---|
| Cliente → Gateway | HTTPS / JSON | Síncrona | TLS terminado no Gateway |
| Gateway → Lançamentos | HTTP/1.1 + JSON | Síncrona | Reverse-proxy YARP — mainApiCluster |
| Gateway → Relatório | HTTP/1.1 + JSON | Síncrona | Reverse-proxy YARP — relatorioApiCluster |
| Lançamentos → RabbitMQ | AMQPS (publish) | **Assíncrona** | `IRabbitMqPublisher.PublicarAsync` → resposta HTTP imediata |
| RabbitMQ → Lançamentos | AMQPS (consume) | **Assíncrona** | `FluxoDeCaixaMainConsumer` — `BasicQos(prefetchCount:1)` + ack manual |
| RabbitMQ → DLQ | AMQPS (DLX route) | **Assíncrona** | Rejeição após MaxRetries → Dead-Letter Exchange → `fluxodecaixa.queue.dlq` |
| DLQ → SQL | TDS (1433) | Síncrona | Dapper / `SqlConnection` — sem requeue em caso de falha |
| Lançamentos → SQL | TDS (1433) | Síncrona | Dapper / `SqlConnection` — INSERT + MERGE UPSERT |
| Relatório → SQL | TDS (1433) | Síncrona | Dapper — apenas em cache MISS |
| Relatório ↔ Redis | TCP (6379) | Síncrona | `IDistributedCache.GetStringAsync / SetStringAsync` |
| Lançamentos ↔ Relatório | **NENHUMA** | — | **Garantia de isolamento de falhas** |

---

## 4. Por que esta arquitetura?

### 4.1 Fluxo assíncrono via RabbitMQ
O endpoint de Lançamentos responde **imediatamente** após publicar no broker — o cliente não aguarda INSERT no SQL. Se o banco cair, as mensagens permanecem na fila; ao recuperar, o consumer drena sem perda. A **DLQ separada** garante segunda tentativa de persistência com rastreabilidade e **sem loop infinito** (`RequeueOnError = false`).

### 4.2 Cache Redis no Relatório
Dados de períodos passados são **imutáveis** → TTL de 365 dias (quase permanente).
Período atual pode receber novos lançamentos → TTL até meia-noite UTC.
Isso elimina o pico de **50 req/s** sobre o SQL Server — >99% dos requests servidos do cache após o primeiro hit.

### 4.3 API Gateway como container dedicado
Ponto único para clientes externos. Onde adicionar cross-cutting concerns futuros: rate-limit (`AspNetCoreRateLimit`), circuit-breaker (Polly), autenticação OIDC, correlação W3C `traceparent`.

### 4.4 SQL Server com read model pré-agregada
`FluxoDeCaixaConsolidado` é atualizada via **MERGE atômico** (UPSERT) a cada lançamento consumido — dados do relatório sempre consistentes sem re-agregação por query.

---

## 5. Volumes & Persistência

| Volume | Container | Conteúdo | Estratégia |
|---|---|---|---|
| `sqldata` | SQL Server | Datafiles do SQL | Volume nomeado; em produção: Azure SQL / RDS |
| Logs | Cada serviço | stdout/stderr | Coletor externo (ELK / Loki / CloudWatch) |

---

## 6. Configuração injetada (env-vars)

| Variável | Container | Exemplo |
|---|---|---|
| `ASPNETCORE_URLS` | Lançamentos / Relatório / Gateway | `http://+:8000` |
| `ConnectionStrings__FluxoDeCaixaConnection` | Lançamentos / Relatório / DLQ | `Server=sqlserver,1433;Database=FluxoCaixa;...` |
| `RabbitMQ__ConnectionString` | Lançamentos / DLQ | `amqps://user:pass@moose.rmq.cloudamqp.com/vhost` |
| `RabbitMQ__QueueName` | Lançamentos | `fluxodecaixa.queue` |
| `RabbitMQ__DeadLetterQueueName` | DLQ | `fluxodecaixa.queue.dlq` |
| `Redis__ConnectionString` | Relatório | `redis:6379` |
| `ASPNETCORE_ENVIRONMENT` | Todos | `Production` |
| `ReverseProxy__Clusters__mainApiCluster__...` | Gateway | `http://lancamentos:8000` |
| `ReverseProxy__Clusters__relatorioApiCluster__...` | Gateway | `http://relatorio:8500` |
