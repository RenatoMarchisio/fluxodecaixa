# ADR-007 — Mensageria Assíncrona com RabbitMQ (CloudAMQP)

- **Status:** Aceita
- **Data:** 2026-04-27
- **Decisores:** Arquiteto Corporativo, Tech Lead
- **Tags:** mensageria, resiliência, async, rabbitmq

## Contexto

Com a arquitetura de microsserviços (ADR-001), o serviço de Lançamentos precisa persistir transações de forma resiliente e desacoplada do SQL Server. Em picos de carga, um INSERT síncrono bloqueante aumenta a latência percebida pelo comerciante e cria dependência direta da disponibilidade do banco.

Além disso, o requisito de **disponibilidade independente** entre escrita e leitura reforça a necessidade de desacoplar o caminho de escrita do banco de dados.

## Opções consideradas

| Solução | Pró | Contra |
|---|---|---|
| **INSERT síncrono direto** | Simples, transacional | Latência exposta ao cliente; queda do SQL = queda do endpoint |
| **RabbitMQ (escolhida)** | Desacopla escrita do SQL; endpoint responde imediatamente; fila absorve picos | Complexidade operacional; mensagens podem chegar fora de ordem (mitigado por `DateOnly`) |
| **Azure Service Bus** | Gerenciado, SLA 99,9% | Custo por operação; vendor lock-in Azure |
| **Outbox Pattern (EF Core + SQL)** | ACID com o banco | Requer polling ou CDC; aumenta acoplamento ao SQL |

## Decisão

**RabbitMQ via CloudAMQP (AMQPS)** com as seguintes regras:

1. **Publicação**: `IRabbitMqPublisher` (Singleton) publica `TransacaoMessage` (record imutável JSON) na fila `fluxodecaixa.queue`. O endpoint HTTP retorna 200 imediatamente após o publish.
2. **Consumo**: `FluxoDeCaixaMainConsumer` (BackgroundService) consome com `BasicQos(prefetchCount:1)` e **ack manual** — garante at-least-once delivery.
3. **Dead-Letter Exchange**: fila `fluxodecaixa.queue` configurada com `x-dead-letter-exchange=fluxodecaixa.dlx`. Mensagens rejeitadas após `MaxRetries` são redirecionadas automaticamente para `fluxodecaixa.queue.dlq`.
4. **Consumer DLQ**: processo separado (`FluxoDeCaixa.DLQ`) consome a DLQ com `RequeueOnError=false` — segunda chance de persistência sem loop infinito.
5. **Persistência de mensagens**: `persistent=true` no publish — mensagens sobrevivem a restart do broker.

## Consequências

### Positivas
- Endpoint de Lançamentos responde em <10ms independente do estado do SQL.
- Fila absorve picos de escrita sem sobrecarregar o banco.
- DLQ garante que nenhum lançamento seja perdido silenciosamente.
- `CorrelationId` em cada mensagem permite rastreamento end-to-end.

### Negativas / Mitigações
- **At-least-once delivery** (possível duplicata em retry) → mitigado com `MERGE` (UPSERT) no consolidado e `ID` único por entidade.
- **Complexidade operacional** → mitigada com CloudAMQP gerenciado (sem cluster próprio).
- **Ordem não garantida** → não é requisito; `DateOnly` no registro é o determinante, não a ordem de chegada.

## Configuração relevante (`appsettings.json`)

```json
{
  "RabbitMQ": {
    "ConnectionString": "amqps://user:pass@moose.rmq.cloudamqp.com/vhost",
    "QueueName": "fluxodecaixa.queue",
    "DeadLetterQueueName": "fluxodecaixa.queue.dlq",
    "MaxRetries": 3
  }
}
```
