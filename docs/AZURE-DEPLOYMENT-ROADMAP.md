# Azure Deployment Roadmap

## Goal

Deploy EShop to Azure for personal testing and demonstration at minimal cost (~€4-6/month),
using managed services instead of self-hosted containers for infrastructure components.

---

## Target Architecture

```
Internet
    │
    ▼
Azure Container Apps Environment
├── api-gateway        (YARP reverse proxy — only public-facing entry point)
├── identity-api       (JWT auth — our own implementation, kept as-is)
├── ordering-api       (connects to Azure SQL + Azure Service Bus)
├── catalog-api        (stateless, no DB)
└── web                (React/Nginx frontend)

Azure SQL Database      (replaces SQL Server container — IdentityDb + OrderingDb)
Azure Service Bus       (replaces RabbitMQ container)
Azure Key Vault         (stores all secrets — passwords, JWT keys, connection strings)
Azure Container Registry (stores Docker images — replaces Docker Hub)
Azure Monitor           (replaces Seq — logs and metrics)
```

---

## GitHub vs Azure — Role Split

These are two separate systems with distinct responsibilities:

| | GitHub | Azure |
|---|---|---|
| **Purpose** | Source control + CI/CD | Cloud infrastructure |
| **Stores** | Code, git history, PRs | Containers, databases, networking |
| **Runs** | GitHub Actions pipelines | Container Apps, SQL, Service Bus |
| **Cost** | Free (public repo) | €4-6/month |

GitHub remains the single source of truth for code. Azure is purely the runtime environment.

### How they connect

```
git push → GitHub
               │
               │  GitHub Actions (ci.yml / cd.yml) — unchanged trigger logic
               │
               ├─ 1. build & run tests                      (same as before)
               ├─ 2. docker build                            (same as before)
               ├─ 3. docker push → Azure Container Registry  (was: Docker Hub)
               └─ 4. deploy → Azure Container Apps           (was: update K8s manifests)
```

Only steps 3 and 4 change. Everything else in the pipeline stays the same.

### K8s manifests

The existing `k8s/` folder is **not used** in this deployment. Azure Container Apps replaces
Kubernetes entirely — there are no manifest files to maintain or apply.
The `k8s/` folder is kept in the repository for reference only (e.g. future AKS migration).

---

## Cost Breakdown

| Service | Tier | Cost |
|---|---|---|
| Azure Container Apps | Consumption (scales to zero) | ~€0-3/month |
| Azure SQL Database | Serverless (auto-pause) | ~€1-2/month |
| Azure Service Bus | Basic | ~€1/month |
| Azure Container Registry | Basic | ~€2/month |
| Azure Key Vault | Standard | ~€0 (free tier for demo) |
| Azure Monitor | Basic ingestion | ~€0-1/month |
| **Total** | | **~€4-6/month** |

> Azure SQL Serverless auto-pauses after 60 minutes of inactivity — you pay only when queries run.
> First request after a pause has a ~30-60 second cold start, which is acceptable for demo use.

---

## Phase 1 — Azure Foundation

**Goal:** Create the Azure resources before touching any code.

### 1.1 Create Resource Group

```bash
az group create --name eshop-rg --location westeurope
```

Everything lives in one resource group so you can delete it all at once when not needed.

### 1.2 Create Azure Container Registry

```bash
az acr create --resource-group eshop-rg --name eshopregistry --sku Basic
```

This replaces Docker Hub. Your CI/CD pipeline will push images here.

### 1.3 Create Azure SQL Database

```bash
az sql server create \
  --name eshop-sql-server \
  --resource-group eshop-rg \
  --location westeurope \
  --admin-user sqladmin \
  --admin-password <strong-password>

az sql db create \
  --resource-group eshop-rg \
  --server eshop-sql-server \
  --name IdentityDb \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --min-capacity 0.5 \
  --capacity 2 \
  --auto-pause-delay 60

az sql db create \
  --resource-group eshop-rg \
  --server eshop-sql-server \
  --name OrderingDb \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --min-capacity 0.5 \
  --capacity 2 \
  --auto-pause-delay 60
```

Two databases on one logical server — both Serverless tier.
`--auto-pause-delay 60` pauses each database after 60 minutes of inactivity.
`--min-capacity 0.5` keeps cost minimal when active but idle.

### 1.4 Create Azure Service Bus

```bash
az servicebus namespace create \
  --resource-group eshop-rg \
  --name eshop-servicebus \
  --location westeurope \
  --sku Basic
```

### 1.5 Create Azure Key Vault

```bash
az keyvault create \
  --name eshop-keyvault \
  --resource-group eshop-rg \
  --location westeurope
```

Store all secrets here:

```bash
az keyvault secret set --vault-name eshop-keyvault --name SqlAdminPassword --value "<password>"
az keyvault secret set --vault-name eshop-keyvault --name JwtSecret --value "<jwt-secret>"
```

### 1.6 Create Container Apps Environment

```bash
az containerapp env create \
  --name eshop-env \
  --resource-group eshop-rg \
  --location westeurope
```

All container apps share this environment — they communicate via internal DNS
(e.g. `http://identity-api` resolves within the environment, same as Docker Compose service names).

---

## Phase 2 — Configuration Changes

**Goal:** Add an `appsettings.Azure.json` to each service. No existing files are modified.

