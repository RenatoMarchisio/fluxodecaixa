# C4 — Nível 4: Código

> Detalhamento das **classes-chave** e fluxos de execução atualizados. O código completo está em `src/`.

---

## 1. Fluxo de Lançamento (Pipeline assíncrono com RabbitMQ)

Sequência para `POST /api/FluxoDeCaixa/InsertCredito`:

```
Cliente → Gateway (YARP) → Endpoint → Validator → RabbitMqPublisher → [200 OK imediato]
                                                                           ↓
                                               RabbitMQ (fluxodecaixa.queue)
                                                                           ↓
                                           FluxoDeCaixaMainConsumer (BackgroundService)
                                                    ↓
                                         INSERT FluxoDeCaixa
                                         MERGE  FluxoDeCaixaConsolidado (UPSERT)
```

### 1.1 Endpoint — Publicação no RabbitMQ

```csharp
group.MapPost("/InsertCredito", async (
    IRabbitMqPublisher publisher,
    IValidator<CreateFluxoDeCaixaCreditoCommand> validator,
    CreateFluxoDeCaixaCreditoCommand command,
    CancellationToken ct) =>
{
    if (command is null) return Results.BadRequest();

    var validacao = await validator.ValidateAsync(command, ct);
    if (!validacao.IsValid)
    {
        var erros = validacao.Errors.Select(e =>
            new BaseError { PropertyMessage = e.PropertyName, ErrorMessage = e.ErrorMessage });
        return Results.BadRequest(new BaseResponse<bool> { Message = "Dados inválidos", Errors = erros });
    }

    var mensagem = new TransacaoMessage(
        DataFc:        command.dataFC,
        Descricao:     command.descricao,
        Credito:       command.credito,
        Debito:        null,
        TipoOperacao:  "CREDITO",
        CorrelationId: Guid.NewGuid(),
        CriadoEm:      DateTime.UtcNow);

    await publisher.PublicarAsync(mensagem, ct);

    return Results.Ok(new BaseResponse<bool>
    {
        Data = true, succcess = true,
        Message = "Caixa Credito realizado com sucesso."
    });
});
```

**Pontos-chave:**
- Validação FluentValidation **inline** (sem MediatR neste ponto — simples e rápido).
- `TransacaoMessage` é um **record imutável** com `CorrelationId` para rastreabilidade.
- `PublicarAsync` é fire-and-forget assíncrono — o endpoint **não aguarda** o consumo.

---

### 1.2 Consumer Principal — `FluxoDeCaixaMainConsumer`

```csharp
protected override async Task ProcessarAsync(
    TransacaoMessage mensagem, IServiceScope scope, CancellationToken ct)
{
    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFluxoDeCaixa>();

    if (mensagem.TipoOperacao == "CREDITO")
    {
        var entidade = new FluxoDeCaixaCredito
        {
            ID        = Guid.NewGuid(),
            dataFC    = mensagem.DataFc,
            descricao = mensagem.Descricao,
            credito   = mensagem.Credito ?? 0m
        };

        await uow.FluxoDeCaixaCredito.InsertAsync(entidade);
        await uow.FluxoDeCaixaConsolidado.UpsertAsync(mensagem.DataFc, mensagem.Credito ?? 0m, 0m);
    }
    else if (mensagem.TipoOperacao == "DEBITO")
    {
        // ... idem para Débito
    }
    else throw new InvalidOperationException($"TipoOperacao desconhecido: '{mensagem.TipoOperacao}'");
}
```

**Pontos-chave:**
- Executa dentro de um `IServiceScope` — repositórios `Scoped` por mensagem.
- Chama INSERT + UPSERT na mesma unidade lógica.
- Exceção → `RabbitMqConsumerBase` faz `Nack + requeue`; após MaxRetries → `Reject` (→ DLQ automático pelo broker).

---

### 1.3 UPSERT — `FluxoDeCaixaConsolidadoRepository`

```csharp
public async Task UpsertAsync(DateOnly dataFc, decimal credito, decimal debito)
{
    using var connection = _ctx.CreateConnection();
    const string sql = @"
        MERGE [dbo].[FluxoDeCaixaConsolidado] AS target
        USING (SELECT @dataFc AS dataFC, @credito AS credito, @debito AS debito) AS source
          ON target.dataFC = source.dataFC
        WHEN MATCHED THEN
            UPDATE SET
                credito   = target.credito + source.credito,
                debito    = target.debito  + source.debito,
                criadoEm  = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT (dataFC, credito, debito, criadoEm)
            VALUES (source.dataFC, source.credito, source.debito, SYSUTCDATETIME());";

    await connection.ExecuteAsync(sql, new { dataFc, credito, debito });
}
```

**Pontos-chave:**
- `MERGE` é atômico — sem race condition entre consumidores paralelos.
- Acumula valores: crédito total = crédito anterior + novo crédito.
- Garante que `FluxoDeCaixaConsolidado` sempre reflita o saldo real por data.

---

## 2. Fluxo de Consulta de Relatório (Cache + SQL)

```
Cliente → Gateway → Endpoint → IDistributedCache (Redis)
                                   ↓ HIT: retorna DTO direto
                                   ↓ MISS:
                               IMediator.Send(Query)
                               → ValidationBehaviour
                               → LoggingBehaviour
                               → PerformanceBehaviour
                               → GetRelatorioQueryHandler
                               → FluxoDeCaixaRelatorioRepository
                               → SQL Server (FluxoDeCaixaConsolidado)
                               → popula Redis com TTL inteligente
                               → retorna DTO
```

