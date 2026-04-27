# Custos de Infraestrutura — Estimativa

> Estimativa **referencial** (USD/mês) para a stack-alvo descrita no `docs/arquitetura/c4/05-deploy.md`. Preços de lista da Azure em 2026 (East US 2). Variações regionais podem ser ±30%.

---

## 1. Cenários

| Cenário | Tráfego esperado | Réplicas | Banco | Mensal estimado |
|---|---|---|---|---|
| **DEV** (1 desenvolvedor) | trivial | 1 cada | Azure SQL S0 | **~ US$ 20** |
| **PRD pequeno** (até 10 req/s sustentado) | até 1 M req/mês | 2 lanc + 3 rel + 2 gw | Azure SQL S2 | **~ US$ 220** |
| **PRD médio** (alvo do desafio: 50 req/s pico) | até 10 M req/mês | 2 lanc + 6 rel + 2 gw + Redis | Azure SQL S4 + GeoReplica | **~ US$ 815** |
| **PRD grande** (300 req/s pico, multi-região) | 50 M req/mês | autoscale 2-15 | Azure SQL P2 + Failover Group | **~ US$ 2.500** |

---

## 2. Detalhamento — PRD médio (alvo do desafio)

| Componente | SKU | Qtd | Preço/mês (US$) | Total |
|---|---|---|---|---|
| **Container Apps Environment** (consumption) | 0.5 vCPU + 1 GB | 11 réplicas-médias | $0,000024/vCPU-s + $0,000003/GiB-s | ~ $200 |
| **Azure SQL Database** | S4 (200 DTU) | 1 + 1 readable replica | $0,73/h + $0,73/h | ~ $1.060 *(usar S2 se 200 DTU for excessivo: ~$300)* |
| **Azure Cache for Redis** | Standard C1 (1 GB, replica) | 1 | $0,099/h | ~ $73 |
| **Azure Front Door Std** | base | 1 | $35 + tráfego | ~ $50 |
| **Azure Key Vault** | Standard | 1 | $0,03/10k ops | ~ $1 |
| **App Insights / Log Analytics** | pay-as-you-go | — | $2,30/GB ingest | ~ $80 (≈ 30 GB/mês) |
| **Container Registry** | Basic | 1 | $5/mês | $5 |
| **Outbound bandwidth** | — | — | $0,087/GB (após 100 GB free) | ~ $20 |
| **Total bruto S2 + Redis + Front Door + Logs + ACA** |  |  |  | **~ $815/mês** |

> Em produção, **comprar reservas de 1 ano** reduz Azure SQL e Container Apps em ~30%.

---

## 3. Estratégias de redução de custo

| Estratégia | Economia esperada | Trade-off |
|---|---|---|
| **Azure SQL Serverless** (auto-pause em ociosidade) | até 50% em DEV/Staging | Cold-start de 1 min após ociosidade |
| **Container Apps min-replicas = 0** (escalas a zero) | até 70% em janelas de baixa | Cold-start no primeiro request |
| **Reservas de 1 ou 3 anos** | 30-60% | Compromisso financeiro |
| **Cache distribuído** com TTL longo | Reduz DTU SQL em até 90% | Eventual inconsistência (TTL) |
| **Compressão de logs + sampling** | 50-70% no App Insights | Menos detalhes para debug |
| **Spot/Preemptible nodes** (K8s) | até 80% no compute | Restart inesperado |

---

## 4. Comparativo Azure × AWS × GCP × On-prem

| Serviço | Azure (escolhido) | AWS equivalente | GCP equivalente | On-prem |
|---|---|---|---|---|
| Container runtime | Container Apps | ECS Fargate | Cloud Run | Kubernetes (kubeadm/Rancher) |
| BD SQL Server | Azure SQL DB | RDS for SQL Server | (não nativo — usar Cloud SQL ou self-hosted) | SQL Server std/ent (~US$ 8k/core/ano) |
| Cache | Azure Cache for Redis | ElastiCache | Memorystore | Redis container |
| Edge / WAF | Front Door | CloudFront + WAF | Cloud Armor | NGINX + ModSecurity |
| Monitor | App Insights / Log Analytics | CloudWatch | Cloud Operations | Grafana + Prometheus + Loki |

> Para clientes que **já usam Microsoft Stack** (AD, Office 365, Power BI), Azure é geralmente 10-20% mais barato no TCO total por integração nativa de identidade e ferramentas.

---

## 5. FinOps — boas práticas adotadas

- **Tags obrigatórias** em todo recurso: `Project`, `Environment`, `CostCenter`, `Owner`.
- **Budget alerts** no Cost Management: 50%, 80%, 100% do orçamento mensal.
- **Right-sizing** semanal: comparar P95 de CPU/RAM com a SKU contratada.
- **Dashboard de custo por serviço** publicado no Power BI / Grafana.
- **Política de PR**: qualquer recurso novo > US$ 50/mês requer aprovação do Tech Lead.

---

## 6. ROI esperado

Cenário PRD médio (~US$ 800/mês = US$ 9.600/ano):
- Substitui **planilha + retrabalho contábil** estimado em **120 h/mês × R$ 80/h ≈ R$ 9.600/mês = US$ 1.900/mês**.
- **Payback < 1 mês**.
- **Redução de fraude/erro** (auditável + UUIDv7 imutável) — benefício adicional não quantificado.
