# /docker — Docker Compose Operations

Manage the EShop Docker stack (SQL Server, RabbitMQ, Seq, and application services).

## Usage
- `/docker up` — Start all services in detached mode
- `/docker down` — Stop and remove all containers
- `/docker logs [service]` — Show logs (optionally for a specific service)
- `/docker rebuild [service]` — Rebuild and restart a specific service (or all)
- `/docker status` — Show running containers and their ports

## Instructions

Execute the appropriate docker compose command:

| Argument | Command |
|----------|---------|
| `up` | `docker compose up -d` |
| `down` | `docker compose down` |
| `logs` | `docker compose logs -f --tail=50` (or `docker compose logs -f --tail=50 <service>`) |
| `logs <service>` | `docker compose logs -f --tail=50 <service>` |
| `rebuild` | `docker compose up -d --build` |
| `rebuild <service>` | `docker compose up -d --build <service>` |
| `status` | `docker compose ps` |

Service names: `sqlserver`, `rabbitmq`, `seq`, `identity-api`, `ordering-api`, `catalog-api`, `api-gateway`

After `up` or `rebuild`, wait a few seconds and run `docker compose ps` to verify all containers are healthy.
Report the accessible URLs:
- Gateway: http://localhost:5000
- Seq: http://localhost:8081
- RabbitMQ: http://localhost:15672
