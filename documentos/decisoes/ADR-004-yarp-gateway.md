# ADR-004 — API Gateway com YARP (Microsoft)

- **Status:** Aceita
- **Data:** 2026-04-26
- **Tags:** gateway, integração, segurança

## Contexto

Decidido em ADR-001 ter um Gateway. Precisamos escolher **qual**.

## Opções consideradas

| Solução | Pró | Contra |
|---|---|---|
| **YARP** (Yet Another Reverse Proxy) — Microsoft | ⭐ Stack 100% .NET; alto desempenho (Kestrel); config JSON simples; abre porta para extensões C# via `IPipelineHandler`; usado em produção pela própria Microsoft | Mais novo; menor ecossistema de plugins prontos |
| **Ocelot** | Maduro, popular em .NET | Performance inferior; manutenção mais lenta; documentação datada |
| **NGINX** | Battle-tested; rica em features | NGINX proxy + WAF, estratégia para dimensionamento horizontal --> YARP + OAuth2(JWT) segurança |
| **Kong / Tyk / Apigee** | Plataformas completas (analytics, marketplace, AuthZ) | Custo alto; complexidade desnecessária para 2 serviços |
| **Traefik** | Auto-discovery (K8s/Docker labels) | Não cobre cenários complexos de transformação de payload |

## Decisão

**YARP 2.3** rodando como microsserviço dedicado em .NET 8.

### Configuração atual (`appsettings.json` resumido)

```jsonc
"ReverseProxy": {
  "Clusters": {
    "mainApiCluster":      { "Destinations": { "d": { "Address": "http://lancamentos:8000" } } },
    "relatorioApiCluster": { "Destinations": { "d": { "Address": "http://relatorio:8500"   } } }
  },
  "Routes": {
    "mainApiSwaggerRoute":      { "ClusterId": "mainApiCluster",      "Match": { "Path": "/fluxodecaixa/swagger/{**catch-all}" }, "Transforms": [{"PathRemovePrefix":"/fluxodecaixa"}] },
    "relatorioApiSwaggerRoute": { "ClusterId": "relatorioApiCluster", "Match": { "Path": "/relatorio/swagger/{**catch-all}"     }, "Transforms": [{"PathRemovePrefix":"/relatorio"}] },
    "relatorioApiRoute":        { "ClusterId": "relatorioApiCluster", "Match": { "Path": "/api/FluxoDeCaixaRelatorio/{**catch-all}" } },
    "mainApiRoute":             { "ClusterId": "mainApiCluster",      "Order": 200, "Match": { "Path": "/{**catch-all}" } }
  }
}
```

## Hooks futuros (sem alterar microsserviços)

| Capacidade | Como adicionar |
|---|---|
| **JWT / OIDC** | `services.AddAuthentication().AddJwtBearer(...)` no Gateway + `[Authorize]` em rotas |
| **Rate limit** | `AspNetCoreRateLimit` (lib NuGet) ou `services.AddRateLimiter(...)` (built-in .NET 8) |
| **Circuit breaker** | `Polly` no `HttpClient` injetado no YARP destino |
| **CORS** | Já configurado; refinar `AllowAnyOrigin` para origens específicas em produção |
| **Header propagation** | YARP `Transforms` — propagar `traceparent`, `Authorization`, `X-Forwarded-For` |
| **mTLS** entre Gateway e microsserviços | YARP `HttpClient` com certificado de cliente |
| **Métricas Prometheus** | `prometheus-net.AspNetCore` middleware no Kestrel |

## Consequências

- ✅ Adicionar uma feature no Gateway é **uma classe C#**, não um plugin de outra plataforma.
- ✅ Testabilidade alta — Gateway é uma WebApi como outra qualquer.
- ✅ Mesmo time de back-end (.NET) consegue evoluir.
- ⚠️ Single point of failure — mitigado com **2+ réplicas** atrás de Load Balancer/Front Door.
- ⚠️ Cuidado para não virar **monólito de cross-cutting** — manter "magro": só roteamento, auth, rate-limit, observabilidade.
