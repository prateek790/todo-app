# User API

ASP.NET Core 10 Web API for the Angular to-do-list workspace's user directory.

## SQL Server and code-first migrations

The API uses EF Core with SQL Server and Redis for shared rate-limit counters. Start the included containers with a strong SQL Server SA password:

```sh
export SQL_SA_PASSWORD='ReplaceWithAStrongPassword123!'
docker compose up -d sqlserver redis
```

If SQL Server is already running in Docker, you can skip Compose and use its host, port, and credentials below.

Store the connection string locally with .NET user-secrets. Replace `<your-sa-password>` with the SQL Server SA password; do not commit credentials:

```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=TodoAppDb;User Id=sa;Password=<your-sa-password>;Encrypt=True;TrustServerCertificate=True"
```

Create/update the database from the EF Core code-first migration:

```sh
dotnet tool restore
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update
```

The migration creates the `Users` table and inserts the example user. To run the API:

```sh
dotnet run
```

The HTTPS launch profile listens at `https://localhost:7228`. Trust the local ASP.NET Core development certificate if it is not already trusted:

```sh
dotnet dev-certs https --trust
```

In Development:

- Interactive API documentation is available at `https://localhost:7228/swagger`.
- The OpenAPI document is available at `https://localhost:7228/openapi/v1.json`.

The API requires `ConnectionStrings__DefaultConnection`. Development settings already point Redis to `localhost:6379`; production must provide `ConnectionStrings__redis` through configuration or an environment variable.

When running the API directly with its Development launch profile, start Redis for authenticated API requests and the shared rate limit. Anonymous sign-up and sign-in requests do not need Redis. In Git Bash, from the `api` directory:

```sh
export SQL_SA_PASSWORD='ReplaceWithAStrongPassword123!'
docker compose up -d redis
dotnet run
```

## JWT authentication

User API endpoints require a bearer token. The API issues 30-minute JWTs, stores password hashes separately from user profile data, and persists revoked token IDs so logout invalidates the token immediately. Existing users have no password until one is provisioned.

Set a private signing key and a separate provisioning key in local user-secrets (never commit either value):

```sh
cd api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 64)"
dotnet user-secrets set "Auth:ProvisioningKey" "<your-own-random-secret-at-least-32-characters>"
```

For a deployed environment, supply the corresponding `Jwt__SigningKey` and `Auth__ProvisioningKey` environment variables through its secret manager. The signing key must be base64-encoded and contain at least 256 bits of random material.

New users can register from the sign-in form or by calling `POST /api/auth/register` with name, email, mobile, gender, date of birth, and a password of at least 12 characters. Registration creates a regular user account and immediately signs it in. The profile is saved in `Users`; the one-way password hash is saved in `UserCredentials`, linked by the generated user ID. Duplicate email addresses are rejected.

Provision a password once for each existing active user at `POST /api/auth/provision-password`, using the provisioning key in the request. Accounts that already have a password cannot be provisioned again. Then sign in through the UI at `/login` or call `POST /api/auth/login`. The header profile icon shows the signed-in user's name and account type; **Sign out** revokes the token and clears the browser session.

The Angular app keeps the access token in session storage and sends it as a bearer token. This is a browser-session token; signing out or closing the tab clears it. Protect the signing and provisioning keys, and use HTTPS outside local development.

Every authenticated user is limited to 100 API requests per UTC hour. Redis stores the counter atomically, so the limit is shared across API instances. Requests beyond the limit receive `429 Too Many Requests` with `Retry-After` and rate-limit headers. If Redis is unavailable, authenticated requests receive `503 Service Unavailable` rather than bypassing the limit. Anonymous authentication endpoints are not included in this per-user limit.

When the model changes, create and apply a migration:

```sh
dotnet ef migrations add DescribeYourChange
dotnet ef database update
```

The Compose file persists SQL Server data in the `sqlserver-data` Docker volume. Use `docker compose down` to stop the container without deleting data. Avoid `docker compose down -v` unless you intend to delete that database volume.

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/users` | List users |
| `GET` | `/api/users/{id}` | Get one user |
| `POST` | `/api/users` | Create a user |
| `PUT` | `/api/users/{id}` | Update a user |
| `DELETE` | `/api/users/{id}` | Delete a user |

The API accepts the Angular user fields: `name`, `email`, `mobile`, `gender`, `dob`, `isActive`, `range`, and `userType`. IDs are assigned by the database. Invalid input returns `400`, missing users return `404`, successful creation returns `201`, and successful deletion returns `204`.

The API allows requests from Angular development origins `http://localhost:4200` and `http://127.0.0.1:4200`.

## Run the UI and API with Aspire

Start the distributed app from the AppHost directory:

```sh
cd ../TodoApp.AppHost
aspire run
```

Aspire starts the API using its HTTPS launch profile and the Angular development server. Open the UI and API endpoints from the Aspire dashboard. Angular proxies `/api` requests to the API's HTTPS endpoint, so the browser uses the UI's origin and the API URL does not need to be hard-coded.

The API reads the existing `ConnectionStrings:DefaultConnection` user-secret. SQL Server remains managed by `api/docker-compose.yml`; start that container separately if it is not already running. Aspire starts Redis for the API. The AppHost doesn't recreate or migrate the database.

To run either project without Aspire, start the API as described above and run `npm start` from `todo-app` with `API_HTTPS` set to the API HTTPS URL.

Example requests are in `api.http`.
