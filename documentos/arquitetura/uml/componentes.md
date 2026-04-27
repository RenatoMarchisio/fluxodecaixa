# UML — Diagrama de Componentes

> Mapa de assemblies (.dll) da solução e suas dependências de compilação.

```mermaid
flowchart TB
  classDef ui   fill:#1168bd,stroke:#fff,color:#fff;
  classDef gw   fill:#dc2626,stroke:#fff,color:#fff;
  classDef app  fill:#0d3a82,stroke:#fff,color:#fff;
  classDef inf  fill:#475569,stroke:#fff,color:#fff;
  classDef dom  fill:#a16207,stroke:#fff,color:#fff;
  classDef dto  fill:#9333ea,stroke:#fff,color:#fff;

  GW[FluxoDeCaixa.Gateway<br/><i>YARP</i>]:::gw
  WL[FluxoDeCaixa.WebApi<br/><i>Lançamentos</i>]:::ui
  WR[FluxoDeCaixaRelatorio.WebApi<br/><i>Relatório</i>]:::ui

  UC[FluxoDeCaixa.Application.UseCases<br/><i>Commands, Queries, Handlers, Behaviours, Validators, Mappers</i>]:::app
  II[FluxoDeCaixa.Application.Interface<br/><i>IRepository, IUnitOfWork</i>]:::app
  DTO[FluxoDeCaixa.Application.Dto<br/><i>DTOs</i>]:::dto

  DOM[FluxoDeCaixa.Domain<br/><i>Entities, Events</i>]:::dom

  PS[FluxoDeCaixa.Persistence<br/><i>Repositories, UoW, DapperContext</i>]:::inf
  INF[FluxoDeCaixa.Infrastructure<br/><i>Dapper TypeHandlers</i>]:::inf

  WL --> UC
  WL --> PS
  WL --> INF
  WR --> UC
  WR --> PS
  WR --> INF

  UC --> DTO
  UC --> II

  II --> DOM
  II --> DTO

  PS --> II
  PS --> INF
```

## Pacotes externos (NuGet) usados

| Assembly | Pacotes principais |
|---|---|
| `FluxoDeCaixa.Application.UseCases` | `MediatR 14`, `AutoMapper 16`, `FluentValidation 11`, `Medo.Uuid7 3` |
| `FluxoDeCaixa.Persistence` | `Dapper 2.1`, `System.Data.SqlClient 4.8` |
| `FluxoDeCaixa.Infrastructure` | `Dapper 2.1` |
| `FluxoDeCaixa.WebApi` / `FluxoDeCaixaRelatorio.WebApi` | `Swashbuckle.AspNetCore 6.6`, `Microsoft.AspNetCore.OpenApi 8` |
| `FluxoDeCaixa.Gateway` | `Yarp.ReverseProxy 2.3`, `Swashbuckle.AspNetCore 6.6` |

## Regras de dependência (Clean Architecture)

✅ Permitido:
- `Domain` ← qualquer um (núcleo é referenciado por todos).
- `Application.UseCases` referencia `Application.Interface`, `Dto` e `Domain`.
- `Persistence` referencia `Application.Interface` e `Infrastructure`.
- `WebApi/*` referencia `Application.UseCases`, `Persistence` e `Infrastructure` (apenas para `AddInjection*`).

❌ Proibido:
- `Domain` referenciar qualquer outra camada.
- `Application.UseCases` referenciar `Persistence` ou `WebApi`.
- `Persistence` referenciar `Application.UseCases`.

> Estas regras podem ser **forçadas em build** com `NetArchTest` ou `ArchUnitNET` (recomendado adicionar no roadmap de testes).
