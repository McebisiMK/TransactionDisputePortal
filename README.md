# TransactionDisputePortal

## Solution structure

```
TransactionDisputePortal.API             ASP.NET Core controllers, HTTP concerns, exception mapping
TransactionDisputePortal.Application     Use cases (CQRS commands/handlers), validation, DTOs, abstractions
TransactionDisputePortal.Domain          Entities and domain events, no framework dependencies
TransactionDisputePortal.Infrastructure  EF Core persistence, external integrations, DI wiring
```

## Running with Docker

```
docker compose up -d --build
```

| Service | URL |
|---|---|
| API (Swagger) | http://localhost:5000/swagger |
| Keycloak | http://localhost:8080 |
| SQL Server | `localhost,1433` (`sa` / `MSSQL_SA_PASSWORD`) |

The SQL Server `sa` password defaults to `Dispute_Portal_2026!` and can be overridden by setting `MSSQL_SA_PASSWORD` in the environment or a `.env` file next to `docker-compose.yml`.

Stop everything with `docker compose down`. Add `-v` to also delete the SQL Server and Keycloak volumes.

## Running locally

Start only the dependencies in Docker, then run the API from the IDE or CLI against LocalDB:

```
docker compose up -d keycloak
dotnet run --project TransactionDisputePortal.API --launch-profile http
```

## Database migrations

Migrations live in `TransactionDisputePortal.Infrastructure/Persistence/Migrations`. When `DataSeedingOptions:Enabled` is `true` (Development and Docker), pending migrations are applied and seed data is inserted at startup. It is `false` in `appsettings.json`, so other environments must apply migrations explicitly.

```
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Name> --project TransactionDisputePortal.Infrastructure --startup-project TransactionDisputePortal.API --output-dir Persistence/Migrations
dotnet ef database update --project TransactionDisputePortal.Infrastructure --startup-project TransactionDisputePortal.API
```

## Authentication

Authentication is handled by Keycloak. The realm is imported from `keycloak/realm-export.json` the first time Keycloak starts. Keycloak's data, including its token signing keys, is kept in the `keycloak-data` volume, so tokens stay valid across Keycloak restarts.

Keycloak skips a realm that already exists, so later changes to `realm-export.json` are not applied automatically. To re-import it, remove the volume and start Keycloak again:

```
docker compose rm -sf keycloak
docker volume rm transactiondisputeportal_keycloak-data
docker compose up -d keycloak
```

| Item | Value |
|---|---|
| Admin console | http://localhost:8080 (`admin` / `admin`) |
| Realm | `transaction-dispute-portal` |
| API audience client | `dispute-portal-api` |
| Swagger client | `dispute-portal-swagger` (public, Authorization Code + PKCE) |
| React client | `dispute-portal-web` (public, Authorization Code + PKCE, http://localhost:3000) |

Test users (password `Password1!`):

| Username | Role | Seed data |
|---|---|---|
| `test.customer` | customer | Cheque and credit card account, 50 transactions |
| `test.customer2` | customer | Cheque and credit card account, 50 transactions |
| `test.agent` | agent | |

The Keycloak users have fixed ids, and the seeder creates a matching customer for each customer user (linked by the token's `sub` claim) with about five months of transactions. The most recent ones are `Pending`, the rest `Posted`. Seeding only adds customers that don't exist yet, so it is safe on every start.

Run the API, open http://localhost:5000/swagger and use **Authorize** to sign in.

## API

All endpoints are under `/api/v1`. Dates in responses are UTC. The `fromDate` and `toDate` filters are calendar days in South African time (Africa/Johannesburg).

| Method | Endpoint | Role | Description |
|---|---|---|---|
| `GET` | `/transactions` | customer | The signed-in customer's transactions, newest first. Filters: `fromDate`, `toDate` (inclusive, `yyyy-MM-dd`), `search` (merchant or reference), `status` (`Pending`/`Posted`), `accountId`. Paging: `pageNumber`, `pageSize` (max 100). |
| `GET` | `/transactions/{id}` | customer | One of the customer's transactions. Another customer's transaction returns `404`. |
| `POST` | `/disputes` | customer | Disputes one of the customer's transactions. Body: `transactionId`, `reason`, `description` (required when `reason` is `Other`, max 1000 characters). Returns `201` with the dispute and a `Location` header. |
| `GET` | `/disputes` | customer | The signed-in customer's dispute history, newest first. Filters: `fromDate`, `toDate` (inclusive, `yyyy-MM-dd`, on the submission date), `status` (`Submitted`/`UnderReview`/`Approved`/`Rejected`/`Withdrawn`). Paging: `pageNumber`, `pageSize` (max 100). |
| `GET` | `/disputes/{id}` | customer | One of the customer's disputes, with a summary of the transaction and the status timeline (oldest first: who changed it, when and why). Another customer's dispute returns `404`. |

Transaction responses include `isDisputable` and `openDisputeId`, so clients don't need to re-implement the dispute rules.

### Dispute rules

- A customer can only dispute their own transactions (`404` otherwise).
- Only `Posted` transactions can be disputed, and only within 120 days of the transaction date (`409`).
- A transaction can have only one open (`Submitted` or `UnderReview`) dispute at a time (`409`). This is checked by the handler and enforced by a filtered unique index, so two simultaneous requests can't both succeed.
- Reasons: `UnauthorisedTransaction`, `DuplicateCharge`, `IncorrectAmount`, `GoodsOrServicesNotReceived`, `NotAsDescribed`, `CancelledSubscription`, `RefundNotReceived`, `Other`.
- Each dispute gets a readable reference such as `DSP-2026-000123`, numbered from a SQL Server sequence.
- A new dispute starts as `Submitted` with a first entry in its status history, and a simulated "Dispute received" notification is logged.
