# Requisitos — Funcionais e Não-Funcionais (refinados)

> A partir do enunciado do desafio, refinamos os requisitos abaixo com critérios de aceitação testáveis.

---

## 1. Requisitos Funcionais

### RF-01 — Registrar lançamento de **crédito**
**Descrição**: O sistema deve permitir registrar uma entrada de caixa associada a uma data e descrição.
**Endpoint**: `POST /api/FluxoDeCaixa/InsertCredito`
**Entrada**:
```json
{ "dataFC": "yyyy-MM-dd", "descricao": "string", "credito": 0.00 }
```
**Critérios de aceitação**:
- [x] Linha persistida em `FluxoDeCaixa` com `ID = UUIDv7`, `credito > 0`.
- [x] Resposta `200 + { succcess: true, message: "Criado com sucesso!" }`.
- [x] Em caso de validação inválida → `400 + { errors: [...] }`.
- [x] Em caso de falha de BD → `200 + { succcess: false, message: <erro> }` *(comportamento atual; roadmap: 5xx)*.

### RF-02 — Registrar lançamento de **débito**
Análogo a RF-01, endpoint `POST /api/FluxoDeCaixa/InsertDebito`.

### RF-03 — Consultar **consolidado diário** por intervalo
**Endpoint**: `GET /api/FluxoDeCaixaRelatorio/Relatorio?inicio=yyyy-MM-dd&fim=yyyy-MM-dd`
**Critérios de aceitação**:
- [x] Retorna lista `[{ dataFC, credito, debito, criadoEm }]` agregada por dia.
- [x] `400` se `inicio` vazio ou `fim ≤ inicio`.
- [x] Suporta intervalo de até 10 anos sem degradação perceptível (<200 ms p95).

### RF-04 *(roadmap)* — Atualizar lançamento
- Comando `UpdateFluxoDeCaixaCommand` já existe (esqueleto). Falta implementar handler completo + endpoint.

### RF-05 *(roadmap)* — Excluir lançamento (soft-delete)

### RF-06 *(roadmap)* — Auditoria / histórico de alterações

### RF-07 *(roadmap)* — Exportar relatório (CSV / Excel / API → ERP)

### Regras de negócio (extraídas dos Validators)

| Regra | Origem | Onde |
|---|---|---|
| `dataFC` ∈ [2020-01-01, 2030-12-31] | `CreateFluxoDeCaixaBaseValidator` | App.UseCases |
| `descricao` 1–255 chars | idem | idem |
| `credito` ∈ [R$ 1, R$ 9.999.999.999,99] | `CreateFluxoDeCaixaCreditoValidator` | idem |
| `debito` mesma regra | `CreateFluxoDeCaixaDebitoValidator` | idem |
| `Relatorio.fim > Relatorio.inicio` | `GetFluxoDeCaixaRelatorioByInicioFimValidator` | idem |

---

## 2. Requisitos Não-Funcionais

### RNF-01 — **Disponibilidade Independente** *(crítico, do enunciado)*
> Lançamentos não pode ficar indisponível se Relatório cair.

| Métrica | Meta | Estratégia |
|---|---|---|
| MTTR Lançamentos quando Relatório falha | 0 (não há propagação) | Microsserviços separados, sem chamada síncrona Lanc→Rel |

### RNF-02 — **Throughput / Carga** *(crítico, do enunciado)*
> Relatório suporta **50 req/s** com no máximo **5% de perda**.

| Métrica | Meta | Estratégia |
|---|---|---|
| Throughput sustentado | ≥ 50 rps | Stateless + autoscale 3→15 réplicas; cache Redis (roadmap); read model pré-agregada |
| Taxa de erro sob pico | < 5% | Circuit breaker no Gateway (roadmap); rate limit configurável |

### RNF-03 — **Latência**
| Métrica | Meta |
|---|---|
| p50 Lançamento | < 50 ms |
| p95 Lançamento | < 200 ms |
| p95 Relatório (com cache) | < 100 ms |
| p95 Relatório (sem cache) | < 250 ms |

