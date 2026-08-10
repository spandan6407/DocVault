# DocVault — Project Overview

This document gives a concise overview of the DocVault solution, how services connect, recommended Docker/dev environment, message broker (RabbitMQ), storage (Azurite/Azure Blob emulation), and high-level data flows. Use this as a quick reference when developing, debugging, or running the solution locally.

---

## Repository layout (high level)

- backend/
  - document-knowledge-management/
    - DocVault.DocumentKnowledgeManagement.API (Document API)
    - DocVault.DocumentKnowledgeManagement.Infrastructure
    - DocVault.DocumentKnowledgeManagement.Application
    - DocVault.DocumentKnowledgeManagement.Domain
  - user-management-service/
    - DocVault.UserManagement.API (User API)
    - DocVault.UserManagement.Infrastructure
    - DocVault.UserManagement.Application
    - DocVault.UserManagement.Domain
  - shared/
    - DocVault.Shared.Contracts
- aspire/
  - DocVault.ServiceDefaults
- frontend/
  - docvault-ui/ (React Vite app)

Note: projects target .NET 10.

---

## Runtime components & expected ports (local dev)

These are used in the code and in frontend API clients:

- User Management API: http://localhost:5261 (USER_API)
- Document Knowledge Management API: http://localhost:5078 (DOC_API)
- Frontend (Vite): http://localhost:5173
- RabbitMQ (message broker): amqp://guest:guest@localhost:5672 (example)
- Azurite (Azure blob storage emulator): blob endpoint http://127.0.0.1:10000 (or configured)
- SQL Server for each backend (localdb / container) — connection string example below

Adjust ports if your environment uses different ports.

---

## Environment variables (recommended)

Example env vars for each API (use secrets manager in prod):

- ASPNETCORE_ENVIRONMENT=Development
- ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=DocVault_UserManagement;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
- ConnectionStrings__DocumentDb="Server=localhost,1433;Database=DocVault_Documents;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"
- Blob__ConnectionString="UseDevelopmentStorage=true"    # for Azurite / storage emulator
- RabbitMQ__ConnectionString="amqp://guest:guest@localhost:5672"
- Jwt__Issuer, Jwt__Key, Jwt__Audience — jwt configuration

Each project contains appsettings.json scaffolding — review them and set environment-specific overrides.

---

## Docker / docker-compose (suggested)

Below is a minimal docker-compose snippet you can use locally to spin up required infra (SQL Server, RabbitMQ, Azurite). It does NOT build the .NET projects — it only shows the infra you can run alongside your locally-built apps.

```yaml
version: '3.8'
services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "Your_password123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    healthcheck:
      test: ["CMD", "bash", "-c", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -Q \"SELECT 1\""]"]

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672" # admin UI

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    command: "azurite-blob --blobHost 0.0.0.0 --loose"
    ports:
      - "10000:10000"

# optional: frontend (serve static build) or other services
```

Run: `docker compose up -d` and then configure backend apps to point to these endpoints.

---

## Message broker (RabbitMQ)

- Purpose: used for application events (user created, role assigned, etc.).
- Typical exchange/queues: configured by MassTransit in the services.
- Use `amqp://guest:guest@localhost:5672` locally unless you changed credentials.
- RabbitMQ management UI is available at http://localhost:15672 (guest/guest by default in the example).

Detailed usage of RabbitMQ in this solution

- Why RabbitMQ: it decouples services and enables asynchronous, event-driven communication. Services publish domain events (for example: user created, role assigned) and other services can subscribe to those events without tight coupling.
- What is published (examples):
  - UserCreatedEvent (published when a new user is created) — other services can react (indexing, audit, notifications).
  - ProjectHeadAssignedEvent / UserRoleAssignedEvent (published when a user is promoted/demoted) — used to notify downstream systems of role changes.
- How it's wired: MassTransit is used as the abstraction over AMQP. Each service configures its endpoints and consumers in startup/configuration code. The queue and exchange names are defined in the MassTransit configuration (check the service's startup code for exact names).
- Failure and retry behavior: MassTransit + RabbitMQ support retry policies and dead-lettering; long-running or transient failures should be handled by configured retries in each consumer.
- Recommendations: ensure RabbitMQ is up before starting services that publish/consume events; use the management UI to inspect exchanges/queues during debugging.

---

## Storage (Blob / Azurite)

- Blob container name used in code: `documents`.
- For local development you can use Azurite and the `UseDevelopmentStorage=true` connection string or explicit Azurite connection: `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vd...;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;`
- Ensure the container exists or let the code call CreateIfNotExistsAsync.

---

## Authentication & Authorization

- JWT bearer tokens are used. Frontend stores token in localStorage (key `token`) and sends Authorization header.
- Role checks are performed by `[Authorize(Roles = "...")]` attributes. Some environments/tokens may not include role claims in the expected place — several server endpoints include fallbacks that assert roles using UserManager if needed.

---

## Key backend endpoints (examples)

- Documents (Doc API)
  - GET /api/projects               -> list projects (now available to roles: Admin,ProjectHead,User)
  - GET /api/projects/{id}/documents
  - POST /api/documents             -> upload file (multipart/form-data)
  - POST /api/documents/compose    -> create typed document (server generates .docx)
  - GET /api/documents/search?q=

