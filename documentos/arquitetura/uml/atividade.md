# UML | Diagrama de Atividade

> Mostra o **fluxo de execução** completo do HTTP Request até a persistência assíncrona via RabbitMQ.

---

## Fluxo de Lançamento (Crédito ou Débito)

```mermaid
flowchart TD
  A([HTTP Request chega no Endpoint]) --> B[Endpoint desserializa JSON em Command]
  B --> C{Validator inline:<br/>dados válidos?}

  C -- não --> D1[Coleta erros FluentValidation]
  D1 --> D2[Retorna BaseResponse com lista de erros]
  D2 --> Z400([HTTP 400 Bad Request])

  C -- sim --> E[Monta TransacaoMessage<br/>com CorrelationId e CriadoEm]
  E --> F[IRabbitMqPublisher.PublicarAsync<br/>AMQPS — fluxodecaixa.queue]
  F --> G([HTTP 200 OK — resposta imediata])

  F -.->|assíncrono| H

  subgraph Consumer[FluxoDeCaixaMainConsumer — BackgroundService]
    H[Recebe mensagem da fila<br/>BasicDeliver + deliveryTag]
    H --> I{Deserializa<br/>TransacaoMessage OK?}
    I -- não --> I1[Log Error — BasicNack requeue]
    I -- sim --> J{TipoOperacao?}
    J -- CREDITO --> K1[INSERT FluxoDeCaixaCredito]
    J -- DEBITO --> K2[INSERT FluxoDeCaixaDebito]
    K1 & K2 --> L[MERGE UPSERT FluxoDeCaixaConsolidado]
    L --> M{Processamento OK?}
    M -- sim --> N[BasicAck — mensagem confirmada]
    M -- não --> O{Tentativas esgotadas?}
    O -- não --> P[BasicNack requeue — nova tentativa]
    O -- sim --> Q[BasicReject — vai para DLQ]
  end

  Q -.->|DLX route| R

  subgraph DLQ[FluxoDeCaixaDlqConsumer — BackgroundService]
    R[Recebe mensagem da DLQ]
    R --> S[Segunda tentativa de persistência<br/>INSERT + UPSERT]
    S --> T{OK?}
    T -- sim --> U[BasicAck]
    T -- não --> V[Log Error — BasicAck sem requeue<br/>evita loop infinito]
  end

  K1 & K2 & L --> DB[(SQL Server<br/>FluxoDeCaixa +<br/>FluxoDeCaixaConsolidado)]
```

---

## Fluxo de Consulta de Relatório (Cache Redis + SQL)

```mermaid
flowchart TD
  A2([GET /Relatorio?inicio=X&fim=Y]) --> B2[Monta chave de cache<br/>relatorio:inicio:fim]
  B2 --> C2{Redis cache HIT?}

  C2 -- sim --> D2[Desserializa JSON do cache]
  D2 --> Z200a([HTTP 200 OK — sub-milisegundo])

  C2 -- não --> E2[mediator.Send - GetRelatorioQuery]
  E2 --> F2{ValidationBehaviour:<br/>Inicio valido, Fim maior que Inicio?}

  F2 -- não --> G2[throw ValidationExceptionCustom]
  G2 --> G3[ValidationMiddleware captura]
  G3 --> Z400b([HTTP 400 Bad Request])

  F2 -- sim --> H2[LoggingBehaviour loga Request]
  H2 --> I2[PerformanceBehaviour timer.Start]
  I2 --> J2[GetRelatorioQueryHandler]
  J2 --> K2[SELECT FluxoDeCaixaConsolidado<br/>WHERE dataFC BETWEEN inicio AND fim]
  K2 --> DB2[(SQL Server<br/>FluxoDeCaixaConsolidado)]
  DB2 --> L2[Monta BaseResponse com DTOs]
  L2 --> M2[PerformanceBehaviour timer.Stop]
  M2 --> N2{Tempo maior que 10ms?}
  N2 -- sim --> O2[LogWarning Long Running]
  N2 -- não --> P2[continua]
  O2 --> P2
  P2 --> Q2[LoggingBehaviour loga Response]
  Q2 --> R2{Periodo encerrado?<br/>fim menor que hoje}
  R2 -- sim --> S2a[SetStringAsync TTL 365 dias<br/>dado imutavel — periodo passado]
  R2 -- não --> S2b[SetStringAsync TTL ate meia-noite UTC<br/>periodo atual — pode receber lancamentos]
  S2a & S2b --> T2([HTTP 200 OK])
```

---

## Estados do pipeline | Lançamento

| Etapa | Componente | O que faz | O que falha |
|---|---|---|---|
| 1 | Endpoint | Desserializa Command, valida inline | JSON inválido → 400 do framework |
| 2 | FluentValidation | Valida regras de negócio (data, valor, descrição) | Lista de erros → 400 com BaseResponse |
| 3 | IRabbitMqPublisher | Publica TransacaoMessage no broker AMQPS | Conexão RabbitMQ falhou → 500 / retry no publisher |
| 4 | Endpoint | Retorna 200 imediatamente | — |
| 5 | FluxoDeCaixaMainConsumer | Consome fila; INSERT + UPSERT | Nack+requeue até MaxRetries → DLQ |
| 6 | FluxoDeCaixaDlqConsumer | Segunda tentativa; sem requeue | Log Error; Ack (descarta) — evita loop |

## Estados do pipeline | Relatório

| Etapa | Componente | O que faz | O que falha |
|---|---|---|---|
| 1 | Endpoint | Verifica Redis | Cache corrompido → desserialização falha → 200 com erro interno |
| 2 | Redis HIT | Retorna dado do cache | — |
| 3 | ValidationBehaviour | Valida datas da query | `ValidationExceptionCustom` → 400 |
| 4 | LoggingBehaviour | Loga payload | Nunca falha (só side-effect) |
| 5 | PerformanceBehaviour | Cronometra | Nunca falha |
| 6 | GetRelatorioQueryHandler | SELECT no consolidado | SqlException capturada → BaseResponse com mensagem de erro |
| 7 | Endpoint | Popula Redis com TTL inteligente | Redis indisponível → responde sem cachear |

> **Princípio:** cada camada faz **uma coisa**. Validar é responsabilidade do Behaviour; logar é responsabilidade do Behaviour; **handler só conhece negócio**; o endpoint só orquestra e delega.