### 2.1 Identity API — `appsettings.Azure.json`

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Server=eshop-sql-server.database.windows.net;Database=IdentityDb;User Id=sqladmin;Password=<from-keyvault>;TrustServerCertificate=false;Encrypt=true"
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights" }
    ]
  }
}
```

### 2.2 Ordering API — `appsettings.Azure.json`

```json
{
  "ConnectionStrings": {
    "OrderingDb": "Server=eshop-sql-server.database.windows.net;Database=OrderingDb;User Id=sqladmin;Password=<from-keyvault>;TrustServerCertificate=false;Encrypt=true"
  },
  "ServiceUrls": {
    "Catalog": "http://catalog-api"
  },
  "RabbitMq": {
    "Host": "<azure-service-bus-connection-string>"
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights" }
    ]
  }
}
```

> Note: MassTransit supports Azure Service Bus natively — the `Host` value changes
> but no C# code changes are required in `Program.cs`, only the transport configuration.

### 2.3 API Gateway — `appsettings.Azure.json`

```json
{
  "ReverseProxy": {
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://identity-api" }
        }
      },
      "ordering-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://ordering-api" }
        }
      },
      "catalog-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://catalog-api" }
        }
      }
    }
  }
}
```

Internal DNS within the Container Apps Environment resolves service names automatically,
exactly like Docker Compose networking.

---

## Phase 3 — CI/CD Pipeline Update

**Goal:** Update GitHub Actions to push images to ACR and deploy to Container Apps
instead of updating K8s manifests.

### 3.1 Add GitHub Secrets

In your GitHub repository settings, add:

| Secret | Value |
|---|---|
| `AZURE_CLIENT_ID` | Service principal app ID |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID |
| `ACR_LOGIN_SERVER` | `eshopregistry.azurecr.io` |

### 3.2 Updated `cd.yml` — key changes

**Build & Push to ACR** (replaces Docker Hub push):

```yaml
- name: Log in to ACR
  uses: azure/docker-login@v1
  with:
    login-server: ${{ secrets.ACR_LOGIN_SERVER }}
    username: ${{ secrets.AZURE_CLIENT_ID }}
    password: ${{ secrets.AZURE_CLIENT_SECRET }}

- name: Build and push
  run: |
    docker build -t ${{ secrets.ACR_LOGIN_SERVER }}/eshop-${{ matrix.service }}:${{ github.sha }} .
    docker push ${{ secrets.ACR_LOGIN_SERVER }}/eshop-${{ matrix.service }}:${{ github.sha }}
```

**Deploy to Container Apps** (replaces K8s manifest update):

```yaml
- name: Deploy to Container Apps
  uses: azure/container-apps-deploy-action@v1
  with:
    containerAppName: ${{ matrix.service }}
    resourceGroup: eshop-rg
    imageToDeploy: ${{ secrets.ACR_LOGIN_SERVER }}/eshop-${{ matrix.service }}:${{ github.sha }}
```

---

## Phase 4 — Deploy Container Apps

**Goal:** Create one Container App per service.

### Example: Identity API

```bash
az containerapp create \
  --name identity-api \
  --resource-group eshop-rg \
  --environment eshop-env \
  --image eshopregistry.azurecr.io/eshop-identity-api:latest \
  --registry-server eshopregistry.azurecr.io \
  --target-port 8080 \
  --ingress internal \
  --env-vars ASPNETCORE_ENVIRONMENT=Azure \
  --min-replicas 0 \
  --max-replicas 2
```

Key flags:
- `--ingress internal` — not reachable from internet, only from within the environment
- `--min-replicas 0` — scales to zero when idle (no cost)
- Only `api-gateway` and `web` use `--ingress external`

### Deployment order

```
1. catalog-api        (no dependencies)
2. identity-api       (depends on Azure SQL)
3. ordering-api       (depends on Azure SQL + Service Bus + catalog-api)
4. api-gateway        (depends on all three APIs)
5. web                (depends on api-gateway)
```

---

## Phase 5 — Verify & Test

```bash
# Check all container apps are running
az containerapp list --resource-group eshop-rg --output table

# Get the public URL of the API gateway
az containerapp show --name api-gateway --resource-group eshop-rg \
  --query "properties.configuration.ingress.fqdn" -o tsv

# Check logs for a specific service
az containerapp logs show --name ordering-api --resource-group eshop-rg --follow
```

Test the full flow:
1. `POST /api/auth/register` → register a user
2. `POST /api/auth/login` → get JWT token
3. `GET /api/products` → list catalog
4. `POST /api/orders` → create an order (triggers domain event → Service Bus)

---

## Cost Control Tips

**Container Apps** scale to zero automatically (`--min-replicas 0`) — no action needed.

**Azure SQL** pauses automatically after 60 minutes of inactivity — no action needed.
If you want to pause immediately:
```bash
az sql db update --resource-group eshop-rg --server eshop-sql-server \
  --name IdentityDb --auto-pause-delay 1

az sql db update --resource-group eshop-rg --server eshop-sql-server \
  --name OrderingDb --auto-pause-delay 1
```

**Delete everything at once when done:**
```bash
az group delete --name eshop-rg --yes
```

Recreate with Phase 1 commands when needed.

---

## What Changes vs What Stays the Same

### Changes
| Before | After |
|---|---|
| Docker Hub (`geopapag/eshop-*`) | Azure Container Registry (`eshopregistry.azurecr.io/eshop-*`) |
| K8s manifests updated by CD pipeline | Azure Container Apps deployed by CD pipeline |
| `appsettings.Docker.json` used in production | `appsettings.Azure.json` used in production |
| RabbitMQ container | Azure Service Bus |
| SQL Server container | Azure SQL Database (Serverless) |
| Seq container | Azure Monitor |

### Does NOT Change
- All C# domain/application/infrastructure code — zero changes
- Dockerfiles — identical images, just pushed to a different registry
- GitHub as source control and CI/CD trigger
- The Identity service — kept as-is, no Entra ID replacement needed
- `appsettings.Docker.json` files — still used for local docker-compose development
- `k8s/` folder — kept in repo for reference, not used in this deployment
