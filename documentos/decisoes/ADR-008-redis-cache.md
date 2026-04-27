# ADR-008 | Cache Distribuído Redis com TTL Inteligente (Relatório)

- **Status:** Aceita
- **Data:** 2026-04-27
- **Decisores:** Arquiteto Corporativo, Tech Lead
- **Tags:** cache, redis, performance, read-heavy

## Contexto

O Serviço de Relatório precisa absorver **50 req/s** com no máximo 5% de perda. A query `SELECT * FROM FluxoDeCaixaConsolidado WHERE dataFC BETWEEN @inicio AND @fim` é eficiente (range-scan no índice clustered), mas mesmo uma query rápida, sob 50 req/s contínuos, coloca pressão desnecessária no SQL Server que também recebe inserções do consumer RabbitMQ.

A natureza dos dados financeiros consolidados é de **imutabilidade por período fechado**: um dia passado não recebe mais lançamentos (nesta versão).

## Opções consideradas

| Solução | Pró | Contra |
|---|---|---|
| **Sem cache** | Zero complexidade | SQL Server sob pressão constante; risco de >5% de perda em pico |
| **Cache em memória (MemoryCache)** | Simples | Não compartilhado entre réplicas; invalida ao reiniciar |
| **Redis distribuído (escolhida)** | Compartilhado entre réplicas; TTL preciso; persistence opcional | Infra adicional |
| **CDN (edge cache)** | Latência ínfima | Não adequado para dados dinâmicos com autenticação futura |

## Decisão

**Redis com estratégia Cache-on-First-Hit** via `IDistributedCache` (StackExchange.Redis):

### TTL Inteligente
```
se fim < hoje UTC  →  TTL = 365 dias  (dado imutável — período fechado)
se fim >= hoje UTC →  TTL = meia-noite UTC de hoje  (período pode receber novos lançamentos)
```

### Chave de cache
```
relatorio:{inicio:yyyy-MM-dd}:{fim:yyyy-MM-dd}
```
Cada combinação de intervalo tem seu próprio cache — sem colisões.

### Fluxo
1. `GetStringAsync(cacheKey)` → HIT: deserializa e retorna sem tocar no SQL.
2. MISS: executa MediatR → SQL → serializa response → `SetStringAsync(cacheKey, json, options)`.

## Consequências

### Positivas
- >99% de cache hits após warmup → SQL Server praticamente não recebe leitura para períodos passados.
- Latência em cache HIT < 1ms (Redis em rede local ou mesmo AZ).
- TTL inteligente garante consistência: período atual expira na meia-noite e é recarregado com dados frescos do dia.
- Escala horizontal do Relatório sem degradação do SQL.

### Negativas / Mitigações
- **Cache stale durante o dia corrente**: novos lançamentos são processados pelo consumer, mas o cache do dia atual só expira à meia-noite. Mitigação: TTL até meia-noite garante que no máximo até o início do dia seguinte o dado está fresco. Para necessidade de real-time, pode-se reduzir o TTL (ex: 5 minutos).
- **Infra adicional (Redis)**: mitigada com Redis gerenciado (Azure Cache / ElastiCache) ou container leve em dev.

## Configuração relevante (`appsettings.json`)

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName  = "fluxodecaixa:";
});
```
