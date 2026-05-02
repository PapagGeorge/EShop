# Containerization, Kubernetes & CI/CD Guide

## Table of Contents

1. [Overview](#1-overview)
2. [Docker — Core Concepts](#2-docker--core-concepts)
3. [Dockerfiles — Deep Dive](#3-dockerfiles--deep-dive)
4. [Docker Compose — Local Development](#4-docker-compose--local-development)
5. [Docker Hub — Publishing Images](#5-docker-hub--publishing-images)
6. [Kubernetes — Core Concepts](#6-kubernetes--core-concepts)
7. [Kubernetes Manifests — Deep Dive](#7-kubernetes-manifests--deep-dive)
8. [Deploying to Kubernetes](#8-deploying-to-kubernetes)
9. [CI/CD with GitHub Actions](#9-cicd-with-github-actions)
10. [Full Workflow from Scratch](#10-full-workflow-from-scratch)

---

## 1. Overview

EShop consists of **4 microservices** and **3 infrastructure services**:

```
                        ┌─────────────────────────────────────────────┐
                        │              Kubernetes Cluster              │
                        │                                              │
  Browser / Client ───► │  api-gateway (port 5000)                     │
                        │       │                                      │
                        │  ┌────┴──────────────────────┐              │
                        │  identity-api  ordering-api  catalog-api    │
                        │  └────────────────────────────┘              │
                        │       │              │                       │
                        │  sqlserver      rabbitmq        seq          │
                        └─────────────────────────────────────────────┘
```

**The development pipeline:**
```
Source Code → Dockerfile → Docker Image → Docker Hub → Kubernetes
                                                ↑
                                       GitHub Actions (CI/CD)
```

---

## 2. Docker — Core Concepts

### What is a Docker Image?

A **Docker image** is a snapshot of your application — the executable, libraries,
configuration, everything bundled into a single file. Think of it like an installation
ISO: static, immutable, and shareable.

### What is a Container?

A **container** is a running instance of an image. You can run 10 containers from
the same image simultaneously, and each one is fully **isolated** from the others.

```
Image  (static, immutable)  →  Container (running, has state)
   └── like a class               └── like an object instance
```

### What is a Dockerfile?

A **Dockerfile** is the recipe for building an image. It contains step-by-step
instructions that Docker executes in order.

---

## 3. Dockerfiles — Deep Dive

All services follow the same pattern. Here is a full breakdown of the **Ordering API**:

**File:** `src/Services/Ordering/EShop.Ordering.API/Dockerfile`

```dockerfile
# ── STAGE 1: base ────────────────────────────────────────────────────
# Use the official .NET 8 runtime image (run-only, no build tools)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080          # Declare that the container listens on port 8080

# ── STAGE 2: build ───────────────────────────────────────────────────
# Use the .NET SDK image which includes the compiler and build tools
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy ONLY the .csproj files first → Docker layer cache optimization
# If dependencies haven't changed, the restore step is served from cache
COPY src/BuildingBlocks/EShop.Shared/EShop.Shared.csproj src/BuildingBlocks/EShop.Shared/
COPY src/Services/Ordering/EShop.Ordering.Domain/EShop.Ordering.Domain.csproj src/Services/Ordering/EShop.Ordering.Domain/
COPY src/Services/Ordering/EShop.Ordering.Application/EShop.Ordering.Application.csproj src/Services/Ordering/EShop.Ordering.Application/
COPY src/Services/Ordering/EShop.Ordering.Infrastructure/EShop.Ordering.Infrastructure.csproj src/Services/Ordering/EShop.Ordering.Infrastructure/
COPY src/Services/Ordering/EShop.Ordering.API/EShop.Ordering.API.csproj src/Services/Ordering/EShop.Ordering.API/

RUN dotnet restore src/Services/Ordering/EShop.Ordering.API/EShop.Ordering.API.csproj

# Copy the remaining source code and publish
COPY src/BuildingBlocks/EShop.Shared/ src/BuildingBlocks/EShop.Shared/
COPY src/Services/Ordering/ src/Services/Ordering/
RUN dotnet publish src/Services/Ordering/EShop.Ordering.API/EShop.Ordering.API.csproj \
    -c Release -o /app/publish --no-restore

# ── STAGE 3: final ───────────────────────────────────────────────────
# Take the small runtime image and copy only the compiled output into it
# This pattern is called "multi-stage build" — the final image has no SDK
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EShop.Ordering.API.dll"]
```

> **Why multi-stage builds?** The SDK image is ~750 MB; the runtime image is ~250 MB.
> With multi-stage builds the final image only contains the compiled output,
> keeping it small and free of build tools.

### Build Commands (manual)

Run from the **solution root** (where `EShop.sln` lives):

```bash
# Identity API
docker build -t geopapag/eshop-identity-api:1.0.0 \
  -f src/Services/Identity/EShop.Identity.API/Dockerfile .

# Ordering API
docker build -t geopapag/eshop-ordering-api:1.0.0 \
  -f src/Services/Ordering/EShop.Ordering.API/Dockerfile .

# Catalog API
docker build -t geopapag/eshop-catalog-api:1.0.0 \
  -f src/Services/Catalog/EShop.Catalog.API/Dockerfile .

# API Gateway
docker build -t geopapag/eshop-api-gateway:1.0.0 \
  -f src/ApiGateway/EShop.ApiGateway/Dockerfile .
```

**Breaking down `docker build`:**
```
docker build  -t  geopapag/eshop-ordering-api:1.0.0  -f  src/.../Dockerfile  .
              │   │                                   │   │                    │
              │   └── tag: username/image-name:version│   └── the Dockerfile   └── build context
              └── tag flag                            └── file flag               (current directory)
```

---

## 4. Docker Compose — Local Development

**Docker Compose** lets you start multiple containers simultaneously with a single command.

**Files:**
- `docker-compose.yml` — defines all services
- `docker-compose.override.yml` — additional settings for local development

### docker-compose.yml Structure

```yaml
services:
  sqlserver:                          # SQL Server 2022
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"                   # host_port:container_port
    healthcheck:                      # Checks if the database is ready
      test: sqlcmd ... "SELECT 1"
      retries: 10

  ordering-api:
    build:
      context: .                      # Build from the current directory
      dockerfile: src/.../Dockerfile  # Using this Dockerfile
    depends_on:
      sqlserver:
        condition: service_healthy    # Starts ONLY after sqlserver is healthy
      rabbitmq:
        condition: service_healthy

networks:
  eshop-network:                      # All containers communicate on this network
    driver: bridge
```

### Docker Compose Commands

```bash
# Start all services in the background
docker compose up -d

# Stream logs from a service
docker compose logs ordering-api
docker compose logs -f ordering-api   # -f = follow (real-time)

# Rebuild and restart a single service after a code change
docker compose up -d --build ordering-api

# Stop all services
docker compose down

# Stop and delete volumes (WARNING: all data is lost)
docker compose down -v
```

### General Docker Commands

```bash
# List running containers
docker ps

# List all local images
docker images

# Open a shell inside a container (for debugging)
docker exec -it eshop-ordering-api bash

# View container logs
docker logs eshop-ordering-api
```

---

## 5. Docker Hub — Publishing Images

**Docker Hub** is the "GitHub for Docker images". Images are stored there so anyone
— including Kubernetes — can pull and run them.

```bash
# Log in to Docker Hub
docker login
# → Enter username: geopapag
# → Enter password (or Access Token)

# Push images
docker push geopapag/eshop-identity-api:1.0.0
docker push geopapag/eshop-ordering-api:1.0.0
docker push geopapag/eshop-catalog-api:1.0.0
docker push geopapag/eshop-api-gateway:1.0.0
```

After pushing, images are publicly available at:
`https://hub.docker.com/u/geopapag`

> **Important:** Kubernetes does not use local images — it always pulls from Docker Hub
> (or another registry). This is why the push step is mandatory before deploying.

---

## 6. Kubernetes — Core Concepts

**Kubernetes (K8s)** is a system that manages containers in production.
It solves problems that Docker Compose cannot handle:

| Problem | Docker Compose | Kubernetes |
|---------|---------------|------------|
| Container crashes | Does not auto-restart | Auto-restarts automatically |
| Scaling (e.g. 3 replicas) | Manual | Automatic (`replicas: 3`) |
| Rolling updates (zero downtime) | Not supported | Built-in |
| Health monitoring | Basic | Readiness + Liveness probes |

### Core Kubernetes Objects

```
Namespace      → Logical grouping of resources (e.g. "eshop")
  └── Pod      → Smallest deployable unit. Runs one or more containers.
  └── Deployment → Manages Pods. Defines replicas, image, update strategy.
  └── Service  → Gives Pods a stable IP/DNS (Pods have ephemeral IPs)
  └── Secret   → Stores sensitive data (passwords, tokens) encrypted
  └── PVC      → PersistentVolumeClaim: requests persistent storage
```

**The relationship Deployment → Pod → Container:**
```
Deployment (ordering-api, replicas: 1)
  └── Pod (ordering-api-6695d6cfc4-q2cws)
        └── Container (image: geopapag/eshop-ordering-api:1.0.0)
```

---

## 7. Kubernetes Manifests — Deep Dive

### Folder Structure

```
k8s/
├── namespace.yaml              # Creates the "eshop" namespace
├── secrets.yaml                # Passwords for SQL Server, RabbitMQ
├── infrastructure/
│   ├── sqlserver.yaml          # SQL Server Deployment + Service + PVC
│   ├── rabbitmq.yaml           # RabbitMQ Deployment + Service + PVC
│   └── seq.yaml                # Seq (logging) Deployment + Service + PVC
└── services/
    ├── identity-api.yaml       # Identity API Deployment + Service
    ├── ordering-api.yaml       # Ordering API Deployment + Service
    ├── catalog-api.yaml        # Catalog API Deployment + Service
    └── api-gateway.yaml        # API Gateway Deployment + Service (LoadBalancer)
```

### `k8s/namespace.yaml`

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: eshop
```

Creates a namespace called "eshop". All project resources live here,
isolated from other projects running on the same cluster.

### `k8s/secrets.yaml`

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: eshop-secrets
  namespace: eshop
type: Opaque
stringData:
  sa-password: "YourStr0ng!Pass"
  rabbitmq-user: "guest"
  rabbitmq-pass: "guest"
```

Stores passwords securely. Services read them as environment variables
without the values ever appearing in the container YAML files.

### `k8s/infrastructure/sqlserver.yaml`

```yaml
# ── PersistentVolumeClaim: request 2 GB of storage ───────────────────
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: sqlserver-pvc
  namespace: eshop
spec:
  accessModes:
    - ReadWriteOnce        # One node at a time
  resources:
    requests:
      storage: 2Gi         # Request 2 GB

---
# ── Deployment: how SQL Server runs ──────────────────────────────────
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sqlserver
  namespace: eshop
spec:
  replicas: 1              # 1 instance
  template:
    spec:
      containers:
      - name: sqlserver
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
        - name: MSSQL_SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: eshop-secrets    # Reads from the Secret created above
              key: sa-password
        resources:
          requests:                  # Minimum resources the container needs
            memory: "1Gi"
            cpu: "500m"             # 500m = 0.5 CPU cores
          limits:                    # Maximum resources it can consume
            memory: "2Gi"
            cpu: "1000m"
        readinessProbe:              # Checks if the container is ready to accept traffic
          exec:
            command: [sqlcmd ... "SELECT 1"]
          initialDelaySeconds: 30    # Wait 30s before starting checks
          failureThreshold: 10       # 10 failures → container = NotReady

---
# ── Service: stable DNS name for SQL Server ───────────────────────────
apiVersion: v1
kind: Service
metadata:
  name: sqlserver          # Other services connect using "sqlserver:1433"
  namespace: eshop
spec:
  ports:
  - port: 1433
    targetPort: 1433
```

### `k8s/services/ordering-api.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ordering-api
  namespace: eshop
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: ordering-api
        image: geopapag/eshop-ordering-api:1.0.0   # Image pulled from Docker Hub
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Docker"                           # Uses appsettings.Docker.json
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "500m"
        readinessProbe:
          httpGet:
            path: /health           # Calls the service's health endpoint
            port: 8080
          initialDelaySeconds: 20
          periodSeconds: 10
          failureThreshold: 5
```

### `k8s/services/api-gateway.yaml`

```yaml
# The gateway uses LoadBalancer type — accessible from the browser
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: eshop
spec:
  type: LoadBalancer        # Creates an external IP (Docker Desktop = localhost)
  ports:
  - port: 5000             # External port
    targetPort: 8080       # Internal container port
```

The `LoadBalancer` type is what makes the API Gateway reachable at
`http://localhost:5000`. The other services use the default `ClusterIP` type —
accessible only within the cluster.

---

## 8. Deploying to Kubernetes

### Prerequisites

- Docker Desktop with Kubernetes enabled (Settings → Kubernetes → Enable Kubernetes)
- All images pushed to Docker Hub

### Step-by-step Deploy

```bash
# 1. Create the namespace
kubectl apply -f k8s/namespace.yaml

# 2. Create secrets (passwords)
kubectl apply -f k8s/secrets.yaml

# 3. Deploy infrastructure (SQL Server, RabbitMQ, Seq)
kubectl apply -f k8s/infrastructure/

# 4. Deploy application services
kubectl apply -f k8s/services/

# ─── ALTERNATIVELY: deploy everything at once ─────────────────────────
# -R = recursive, required because k8s/ has subdirectories (infrastructure/, services/)
kubectl apply -R -f k8s/
```

### Verify the Deployment

```bash
# List all pods
kubectl get pods -n eshop

# Expected output:
# NAME                            READY   STATUS    RESTARTS   AGE
# api-gateway-7889c9fb6-p8k8x     1/1     Running   0          5m
# catalog-api-778dc7fd49-w22gh    1/1     Running   0          5m
# identity-api-74cdf45844-qmptc   1/1     Running   0          5m
# ordering-api-6695d6cfc4-q2cws   1/1     Running   0          5m
# rabbitmq-fcc887d86-l9vt6        1/1     Running   0          5m
# seq-7d58db6f59-vn8hs            1/1     Running   0          5m
# sqlserver-bbc9565d6-rcpvs       1/1     Running   0          5m

# List services (IPs and ports)
kubectl get services -n eshop

# Inspect a pod in detail (useful for debugging)
kubectl describe pod <pod-name> -n eshop

# Stream pod logs
kubectl logs <pod-name> -n eshop
kubectl logs -f <pod-name> -n eshop   # real-time
```

### Management Commands

```bash
# Force restart a deployment (pulls the latest image)
kubectl rollout restart deployment/ordering-api -n eshop

# Update the image to a new version
kubectl set image deployment/ordering-api \
  ordering-api=geopapag/eshop-ordering-api:1.0.1 -n eshop

# Watch a rolling update
kubectl rollout status deployment/ordering-api -n eshop

# Delete everything (WARNING: all data is lost)
kubectl delete namespace eshop
```

---

## 9. CI/CD with GitHub Actions

### What is CI/CD?

**CI (Continuous Integration):** Every time you push code, tests and the build run
automatically. You find out immediately if something is broken.

**CD (Continuous Delivery):** Every time you merge to main, new Docker images are
built and published automatically.

```
Developer push → CI: Build + Test → (if passed) → CD: Build Image → Push to Docker Hub
                  ↑                                                          ↓
               develop branch                               Update k8s/*.yaml (new image tag)
                                                                             ↓
                                                              git commit + push [skip ci]
                                                                             ↓
                                                            git pull origin main  (manual)
                                                                             ↓
                                                         kubectl apply -R -f k8s/  (manual)
```

> **Why `git pull` before `kubectl apply`?**
> The CD pipeline automatically edits the `k8s/services/*.yaml` files (replacing the
> image tag) and commits that change back to the `main` branch. If you run
> `kubectl apply` without pulling first, your local YAML files still have the
> **old** image tag and Kubernetes will not pick up the new image.

### File Structure

```
.github/
└── workflows/
    ├── ci.yml    # CI Pipeline
    └── cd.yml    # CD Pipeline
```

### CI Pipeline: `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches:
      - develop
      - 'feature/**'        # Runs on every feature branch push
  pull_request:
    branches:
      - develop
      - main                # Runs when a PR is opened

jobs:
  build-and-test:
    runs-on: ubuntu-latest  # GitHub provides a free Ubuntu runner
    env:
      FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true

    steps:
      - uses: actions/checkout@v4         # Downloads the source code
      - uses: actions/setup-dotnet@v4     # Installs .NET 8
        with:
          dotnet-version: '8.0.x'

      - run: dotnet restore EShop.sln
      - run: dotnet build EShop.sln --no-restore --configuration Release

      # Only unit tests run here — integration tests require SQL Server
      - run: dotnet test tests/EShop.Identity.UnitTests ...   # 22 tests
      - run: dotnet test tests/EShop.Ordering.UnitTests ...   # 52 tests

      - uses: actions/upload-artifact@v4  # Saves test results as downloadable artifact
        with:
          name: test-results
          path: '**/*.trx'
```

### CD Pipeline: `.github/workflows/cd.yml`

```yaml
name: CD

on:
  push:
    branches:
      - main          # Runs after every merge to main
    tags:
      - 'v*.*.*'      # Also runs when you push a git tag like v1.0.1

env:
  DOCKER_HUB_USERNAME: geopapag
  FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true

permissions:
  contents: write     # Required to commit updated manifests back to the repo

jobs:
  # ── Job 1: Build & Push (4 parallel jobs) ────────────────────────
  build-and-push:
    strategy:
      matrix:
        service:
          - { name: identity-api, dockerfile: src/Services/Identity/... }
          - { name: ordering-api, dockerfile: src/Services/Ordering/... }
          - { name: catalog-api,  dockerfile: src/Services/Catalog/...  }
          - { name: api-gateway,  dockerfile: src/ApiGateway/...        }
    steps:
      - uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}   # GitHub Secret
          password: ${{ secrets.DOCKER_PASSWORD }}   # GitHub Secret

      - uses: docker/metadata-action@v5
        # Automatically generates image tags:
        # push to main   → latest + sha-abc1234
        # git tag v1.0.1 → 1.0.1 + 1.0 + latest + sha-abc1234

      - uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          cache-from: type=gha   # GitHub Actions cache → faster subsequent builds

  # ── Job 2: Update K8s Manifests ──────────────────────────────────
  update-manifests:
    needs: build-and-push   # Runs AFTER all build-and-push jobs complete
    steps:
      - name: Compute image tag
        # push to main   → sha-abc1234
        # git tag v1.0.1 → 1.0.1

      - name: Update image tags
        run: |
          # Replaces the tag in each K8s YAML file
          sed -i "s|image: geopapag/eshop-ordering-api:.*|...:sha-abc1234|g" \
            k8s/services/ordering-api.yaml
          # (same for the other 3 services)

      - name: Commit updated manifests
        run: |
          git commit -m "chore(k8s): update image tags to sha-abc1234 [skip ci]"
          git push
          # [skip ci] → prevents CI from triggering again for this commit
```

### GitHub Secrets

Credentials are **never** written in code. They are stored securely on GitHub:

```
GitHub Repo → Settings → Secrets and variables → Actions → New repository secret

DOCKER_USERNAME = geopapag
DOCKER_PASSWORD = <Docker Hub Access Token>
```

The workflow reads them as `${{ secrets.DOCKER_USERNAME }}` — they never appear
in logs or source code.

**Docker Hub Access Token** (recommended over your password):
`hub.docker.com → Account Settings → Personal access tokens → Generate new token`

---

## 10. Full Workflow from Scratch

If you need to rebuild the project from zero, follow this order:

```bash
# ── STEP 1: Build Docker images ──────────────────────────────────────
docker build -t geopapag/eshop-identity-api:1.0.0 \
  -f src/Services/Identity/EShop.Identity.API/Dockerfile .

docker build -t geopapag/eshop-ordering-api:1.0.0 \
  -f src/Services/Ordering/EShop.Ordering.API/Dockerfile .

docker build -t geopapag/eshop-catalog-api:1.0.0 \
  -f src/Services/Catalog/EShop.Catalog.API/Dockerfile .

docker build -t geopapag/eshop-api-gateway:1.0.0 \
  -f src/ApiGateway/EShop.ApiGateway/Dockerfile .

# ── STEP 2: Push to Docker Hub ───────────────────────────────────────
docker login
docker push geopapag/eshop-identity-api:1.0.0
docker push geopapag/eshop-ordering-api:1.0.0
docker push geopapag/eshop-catalog-api:1.0.0
docker push geopapag/eshop-api-gateway:1.0.0

# ── STEP 3: Deploy to Kubernetes ─────────────────────────────────────
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/infrastructure/
kubectl apply -f k8s/services/

# ── STEP 4: Verify ───────────────────────────────────────────────────
kubectl get pods -n eshop
# Wait until all pods show 1/1 Running

# ── STEP 5: Access ───────────────────────────────────────────────────
# API Gateway: http://localhost:5000
# Seq logs:    requires kubectl port-forward (see below)

kubectl port-forward svc/seq 8081:80 -n eshop
# Seq UI: http://localhost:8081
```

### After Every Code Change (with CI/CD active)

```
1. git push origin develop     → CI runs automatically (build + 74 tests)

2. Merge to main               → CD runs automatically:
                                   - Builds 5 Docker images (4 services + web)
                                   - Pushes to Docker Hub with a new sha tag
                                   - Edits k8s/services/*.yaml with the new tag
                                   - Commits and pushes back to main [skip ci]

3. git pull origin main        → Sync the updated k8s/*.yaml files locally
                                   (the CD pipeline committed them — without this
                                    step your local files still have the old tag)

4. kubectl apply -R -f k8s/    → Apply the updated manifests to the cluster
                                   (-R = recursive, required for subdirectories)

5. kubectl rollout status deployment/<name> -n eshop
                               → Confirm the new pod is Running
```

---

*Last updated: May 2026*