### 2.1 Endpoint com Cache-on-First-Hit

```csharp
group.MapGet("/Relatorio", async (
    IMediator mediator, IDistributedCache cache,
    [FromQuery] DateTime inicio, [FromQuery] DateTime fim,
    CancellationToken ct) =>
{
    var cacheKey = $"relatorio:{inicio:yyyy-MM-dd}:{fim:yyyy-MM-dd}";

    var cachedJson = await cache.GetStringAsync(cacheKey, ct);
    if (cachedJson is not null)
    {
        var cachedData = JsonSerializer.Deserialize<
            BaseResponse<IEnumerable<FluxoDeCaixaRelatorioDto>>>(cachedJson);
        return cachedData is not null ? Results.Ok(cachedData) : Results.Ok(/* erro desserialização */);
    }

    // Cache MISS → SQL via CQRS
    var response = await mediator.Send(
        new GetRelatorioByDateFCInicioFimQuery { Inicio = inicio, Fim = fim }, ct);

    if (response.succcess)
    {
        // TTL inteligente
        var agora  = DateTime.UtcNow;
        var fimUtc = fim.Date.ToUniversalTime();

        DistributedCacheEntryOptions cacheOptions = fimUtc < agora.Date
            ? new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) }  // dado imutável
            : new() { AbsoluteExpiration = agora.Date.AddDays(1).ToUniversalTime() }; // até meia-noite

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions, ct);
    }

    return response.succcess ? Results.Ok(response) : Results.BadRequest(response);
});
```

---

## 3. Consumer DLQ — `FluxoDeCaixaDlqConsumer`

```csharp
public sealed class FluxoDeCaixaDlqConsumer : RabbitMqConsumerBase
{
    protected override string GetQueueName() => "fluxodecaixa.queue.dlq";
    protected override bool RequeueOnError => false; // sem loop infinito

    protected override async Task ProcessarAsync(
        TransacaoMessage mensagem, IServiceScope scope, CancellationToken ct)
    {
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFluxoDeCaixa>();

        if (mensagem.TipoOperacao == "CREDITO")
        {
            var entidade = new FluxoDeCaixaCredito
            {
                ID        = Uuid7.NewGuid(),     // UUIDv7 (timestamp embutido)
                dataFC    = mensagem.DataFc,
                descricao = mensagem.Descricao,
                credito   = mensagem.Credito ?? 0m
            };
            await uow.FluxoDeCaixaCredito.InsertAsync(entidade);
            await uow.FluxoDeCaixaConsolidado.UpsertAsync(mensagem.DataFc, mensagem.Credito ?? 0m, 0m);
        }
        // ... idem DEBITO
    }
}
```

**Pontos-chave:**
- `RequeueOnError = false` → em falha, a mensagem é descartada (apenas log). Sem risco de loop.
- Mesma lógica de persistência do consumer principal — a segunda chance é idêntica.
- Projeto **separado** do Lançamentos: falha no DLQ não afeta o consumo da fila principal.

---

## 4. Pipeline de Comportamentos (cross-cutting)

### 4.1 `ValidationBehaviour<TRequest,TResponse>`

```csharp
public async Task<TResponse> Handle(
    TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
    if (_validators.Any())
    {
        var context  = new ValidationContext<TRequest>(request);
        var results  = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.Where(r => r.Errors.Any())
                              .SelectMany(r => r.Errors)
                              .Select(e => new BaseError {
                                  PropertyMessage = e.PropertyName,
                                  ErrorMessage    = e.ErrorMessage })
                              .ToList();
        if (failures.Any())
            throw new ValidationExceptionCustom(failures);
    }
    return await next();
}
```

### 4.2 `PerformanceBehaviour` — limiar de 10 ms (ajustável)

```csharp
_timer.Start();
var response = await next();
_timer.Stop();
if (_timer.ElapsedMilliseconds > 10)
    _logger.LogWarning("Long Running: {name} ({ms}ms) {@req}",
        typeof(TRequest).Name, _timer.ElapsedMilliseconds, request);
return response;
```

### 4.3 `LoggingBehaviour`

```csharp
_logger.LogInformation("Request: {name} {@request}", typeof(TRequest).Name, request);
var response = await next();
_logger.LogInformation("Response: {name} {@response}", typeof(TResponse).Name, response);
return response;
```

---

## 5. DI — Registro completo dos serviços

### `FluxoDeCaixa.WebApi/Program.cs`

```csharp
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<FluxoDeCaixaMainConsumer>();
```

### `FluxoDeCaixaRelatorio.WebApi/Program.cs`

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration  = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName   = "fluxodecaixa:";
});
```

### `Application.UseCases/ConfigureServices.cs`

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
    cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});
services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
services.AddSingleton (typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
services.AddSingleton (typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
```

### `Persistence/ConfigureServices.cs`

```csharp
services.AddSingleton<DapperContextFC>();
services.AddScoped<IFluxoDeCaixaCreditoRepository,      FluxoDeCaixaCreditoRepository>();
services.AddScoped<IFluxoDeCaixaDebitoRepository,       FluxoDeCaixaDebitoRepository>();
services.AddScoped<IFluxoDeCaixaConsolidadoRepository,  FluxoDeCaixaConsolidadoRepository>();
services.AddScoped<IFluxoDeCaixaRelatorioRepository,    FluxoDeCaixaRelatorioRepository>();
services.AddScoped<IUnitOfWorkFluxoDeCaixa,             UnitOfWorkFluxoDeCaixa>();
```
