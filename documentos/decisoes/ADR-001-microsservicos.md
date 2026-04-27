# ADR-001 | Adotar arquitetura de Microsserviços com API Gateway

- **Status:** Aceita
- **Data:** 2026-04-26
- **Decisores:** Arquiteto Corporativo, Tech Lead
- **Tags:** estilo-arquitetural, alta-disponibilidade

## Contexto

O desafio impõe um requisito não-funcional decisivo:

> *"O serviço de controle de lançamento **não deve ficar indisponível** se o sistema de consolidado diário cair. Em dias de pico, o serviço de consolidado diário recebe 50 req/s, com no máximo 5% de perda de requisições."*

Isso impõe **isolamento de falhas** e **escalabilidade independente** entre o caminho de **escrita** (lançamentos) e o caminho de **leitura** (relatório consolidado).

## Opções consideradas

### Opção A — Monólito modular
- ✅ Simplicidade operacional (1 deploy, 1 banco, 1 pipeline).
- ✅ Menor latência interna (chamadas in-process).
- ❌ **Falha catastrófica compartilhada**: um pico de leitura derruba o processo inteiro → viola RNF-01.
- ❌ Escala vertical apenas, multiplicar instâncias custa mais (replica todo o código, mesmo o que não tem carga).
- ❌ Liberações acopladas, qualquer mudança no Relatório requer redeploy de Lançamentos.

### Opção B — Microsserviços (escolhida)
- ✅ **Atende RNF-01 por design**: processos separados, falhas não se propagam.
- ✅ Escala independente Relatório pode ter 10 réplicas enquanto Lançamentos tem 2.
- ✅ Times podem evoluir os bounded contexts independentemente.
- ✅ Permite **CQRS físico** futuro (banco de leitura separado para o Relatório).
- ❌ Maior complexidade operacional (3 processos, gateway, observabilidade distribuída).
- ❌ Latência de hop extra (mitigada pelo Gateway in-cluster).

### Opção C — Serverless (FaaS)
- ✅ Auto-scale gratuito.
- ❌ Cold-start incompatível com o pico de 50 req/s (latência inaceitável no primeiro request).
- ❌ Equipe ainda não tem maturidade operacional em FaaS para .NET 8.

## Decisão

**Adotar Microsserviços (Opção B)** com as seguintes regras:

1. **Dois microsserviços de negócio** + **um API Gateway**:
   - `FluxoDeCaixa.WebApi` (Lançamentos)
   - `FluxoDeCaixaRelatorio.WebApi` (Relatório)
   - `FluxoDeCaixa.Gateway` (YARP)
2. **Sem chamadas síncronas** entre Lançamentos e Relatório. Comunicação futura será **assíncrona via eventos** (Outbox + broker).
3. **Banco compartilhado nesta versão**, com tabelas separadas:
   - `FluxoDeCaixa` é escrita por Lançamentos.
   - `FluxoDeCaixaConsolidado` é lida por Relatório (read model pré-agregada).
   - Path evolutivo claro para **database-per-service**.
4. **Gateway é o único ponto público** microsserviços têm ingress *internal-only* em produção.

## Consequências

### Positivas
- RNF-01 atendido por design.
- Escalabilidade fina e isolada.
- Caminho aberto para evoluções (CQRS físico, mensageria, novos bounded contexts).

### Negativas / mitigadas
- **Complexidade operacional** → mitigada com Docker Compose para dev local, Container Apps / Kubernetes para produção, observabilidade unificada.
- **Latência adicional** (1 hop no Gateway) → mitigada com YARP em-process e ambos rodando na mesma VPC.
- **Eventual inconsistency** entre `FluxoDeCaixa` e `FluxoDeCaixaConsolidado` → mitigada com Outbox + job de consolidação (roadmap).
