# /test — Run Tests

Run EShop test suites. Accepts an optional argument to scope the test run.

## Usage
- `/test` — Run all tests (74 total)
- `/test identity` — Run Identity unit tests only
- `/test ordering` — Run Ordering unit tests only
- `/test integration` — Run Ordering integration tests only

## Instructions

Based on the argument provided (or no argument for all tests), run the appropriate command:

| Argument | Command |
|----------|---------|
| *(none)* | `dotnet test EShop.sln` |
| `identity` | `dotnet test tests/EShop.Identity.UnitTests` |
| `ordering` | `dotnet test tests/EShop.Ordering.UnitTests` |
| `integration` | `dotnet test tests/EShop.Ordering.IntegrationTests` |

After running:
1. Report the total number of tests passed/failed/skipped
2. If any tests fail, show the failure details and suggest fixes
3. Do NOT modify any code unless the user explicitly asks for a fix
