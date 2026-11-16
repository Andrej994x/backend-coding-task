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

Tests in `Claims.Tests/Unit` need no Docker. Tests in `Claims.Tests/Integration` start the real API with
Testcontainers (trait `Category=Integration`). To run only the unit tests:

```bash
dotnet test -- --filter-not-trait "Category=Integration"
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
- Requests use small DTOs (`Contracts/`), so clients cannot send `Id` or `Premium`; the server always sets them.

## Validation rules

Business rules live in `Validation/` and run in the services before anything is saved.
A broken rule returns `400 Bad Request` with a `ValidationProblemDetails` body (same shape as model binding errors).
A missing required field is also a `400`. Unknown ids return `404` on `GET` and `DELETE`.

| Entity | Rule |
|--------|------|
| Claim  | The related cover must exist. |
| Claim  | `DamageCost` cannot exceed 100,000 (exactly 100,000 is allowed). |
| Claim  | `Created` must be within the cover period. The first and the last day of the cover are allowed. |
| Cover  | `StartDate` cannot be in the past (today is allowed). |
| Cover  | `EndDate` must be after `StartDate`. |
| Cover  | The period cannot exceed one calendar year (`StartDate.AddYears(1)` is the latest allowed `EndDate`). |
| Both   | The enum type must be a known value. |

The premium quote endpoint (`POST /Covers/compute`) uses the same cover rules, because only such a cover can be bought.

Date assumptions:

- All dates are UTC. A date sent without an offset (`"2027-01-01"`) is treated as UTC; a date with an offset is
  converted to UTC. Before this change the Mongo driver treated such values as server local time and shifted them.
  Existing data is not migrated: the local setup always starts with empty containers.
- Rules compare calendar dates; the time of day is ignored.
- A cover from 29 February can run until 28 February of the next year (this is how `AddYears(1)` works).

Not added on purpose, because the assignment does not ask for it: a minimum `DamageCost`, and blocking the
deletion of a cover that still has claims.
