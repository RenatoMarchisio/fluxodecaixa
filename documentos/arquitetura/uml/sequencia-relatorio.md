# UML — Diagrama de Sequência: Consultar Relatório Consolidado (Cache Redis + SQL)

> Fluxo de `GET /api/FluxoDeCaixaRelatorio/Relatorio?inicio=&fim=` com estratégia Cache-on-First-Hit.

---

## Caminho com Cache HIT (Redis)

```mermaid
sequenceDiagram
  autonumber
  actor C as Cliente
  participant GW    as API Gateway (YARP)
  participant EP    as FluxoDeCaixaRelatorioEndpoints
  participant REDIS as IDistributedCache (Redis)

  C->>GW:    GET /api/FluxoDeCaixaRelatorio/Relatorio?inicio=2026-01-01&fim=2026-01-31
  GW->>EP:   HTTP forward (relatorioApiCluster)
  EP->>REDIS: GetStringAsync("relatorio:2026-01-01:2026-01-31")
  REDIS-->>EP: cachedJson (not null)
  EP->>EP:   JsonSerializer.Deserialize<BaseResponse<IEnumerable<DTO>>>
  EP-->>GW:  HTTP 200 { data: [...], success: true }
  GW-->>C:   200 OK (sem tocar no SQL — sub-milisegundo)
```

---

## Caminho com Cache MISS (Redis → SQL → popula cache)

```mermaid
sequenceDiagram
  autonumber
  actor C as Cliente
  participant GW    as API Gateway (YARP)
  participant EP    as FluxoDeCaixaRelatorioEndpoints
  participant REDIS as IDistributedCache (Redis)
  participant MD    as ValidationMiddleware
  participant ME    as IMediator (MediatR)
  participant VB    as ValidationBehaviour
  participant LB    as LoggingBehaviour
  participant PB    as PerformanceBehaviour
  participant QH    as GetRelatorioQueryHandler
  participant RR    as FluxoDeCaixaRelatorioRepository
  participant DB    as SQL Server<br/>(FluxoDeCaixaConsolidado)

  C->>GW:    GET /api/FluxoDeCaixaRelatorio/Relatorio?inicio=2026-01-01&fim=2026-01-31
  GW->>EP:   HTTP forward (relatorioApiCluster)
  EP->>REDIS: GetStringAsync(cacheKey)
  REDIS-->>EP: null (MISS)

  EP->>MD:   passa para pipeline MediatR
  MD->>ME:   mediator.Send(GetRelatorioByDateFCInicioFimQuery)
  ME->>VB:   Handle (next)
  VB->>VB:   ValidateAsync { Inicio != null, Fim > Inicio }

  alt Validação falhou
    VB-->>MD:  throw ValidationExceptionCustom
    MD-->>EP:  catch → 400 { errors }
    EP-->>GW:  HTTP 400
    GW-->>C:   400 Bad Request
  else Validação OK
    VB->>LB:   next()
    LB->>LB:   LogInformation "Request: GetRelatorio..."
    LB->>PB:   next()
    PB->>PB:   timer.Start()
    PB->>QH:   next()
    QH->>RR:   GetFluxoDeCaixaRelatorioAsync(inicio, fim)
    RR->>DB:   SELECT * FROM FluxoDeCaixaConsolidado WHERE dataFC BETWEEN @inicio AND @fim
    DB-->>RR:  IEnumerable<FluxoDeCaixaRelatorioDto>
    RR-->>QH:  dtos
    QH->>QH:   monta BaseResponse { success=true, data=dtos }
    QH-->>PB:  BaseResponse
    PB->>PB:   timer.Stop() — LogWarning se >10ms
    PB-->>LB:  BaseResponse
    LB->>LB:   LogInformation "Response: ..."
    LB-->>VB:  BaseResponse
    VB-->>ME:  BaseResponse
    ME-->>EP:  BaseResponse

    EP->>EP:   Calcula TTL inteligente
    Note right of EP: fim < hoje → TTL 365 dias (imutável)<br/>fim >= hoje → TTL até meia-noite UTC (pode mudar)
    EP->>REDIS: SetStringAsync(cacheKey, json, cacheOptions)
    EP-->>GW:  HTTP 200 { data: [...], success: true }
    GW-->>C:   200 OK
  end
```

---

## TTL Inteligente — lógica de decisão

```mermaid
flowchart TD
  A[fim da consulta] --> B{fim.Date < hoje UTC?}
  B -- Sim<br/>período passado --> C[TTL = 365 dias<br/>dado imutável]
  B -- Não<br/>período inclui hoje --> D[TTL = meia-noite UTC de hoje<br/>pode receber novos lançamentos]
  C --> E[SetStringAsync com AbsoluteExpirationRelativeToNow]
  D --> F[SetStringAsync com AbsoluteExpiration]
```

---

## Comentários de design

- **Cache HIT** não acessa MediatR nem SQL — latência ≤ 1ms (Redis em rede local).
- **>99% de hits** após warmup — aniquila o pico de 50 req/s sobre o SQL Server.
- **Chave de cache** inclui intervalo de datas → granularidade fina; consultas diferentes têm caches independentes.
- **TTL de 365 dias** para períodos fechados: dado financeiro de um dia passado não muda (sem edição de lançamentos nesta versão).
- **TTL até meia-noite** para o dia atual: novos lançamentos chegam via RabbitMQ e são consolidados via UPSERT durante o dia — o cache "expira" ao virar o dia e o próximo request popula com dados atualizados.
- O `PerformanceBehaviour` detecta queries lentas (>10ms) mesmo com Dapper — útil para monitorar degradação do SQL Server.
