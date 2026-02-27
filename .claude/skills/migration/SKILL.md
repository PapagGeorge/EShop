# /migration — EF Core Migration Commands

Manage Entity Framework Core migrations for EShop services.

## Usage
- `/migration add <Name> [service]` — Create a new migration
- `/migration update [service]` — Apply pending migrations to database
- `/migration list [service]` — List all migrations
- `/migration remove [service]` — Remove the last migration

Service defaults to `ordering` if not specified. Valid services: `identity`, `ordering`.

## Instructions

Map service names to project paths:

| Service | --project | --startup-project |
|---------|-----------|-------------------|
| `identity` | `src/Services/Identity/EShop.Identity.Infrastructure` | `src/Services/Identity/EShop.Identity.API` |
| `ordering` | `src/Services/Ordering/EShop.Ordering.Infrastructure` | `src/Services/Ordering/EShop.Ordering.API` |

Execute the corresponding EF Core command:

| Action | Command |
|--------|---------|
| `add <Name>` | `dotnet ef migrations add <Name> --project <proj> --startup-project <startup>` |
| `update` | `dotnet ef database update --project <proj> --startup-project <startup>` |
| `list` | `dotnet ef migrations list --project <proj> --startup-project <startup>` |
| `remove` | `dotnet ef migrations remove --project <proj> --startup-project <startup>` |

Ensure `dotnet-ef` tool is installed. If not, run: `dotnet tool install --global dotnet-ef`

After adding a migration, remind the user to review the generated migration file before applying it.
