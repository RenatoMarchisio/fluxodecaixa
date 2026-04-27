# Roadmap & Evoluções Futuras

> Trabalho **não incluído** nesta versão da solução, organizado por horizonte e valor de negócio.

---

## 1. Horizonte 0 (próximos 30 dias) Hardening de produção

| # | Item | Esforço | Valor |
|---|---|---|---|
| 1 | **Health checks** `/healthz` e `/healthz/ready` (com `SqlServerHealthCheck`) | 1 dia | Habilita rolling deploy zero-downtime |
| 2 | **JWT/OIDC** real no Gateway + `[Authorize]` nas rotas | 3 dias | Atende RNF-05 (segurança) |
| 3 | **Rate Limit** built-in `services.AddRateLimiter()` | 1 dia | Protege contra DoS / clients abusivos |
| 4 | **Headers de segurança** (HSTS, CSP, X-Frame, etc.) | 1 dia | Compliance OWASP |
| 5 | **CorrelationId middleware** (propaga `X-Correlation-Id`) | 1 dia | Debug em ambiente distribuído |
| 6 | **Suite de testes** xUnit + FluentAssertions + Testcontainers | 5 dias | Confiança para refactor |
| 7 | **CI/CD pipeline** GitHub Actions (build + test + image + deploy) | 3 dias | Frequência de release |
| 8 | **DbUp / DACPAC** para migrations versionadas | 2 dias | Schema sob controle |

---

## 2. Horizonte 1 (próximos 90 dias) Performance & Observabilidade

| # | Item | Esforço | Valor |
|---|---|---|---|
| 9 | **Cache Redis** no caminho do Relatório (TTL = endOfDay(fim)) | 5 dias | Atende 50 rps com folga de 100× |
| 10 | **OpenTelemetry** → App Insights / Tempo / Jaeger (logs + métricas + traces) | 5 dias | Visibilidade end-to-end |
| 11 | **Polly Circuit Breaker + Retry** entre Gateway → microsserviços | 3 dias | Resiliência |
| 12 | **Dashboards Grafana** (4 dashboards do `observabilidade.md` §6) | 5 dias | Operação eficiente |
| 13 | **Outbox Pattern** (Lançamentos publica eventos consolidação) | 8 dias | Eventual consistency confiável |
| 14 | **Job de consolidação assíncrono** (BackgroundService + Service Bus) | 5 dias | Substitui SQL manual |
| 15 | **Load test** com k6 (cenário 50 rps, 30 min) | 3 dias | Validação contínua de RNF-02 |

---

## 3. Horizonte 2 (próximos 6 meses) Escala & Funcionalidades

| # | Item | Esforço | Valor |
|---|---|---|---|
| 16 | **Update / Delete** de lançamento (soft-delete, com auditoria) | 8 dias | Atende RF-04, RF-05 |
| 17 | **Categorização** (centro de custo, projeto, tags) | 10 dias | Habilita relatórios analíticos |
| 18 | **Relatórios multi-período** (semanal/mensal/anual) | 5 dias | Atende CN-02.2 |
| 19 | **Exportação** CSV/Excel/PDF | 5 dias | Operação financeira tradicional |
| 20 | **Multi-tenant** (column `TenantId` + claim no JWT) | 15 dias | Vender como SaaS |
| 21 | **Banco de leitura separado** (read replica do Azure SQL ou banco dedicado) | 8 dias | CQRS físico |
| 22 | **Front-end** (SPA Blazor / React) consumindo o Gateway | 25 dias | UX para o comerciante |
| 23 | **App Mobile** (.NET MAUI / Flutter) | 30 dias | Captura de lançamentos no balcão |

---

## 4. Horizonte 3 (>6 meses) Visão estratégica

| # | Item | Por quê |
|---|---|---|
| 24 | **Event Sourcing** (com snapshots) | Auditoria total + capacidade de "rewind" |
| 25 | **Machine Learning** sobre o consolidado: previsão de fluxo de caixa, detecção de anomalia | Diferencial competitivo |
| 26 | **Open Banking integration** | Conciliação automática com bancos via API |
| 27 | **Multi-região active-active** (Azure Cosmos + Service Bus geo) | RPO=0, RTO<10s; mercados internacionais |
| 28 | **API pública** com plano de monetização (rate limit por SKU + Apigee/APIM) | Receita complementar via parceiros |
| 29 | **Marketplace de plugins** (regras fiscais, integrações verticais) | Ecossistema |

---

## 5. Decisões a revisitar

| Decisão atual | Quando reconsiderar |
|---|---|
| Banco compartilhado | Quando os schemas divergirem ou quando o time for partido em squads sem coordenação |
| Assemblies compartilhados | Quando a regra de negócio dos dois serviços divergir significativamente |
| Dapper | Se a equipe for nova em SQL ou o domínio crescer para 50+ entidades — avaliar EF Core |
| YARP | Se virar API pública com 1000+ clientes B2B → considerar APIM (gestão de planos/SDKs) |
| SQL Server | Se padrões de leitura analítica dominarem → considerar replicar para um data lake (Synapse / BigQuery) |

---

## 6. Critérios para classificar uma issue como roadmap

Uma demanda entra no roadmap se atender pelo menos **um** dos critérios:
- Toca em **arquitetura** (não é só feature CRUD).
- Tem **valor de negócio quantificável** (ROI estimado).
- Reduz risco operacional / custo / compliance.

E sai do roadmap se:
- Tem dependência hard de tech debt → primeiro pagar a dívida.
- O custo é alto e o ROI < 1 ano de payback.
- Existe alternativa SaaS que resolve com menor TCO.
