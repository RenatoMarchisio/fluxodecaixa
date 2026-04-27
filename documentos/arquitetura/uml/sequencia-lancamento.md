# UML — Diagrama de Sequência: Lançar Crédito (Pipeline Assíncrono RabbitMQ)

> Fluxo completo de `POST /api/FluxoDeCaixa/InsertCredito` — do Gateway à persistência final via broker.

---

## Fase 1 — Publicação (síncrona, rápida)

```mermaid
sequenceDiagram
  autonumber
  actor C as Cliente (Comerciante)
  participant GW  as API Gateway (YARP)
  participant EP  as FluxoDeCaixaEndpoints
  participant VAL as IValidator (FluentValidation)
  participant PUB as IRabbitMqPublisher
  participant MQ  as RabbitMQ<br/>(fluxodecaixa.queue)

  C->>GW:   POST /api/FluxoDeCaixa/InsertCredito {dataFC, descricao, credito}
  GW->>EP:  HTTP forward (mainApiCluster)
  EP->>VAL: ValidateAsync(command)

  alt Validação falhou
    VAL-->>EP: ValidationResult com erros
    EP-->>GW:  HTTP 400 { Message, Errors }
    GW-->>C:   400 Bad Request
  else Validação OK
    VAL-->>EP: IsValid = true
    EP->>PUB:  PublicarAsync(TransacaoMessage{TipoOperacao=CREDITO, CorrelationId, CriadoEm})
    PUB->>MQ:  BasicPublish (AMQPS, persistent=true)
    MQ-->>PUB: (broker ACK)
    PUB-->>EP: Task completed
    EP-->>GW:  HTTP 200 { success=true, message="Caixa Credito realizado com sucesso." }
    GW-->>C:   200 OK (imediato — sem aguardar persistência)
  end
```

---

## Fase 2 — Consumo e Persistência (assíncrona, BackgroundService)

```mermaid
sequenceDiagram
  autonumber
  participant MQ   as RabbitMQ<br/>(fluxodecaixa.queue)
  participant CON  as FluxoDeCaixaMainConsumer<br/>(BackgroundService)
  participant UOW  as IUnitOfWorkFluxoDeCaixa
  participant REPC as FluxoDeCaixaCreditoRepository
  participant CONS as FluxoDeCaixaConsolidadoRepository
  participant DB   as SQL Server

  MQ->>CON:   Received (BasicDeliver, deliveryTag)
  CON->>CON:  Deserializa TransacaoMessage (JSON)

  alt ProcessarAsync OK
    CON->>UOW:   GetRequiredService<IUnitOfWorkFluxoDeCaixa>
    CON->>REPC:  InsertAsync(FluxoDeCaixaCredito)
    REPC->>DB:   INSERT INTO [dbo].[FluxoDeCaixa] VALUES (@ID, @dataFC, @credito, @descricao, ...)
    DB-->>REPC:  1 row affected
    REPC-->>CON: done
    CON->>CONS:  UpsertAsync(dataFc, credito, 0m)
    CONS->>DB:   MERGE [dbo].[FluxoDeCaixaConsolidado] USING ... WHEN MATCHED UPDATE ... WHEN NOT MATCHED INSERT
    DB-->>CONS:  OK
    CONS-->>CON: done
    CON->>MQ:    BasicAck(deliveryTag)
  else Erro (ex.: SQL fora do ar) — até MaxRetries
    CON->>MQ:    BasicNack(deliveryTag, requeue=true)
    MQ->>CON:    Re-entrega mensagem
    Note over MQ,CON: após MaxRetries...
    CON->>MQ:    BasicReject(deliveryTag, requeue=false)
    MQ->>MQ:     Dead-Letter Exchange → fluxodecaixa.queue.dlq
  end
```

---

## Fase 3 — Dead Letter Queue (fallback, projeto FluxoDeCaixa.DLQ)

```mermaid
sequenceDiagram
  autonumber
  participant MQ2  as RabbitMQ<br/>(fluxodecaixa.queue.dlq)
  participant DLQC as FluxoDeCaixaDlqConsumer
  participant UOW2 as IUnitOfWorkFluxoDeCaixa
  participant DB2  as SQL Server

  MQ2->>DLQC: Received (mensagem rejeitada da fila principal)
  DLQC->>DLQC: Deserializa TransacaoMessage

  alt Persistência OK
    DLQC->>UOW2: INSERT + UPSERT (mesmo fluxo do consumer principal)
    UOW2->>DB2:  INSERT FluxoDeCaixa + MERGE FluxoDeCaixaConsolidado
    DB2-->>DLQC: OK
    DLQC->>MQ2:  BasicAck
  else Erro persistência DLQ
    DLQC->>DLQC: LogError — sem requeue (RequeueOnError=false)
    DLQC->>MQ2:  BasicAck (descarta — evita loop infinito)
    Note over DLQC: Operador deve investigar logs
  end
```

---

## Comentários de design

- **Resposta imediata ao cliente** — o comerciante recebe confirmação em milissegundos, independente do estado do banco.
- **Resiliência** — fila absorve picos de escrita; banco pode ser restaurado sem perder lançamentos.
- **DLQ como rede de segurança** — nenhum lançamento é perdido silenciosamente; há sempre uma segunda tentativa e rastro de log.
- **RequeueOnError = false no DLQ** — previne loop infinito de mensagens venenosas ("poison messages").
- **CorrelationId** em cada `TransacaoMessage` permite rastrear o ciclo de vida fim-a-fim no sistema de observabilidade.