### RNF-04 — **Disponibilidade global**
| SLO | Meta |
|---|---|
| Lançamentos | 99,9% / mês (≈ 43 min downtime/mês) |
| Relatório | 99,5% / mês |
| Gateway | 99,95% (replicado, com Front Door) |

### RNF-05 — **Segurança**
| Item | Meta | Status |
|---|---|---|
| Validação de input | 100% endpoints | ✅ FluentValidation |
| AuthN | JWT/OIDC no Gateway | ⚠️ Placeholder em `appsettings`; integração roadmap |
| AuthZ | Role-based (`admin`, `editor`) | ⚠️ Roadmap |
| Criptografia em trânsito | TLS 1.2+ obrigatório | ✅ Gateway HTTPS; mTLS interno (roadmap) |
| Criptografia em repouso | TDE no SQL Server | ✅ Default em Azure SQL |
| Secrets | Em Vault, nunca em git | ⚠️ Hoje em `appsettings`; mover para Key Vault em produção |
| Headers de segurança | `Strict-Transport-Security`, `X-Content-Type-Options`, etc. | ⚠️ Adicionar middleware no Gateway |
| Rate limit | 1000 rps / IP | ⚠️ Roadmap |

### RNF-06 — **Escalabilidade**
- **Horizontal**: serviços stateless, podem rodar N réplicas atrás do Gateway.
- **Vertical**: SQL Server pode escalar de S0 → S2 → S4 → P1+ conforme carga.
- **Autoscaling**: KEDA HTTP scaler (K8s) ou ACA HTTP rule (Azure) — gatilho 30 rps por instância.

### RNF-07 — **Observabilidade**
| Pilar | Ferramenta | Status |
|---|---|---|
| Logs estruturados | `LoggingBehaviour` + ILogger | ✅ |
| Logs centralizados | App Insights / Loki / ELK | ⚠️ Roadmap (config) |
| Métricas | Prometheus / App Insights metrics | ⚠️ Roadmap |
| Traces distribuídos | OpenTelemetry → Jaeger / Tempo | ⚠️ Roadmap |
| Alertas | Alertas em SLOs (p95, error rate) | ⚠️ Roadmap |

### RNF-08 — **Manutenibilidade**
- Clean Architecture estrita (regras documentadas em `docs/arquitetura/uml/componentes.md`).
- Validações centralizadas em `*Validator.cs`.
- Cross-cutting em `Behaviours/`.
- Cobertura de testes (roadmap): mínimo 70% nas camadas Application e Persistence.

### RNF-09 — **Portabilidade / Deploy**
- Imagens Docker prontas (`docker/Dockerfile.*`).
- `docker-compose.yml` para subir tudo localmente.
- Compatível com Azure Container Apps, AWS ECS, Kubernetes.

### RNF-10 — **Conformidade**
- LGPD: campos `descricao` podem conter dados pessoais → criptografia em coluna *(roadmap)* e logs sem PII (revisar `LoggingBehaviour` para mascarar `descricao` em produção).

---

## 3. Matriz de Rastreabilidade

| Requisito | Implementado em | Verificado por |
|---|---|---|
| RF-01 | `CreateFluxoDeCaixaCreditoCommand` + Handler + Endpoint | Smoke test em `SETUP.md` §2.4 |
| RF-02 | `CreateFluxoDeCaixaDebitoCommand` + Handler + Endpoint | idem |
| RF-03 | `GetRelatorioByDateFCInicioFimQuery` + Handler + Endpoint | idem |
| RNF-01 | Microsserviços independentes (`FluxoDeCaixa.WebApi` ≠ `FluxoDeCaixaRelatorio.WebApi`) | Teste de chaos: `docker compose stop relatorio` → Lançamentos continua |
| RNF-02 | Read model + autoscale + cache (roadmap) | Load test com `k6` (script em roadmap) |
| RNF-03 | Dapper + Behaviours + threshold de log | `PerformanceBehaviour` reporta automaticamente |
| RNF-05 | FluentValidation + Middleware + CORS + JWT placeholder | Code review + DAST |
| RNF-08 | ADR-001..006 + estrutura de pastas | Code review |
