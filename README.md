# Insurance Claims API

ASP.NET Core (.NET 10) API to create, read and delete marine insurance **covers** and the **claims** made against them.
Claims and covers are stored in MongoDB; every create/delete is audited to SQL Server.

The original assignment is in [docs/README.md](docs/README.md).

## Prerequisites

- .NET 10 SDK
- Docker Desktop (running). The API and the integration tests start MongoDB and SQL Server with Testcontainers,
  so no local database installation is needed.

## Run the API

```bash
dotnet run --project Claims --launch-profile Claims
```

Swagger UI: https://localhost:7052/swagger. The first start pulls the Docker images and can take a few minutes.

## Run the tests

```bash
dotnet test
```

## Architecture

One API project with simple logical layers:

| Folder         | Responsibility |
|----------------|----------------|
| `Controllers/` | HTTP only: bind the request, call a service, return the HTTP result. |
| `Services/`    | Business operations (`ClaimService`, `CoverService`) and premium pricing (`PremiumCalculator`). |
| `Persistence/` | MongoDB access through EF Core (`ClaimsContext`, `ClaimRepository`, `CoverRepository`). |
| `Auditing/`    | Audit records in SQL Server (`AuditContext`, `Auditer`). |
| `Domain/`      | Entities and enums (`Claim`, `Cover`). |

Design choices:

- Interfaces exist only where they help: repositories and the auditer, so services can be unit tested without databases.
  Services and the premium calculator are concrete classes because there is only one implementation and one consumer.
- Repositories are small and specific to one entity. No generic repository, CQRS or mediator, because the domain is small.
- All dependencies are registered in `Program.cs`; nothing is created with `new` inside controllers.
