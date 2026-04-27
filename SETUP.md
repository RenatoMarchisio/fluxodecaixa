# SETUP | Fluxo de Caixa

Guia detalhado para subir a solução **localmente** (.NET 8 + SQL Server) ou **via Docker** (recomendado, sem dependências locais além do Docker).

---

## 1. Pré-requisitos

### 1.1 Para execução via Docker (recomendado)

| Requisito | Versão |
|---|---|
| Docker Engine ou Docker Desktop | ≥ 24.0 |
| Docker Compose | v2 (vem embutido no Docker Desktop) |
| Portas livres | `1433`, `5000`, `8000`, `8500` |
| RAM disponível | ≥ 4 GB (SQL Server consome ~2 GB) |

### 1.2 Para execução local (sem Docker)

| Requisito | Versão | Como obter |
|---|---|---|
| **.NET 8 SDK** | 8.0.100 ou superior | https://dotnet.microsoft.com/download |
| **SQL Server** | LocalDB (Windows) **ou** Express/Developer 2019+ | https://www.microsoft.com/sql-server/sql-server-downloads |
| **`sqlcmd`** | qualquer | acompanha o SQL Server, ou via `mssql-tools18` |
| Visual Studio 2022 17.8+ *(opcional)* | — | para executar via Multi-startup |

Verificação:
```bash
dotnet --version           # 8.x.x
sqlcmd -?                  # exibe ajuda
docker --version           # 24+
docker compose version     # v2.x
```

---

## 2. Setup via Docker (caminho rápido)

### 2.1 Subir tudo

```bash
cd FluxoDeCaixa/docker
docker compose up -d --build
```

> O primeiro build leva ~3-5 minutos (download de imagens .NET SDK/runtime + restore de pacotes).

### 2.2 Verificar saúde

```bash
docker compose ps
docker compose logs -f gateway
```

### 2.3 URLs prontas para usar

| Serviço | URL | Observação |
|---|---|---|
| **API Gateway** (Swagger agregado) | http://localhost:5000/swagger | Único ponto de entrada externo |
| **Lançamentos** (Swagger direto) | http://localhost:8000 | Acesso direto p/ debug |
| **Relatório** (Swagger direto) | http://localhost:8500 | Acesso direto p/ debug |
| **SQL Server** | `localhost,1433` | `sa` / `Fluxo@2026Dev!` |

### 2.4 Smoke test

```bash
# Cria um crédito
curl -X POST http://localhost:5000/api/FluxoDeCaixa/InsertCredito \
  -H "Content-Type: application/json" \
  -d '{"dataFC":"2026-04-26","descricao":"Venda 1","credito":250}'

# Cria um débito
curl -X POST http://localhost:5000/api/FluxoDeCaixa/InsertDebito \
  -H "Content-Type: application/json" \
  -d '{"dataFC":"2026-04-26","descricao":"Compra 1","debito":100}'

# Consulta o consolidado (após executar a recarga abaixo)
curl "http://localhost:5000/api/FluxoDeCaixaRelatorio/Relatorio?inicio=2026-01-01&fim=2026-12-31"
```

> **Nota**: a tabela `FluxoDeCaixaConsolidado` é alimentada por um job SQLServer (SqlAgents disponível na versão Developer\Standard\Enterprise ) na versão atual via SQL manual, (no roadmap futuro via mensageria/Outbox). Para repopular agora, rode e execute o comando que vai criar um scheduled no windows para criar os registros do mês seguinte, no ultimo dia do mês atual.
> ```sql
> sqlcmd -S "(localdb)\MSSQLLocalDB" -d fluxocaixa -E -Q "EXEC sp_CriarConsolidadoMesSeguinte"
> ```
> ```cmd
> schtasks /create /tn "CreateFluxoDeCaixaRelatorioConsolidado" /tr "sqlcmd -S \"(localdb)\MSSQLLocalDB\" -d fluxocaixa -E -Q \"EXEC sp_CriarConsolidadoMesSeguinte\"" /sc monthly /mo LASTDAY /m * /st 23:59
> ```

### 2.5 Derrubar tudo

```bash
docker compose down            # mantém o volume do banco
docker compose down -v         # apaga TUDO, inclusive dados
```

---

## 3. Setup local (sem Docker)

### 3.1 Restaurar dependências

Da raiz do repositório:

```bash
dotnet restore FluxoDeCaixa.sln
dotnet build   FluxoDeCaixa.sln -c Debug
```

### 3.2 Criar o banco de dados

#### 3.2.1 SQL Server LocalDB (Windows)

```powershell
# 1. Garante a instância
sqllocaldb start mssqllocaldb

# 2. Cria o banco FluxoCaixa (caso não exista)
sqlcmd -S "(localdb)\mssqllocaldb" -Q "IF DB_ID('FluxoCaixa') IS NULL CREATE DATABASE FluxoCaixa"

# 3. Aplica o DDL
sqlcmd -S "(localdb)\mssqllocaldb" -d FluxoCaixa -i "Docker\Sql\init.sql"
```

#### 3.2.2 SQL Server Express / Developer (Linux/macOS/Windows)