- Users (User API)
  - POST /api/users/project-change-request   -> submit project-change request (User/ProjectHead)
  - GET  /api/project-change-requests        -> Admin: list pending requests
  - PUT  /api/project-change-requests/{id}/approve
  - PUT  /api/project-change-requests/{id}/reject
  - PUT  /api/users/{id}/change-project      -> Admin manual change

---

## High-level data flows

1. Document Upload (file)
   - Frontend: form uploads file (title/description + file) to Doc API `/api/documents` (multipart).
   - Doc API: validates role & project (BR-007), stores blob to `documents/{projectId}/{guid}_{filename}` and saves metadata to Documents table.
   - Doc API returns DocumentResponse to frontend which renders cards (DocumentCard).

2. Compose Document (typed)
   - Frontend: posts { Title, Description, Content, ProjectId } to Doc API `/api/documents/compose`.
   - Doc API: validates project & role, uses DocumentFormat.OpenXml to build a .docx in-memory, uploads to blob storage, and stores metadata (FileName ends with .docx, ContentType = application/vnd.openxmlformats-officedocument.wordprocessingml.document).

3. Download
   - Frontend GET triggers Doc API `/api/documents/{id}/download`, Doc API fetches blob bytes and returns as blob with appropriate ContentType.

4. User Project Change Request
   - Frontend (User/ProjectHead) selects target project and POSTS to User API `/api/users/project-change-request`.
   - User API: authenticates the caller, server-side verifies role (fallback to UserManager roles if token claims are missing), optionally validates project exists (via Doc API). Persists ProjectChangeRequest (Pending).
   - Request lifecycle and messaging:
     - When a user submits a change request, the request is stored in the UserManagement database in the ProjectChangeRequests table with Status = "Pending" and metadata (UserId, CurrentProjectId, RequestedProjectId, RequestedAt).
     - The Admin UI periodically calls GET `/api/project-change-requests` to list pending requests.
     - When an admin approves a request, the API performs the following:
       1. Calls AdminChangeUserProjectAsync which updates the ApplicationUser.ProjectId and, if the user was a ProjectHead, demotes them to "User" to avoid multiple heads.
       2. Updates the ProjectChangeRequest.Status to "Approved" and sets ResolvedAt.
       3. Optionally publishes an event (e.g., UserProjectChangedEvent) to RabbitMQ via MassTransit so other services can react (audit logs, notifications).
     - When an admin rejects a request, the API updates the ProjectChangeRequest.Status to "Rejected" and sets ResolvedAt. Optionally a notification event can be published.
     - If the admin prefers to manually change a user's project without a pending request, the Admin API exposes `PUT /api/users/{id}/change-project` which invokes AdminChangeUserProjectAsync directly and records the change as an administrative action.
   - Admin UI: GET /api/project-change-requests lists pending requests. Admin can Approve or Reject.
   - Approve: calls UserService.AdminChangeUserProjectAsync which sets user.ProjectId = newProjectId and demotes ProjectHead to User (to avoid two heads). Request status updated to Approved.

5. Events & Message Broker
   - Some actions (user created, role assigned) publish events via MassTransit to RabbitMQ so other services can subscribe. Check mass transit configuration in services for topic names.

---

## Common issues & troubleshooting

- "Locked DLL" (MSB3021 / MSB3027): if Visual Studio/MSBuild cannot overwrite a DLL, stop the running process (debug session or a running API) and rebuild. Use `taskkill /PID <pid> /F` or stop from VS.

- Token role claims missing → 403 when calling endpoints protected with `[Authorize(Roles=...)]`. Several endpoints include code to fallback to `UserManager.GetRolesAsync(appUser)` if roles aren't present in claims. In local dev ensure tokens include role claims or use the server-side fallback.

- DocumentFormat.OpenXml issues: make sure required NuGet package is added in the Infrastructure project (DocumentFormat.OpenXml version specified). If enum types cannot be found, check package version and `using` statements. The code currently uses fully-qualified or aliased types to avoid ambiguity.

- Blob storage: if using Azurite, ensure the connection string is set and the container name `documents` is available; code calls CreateIfNotExistsAsync.

---

## How to run locally (recommended steps)

1. Start infra (Docker) if you want local RabbitMQ / SQL Server / Azurite:
   - `docker compose up -d` using the snippet above or your own compose file.
2. Configure environment variables (connection strings, Jwt settings) for each API project.
3. In each backend project (UserManagement.Infrastructure, DocumentKnowledgeManagement.Infrastructure) run EF migrations (if DB is empty):
   - `dotnet ef database update --project <InfrastructureProject> --startup-project <ApiProject>`
4. Run APIs (from Visual Studio or `dotnet run` in project folder).
5. Start frontend:
   - `cd frontend/docvault-ui`
   - `npm install` (or `pnpm`/`yarn`)
   - `npm run dev`
6. Open frontend (http://localhost:5173) and login, then exercise flows.

---

## Next steps / TODOs

- Standardize authentication tokens so role claims are included consistently.
- Replace inline styles in React components with the new CSS classes for consistent UI.
- Add integration tests for key flows (document upload, compose, project-change request).
- Harden error handling and return more granular error messages for client debugging.

---

If you want, I can:
- Generate a docker-compose that builds and runs the APIs + frontend as containers (complete local dev compose),
- Add a short Postman collection with example requests for the key APIs,
- Or continue and start fixing specific runtime issues you observe (paste errors / logs and I will iterate).


---

Created by: GitHub Copilot
Date: 2026-07-22