```bash
sqlcmd -S "localhost,1433" -U sa -P "<sua-senha>" \
  -Q "IF DB_ID('FluxoCaixa') IS NULL CREATE DATABASE FluxoCaixa"
sqlcmd -S "localhost,1433" -U sa -P "<sua-senha>" -d FluxoCaixa \
  -i "Docker\Sql\init.sql"
```

E ajuste o `appsettings.Development.json` de cada WebApi:

```jsonc
"ConnectionStrings": {
  "FluxoDeCaixaConnection": "Server=localhost,1433;Database=FluxoCaixa;User Id=sa;Password=<sua-senha>;TrustServerCertificate=True"
}
```

### 3.3 Subir os 4 serviços

Em **três terminais**:

```bash
# Terminal 1 — Lançamentos
dotnet run --project src/FluxoDeCaixa.WebApi
# Listening on http://localhost:8000

# Terminal 2 — Relatório
dotnet run --project src/FluxoDeCaixaRelatorio.WebApi
# Listening on http://localhost:8500

# Terminal 3 — Gateway
dotnet run --project src/FluxoDeCaixa.Gateway
# Listening on https://localhost:5000

# Terminal 4 — BackEnd DLQ
dotnet run --project src/FluxoDeCaixaDLQ
# Listening on https://localhost:5150
```

> **Visual Studio 2022**: Solution → Properties → *Multiple startup projects* → Action = *Start* nos 3 projetos acima → F5.

### 3.4 Validar

Abra https://localhost:5000/swagger/index.html — você deve ver as 3 APIs (Gateway, Fluxo de Caixa, Relatório) agregadas.

---

## 4. Variáveis de Configuração

Cada serviço suporta as seguintes chaves (em `appsettings.json` ou via env-vars `ASPNETCORE__`):

| Chave | Descrição | Default |
|---|---|---|
| `ConnectionStrings:FluxoDeCaixaConnection` | Conexão SQL Server | `(localdb)\mssqllocaldb;Database=FluxoCaixa;...` |
| `Jwt:Issuer` | Emissor do token (placeholder) | `Fluxo de Caixa - BC` |
| `Jwt:Secret` | Chave HMAC do JWT | *(definir antes de produção)* |
| `Jwt:Expires` | Expiração em minutos | `30` |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Staging` / `Production` | `Development` |
| `ASPNETCORE_URLS` | URLs/portas onde o Kestrel escuta | `http://+:8000` etc. |

No Docker, as variáveis são injetadas pelo `docker-compose.yml` — basta editar lá.

---

## 5. Troubleshooting

| Sintoma | Causa | Correção |
|---|---|---|
| `A network-related or instance-specific error...` | SQL Server fora do ar | `docker compose ps` (verifique container `sqlserver`) ou `sqllocaldb info` |
| `Login failed for user 'sa'` | Senha errada | Use exatamente `Fluxo@2026Dev!` no Docker, ou ajuste `appsettings.Development.json` |
| `port is already allocated` | Porta 8000/8500/5000/1433 ocupada | Pare o processo conflitante ou edite `docker-compose.yml` |
| `400 Bad Request` ao inserir | Validação FluentValidation | Veja o corpo da resposta — `errors[]` traz `propertyMessage` + `errorMessage` |
| Swagger do Gateway não mostra APIs | Serviços downstream não subiram | `docker compose logs lancamentos relatorio` |
| Relatório retorna `[]` | A tabela `FluxoDeCaixaConsolidado` está vazia | Rode o `INSERT ... SELECT ... GROUP BY` da seção 2.4 |

---

## 6. Estrutura de Build & Deploy

### 6.1 Build de produção (binários)

```bash
dotnet publish src/FluxoDeCaixa.WebApi          -c Release -o out/lancamentos
dotnet publish src/FluxoDeCaixaRelatorio.WebApi -c Release -o out/relatorio
dotnet publish src/FluxoDeCaixa.Gateway         -c Release -o out/gateway
dotnet publish src/FluxoDeCaixaDLQ              -c Release -o out/DLQ
```

### 6.2 Build das imagens Docker (uma a uma)

```bash
docker build -f docker/Dockerfile.lancamentos -t fluxodecaixa/lancamentos:latest .
docker build -f docker/Dockerfile.relatorio   -t fluxodecaixa/relatorio:latest   .
docker build -f docker/Dockerfile.gateway     -t fluxodecaixa/gateway:latest     .
docker build -f docker/Dockerfile.dlq         -t fluxodecaixa/dlq:latest         .
```

### 6.3 Caminho recomendado para Cloud

- **Azure**: Container Apps + Azure SQL → `az containerapp up` (3 apps + 1 BD).
- **AWS**: ECS Fargate + RDS for SQL Server.
- **Kubernetes**: 3 Deployments + 3 Services + 1 Ingress (NGINX/Traefik) + StatefulSet/Operator de SQL.

Detalhes em `docs/arquitetura/c4/05-deploy.md` e `docs/operacao/custos.md`.

---

## 7. Próximos Passos

- Rodar a suíte de testes: `dotnet test` *(estrutura preparada — implementação em roadmap)*
- Ler **[`docs/arquitetura/c4/01-contexto.md`](docs/arquitetura/c4/01-contexto.md)** para entender a arquitetura.
- Ler **[`docs/decisoes/`](docs/decisoes/)** para entender o "porquê" das escolhas.
