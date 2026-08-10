# DocVault — Project Documentation

## Overview
DocVault is a microservices-based document and user management system composed of two main backend services and an AppHost orchestration used for local development/test runs:

- document-knowledge-management (Document Knowledge Management Service)
- user-management-service (User Management Service)
- AppHost (Aspire/DocVault.AppHost) that orchestrates local containers required by the system (RabbitMQ, SQL Server, Azurite)

This file summarizes the folder layout, APIs, message flows, configuration, business rules, health & readiness checks, test steps, troubleshooting guidance, and scope for future fixes. Use this as a single reference when handing the project to an AI or another engineer.

---

## Repository (high-level) Folder Structure

- aspire/
  - DocVault.AppHost/Program.cs — AppHost orchestration (starts RabbitMQ, SQL Server, Azurite and the two API projects)
- backend/
  - document-knowledge-management/
    - DocVault.DocumentKnowledgeManagement.API/Program.cs
    - DocVault.DocumentKnowledgeManagement.API/Controllers/
      - ProjectsController.cs
      - DocumentsController.cs
      - Auth/Debug endpoints
    - DocVault.DocumentKnowledgeManagement.Application/Interfaces/
    - DocVault.DocumentKnowledgeManagement.Infrastructure/Services/
    - DocVault.DocumentKnowledgeManagement.Domain/
  - user-management-service/
    - DocVault.UserManagement.API/Program.cs
    - DocVault.UserManagement.API/Controllers/
      - AuthController.cs
      - UsersController.cs
      - HealthController.cs
    - DocVault.UserManagement.Application/DTOs/
    - DocVault.UserManagement.Infrastructure/Identity/
    - DocVault.UserManagement.Infrastructure/Messaging/Consumers/
      - ProjectCreatedConsumer.cs
    - DocVault.UserManagement.Infrastructure/Services/
- PROJECT_DOCUMENTATION.md (this file)

Note: Additional shared projects exist for Application/Domain layers under each service.

---

## Services, Ports & Local Runtime
When run via AppHost (aspire run), the environment creates containerized dependencies and maps dynamic ports. Typical ports observed in AppHost UI (example):

- RabbitMQ management UI: http://localhost:<management-port>
- RabbitMQ AMQP (broker): tcp://localhost:<amqp-port>
- Document service (HTTP): http://localhost:5078 (Scalar UI at /scalar)
- User service (HTTP): http://localhost:5261
- SqlServer: tcp://localhost:<sql-port>
- Azurite (Storage emulator): http://localhost:<blob-port>

These ports may change during each AppHost run; the app code resolves a ConnectionStrings.rabbitmq or environment variable RABBITMQ__CONNECTIONSTRING to discover the AMQP endpoint.

---

## Configuration Keys (important)

- appsettings.json keys used:
  - ConnectionStrings:DefaultConnection — SQL Server connection string
  - ConnectionStrings:rabbitmq — optional amqp URI (amqp://host:port)
  - RabbitMQ:Host, RabbitMQ:Port, RabbitMQ:Username, RabbitMQ:Password — explicit broker settings
  - JwtSettings: SecretKey, Issuer, Audience, ExpiryInMinutes
  - UserService:HealthUrl, WaitTimeoutSeconds — document service waits for user readiness
  - HttpsPort or ASPNETCORE_HTTPS_PORT — optional HTTPS port for redirect

- Environment variables recognized (priority over config):
  - RABBITMQ__CONNECTIONSTRING — full amqp URI to broker
  - ASPNETCORE_HTTPS_PORT — port for UseHttpsRedirection

---

## Authentication and JWT

- User Management service issues JWT tokens at /api/auth/login using AuthService.
- Token claims include:
  - sub (JwtRegisteredClaimNames.Sub) = user.Id
  - email (JwtRegisteredClaimNames.Email)
  - jti
  - role (both ClaimTypes.Role and a `role` claim are included)
  - projectId (custom claim)
  - firstName, lastName
- Both services disable automatic inbound claim mapping (JwtSecurityTokenHandler.DefaultMapInboundClaims = false) and configure TokenValidationParameters to use NameClaimType = "sub" and RoleClaimType = "role".
- The User Management service copies role claims between `role` and ClaimTypes.Role on token validation and also registers an IClaimsTransformation to normalize claims.

---

## APIs (endpoints summary)

User Management API (base /api)
- POST /api/auth/login — Login returns a JWT
- GET /api/auth/me — Get current user
- POST /api/users — Create user (Admin only)
- GET /api/users — List users (Admin only)
- GET /api/user-projects — Diagnostic: list projects known to user service (Admin)
- PUT /api/users/{id}/assign-project-head — Assign project head (Admin)
- GET /health/ready — Readiness endpoint (checks DB and RabbitMQ TCP connectivity)
- GET /health/liveness — Liveness endpoint

Document Knowledge API (base /api)
- POST /api/projects — Create project (Admin only)
- GET /api/projects — List projects (Admin)
- GET /api/projects/{id} — Get project by id (Admin, ProjectHead, User)
- GET /api/projects/me — Get project assigned to current user (My Project endpoint)
- POST /api/documents — Upload document (ProjectHead, User)
- GET /api/projects/{projectId}/documents — List documents for project
- GET /api/documents/{id}/download — Download document
- DELETE /api/documents/{id} — Delete document
- GET /api/debug/claims — Diagnostic endpoint to inspect received claims (Authorize)

---

## Message Flows (RabbitMQ + MassTransit)

- Event: ProjectCreatedEvent (published by document service after project creation)
  - Exchange name: "project-created" (fanout).
  - Document service publishes ProjectCreatedEvent via IPublishEndpoint.Publish.
  - User management registers a durable queue "user-management-project-created" bound to exchange "project-created".
  - ProjectCreatedConsumer consumes the event and inserts a record in UserProjects linking a user (admin if found by email) to the project.
  - Queue is durable and AutoDelete=false, so messages persist if consumer is temporarily down.

- Event: UserCreatedEvent (published by user service after user creation) — consumed by other services as needed.

---

## Business Rules (as implemented)

- Roles: Admin, ProjectHead, User.
- Admin can create projects, users, assign project heads, list all data.
- ProjectHead can view and manage documents within their assigned project and view members.
- User can view project details and manage their own document uploads within the assigned project.
- Users/ProjectHeads can only access documents for their own assigned project (enforced by comparing JWT projectId claim to resource projectId). Admin bypasses project restriction.
- When a project is created, a ProjectCreatedEvent is published; consumer creates a UserProject row to represent that project in user-service.
- When creating a user, user-service checks that the target project exists in UserProjects (unless admin bypass is used to avoid race conditions).

---

## Health & Readiness

- User readiness checks:
  - DB connectivity via EF Core CanConnectAsync
  - TCP connectivity to RabbitMQ host/port
  - Responds 200 only if both checks pass (used by document service at startup)

- Document service: at startup, polls user readiness endpoint for up to UserService:WaitTimeoutSeconds (default 60s) before continuing. If user service is not ready, it logs and continues (best-effort).

---

## Troubleshooting Checklist (quick)

1. Verify RabbitMQ connectivity
   - Confirm broker AMQP port (AppHost UI shows tcp://localhost:<port>).
   - Set RABBITMQ__CONNECTIONSTRING or ConnectionStrings.rabbitmq = "amqp://localhost:<port>" in both service configs.
   - Ensure services log the resolved host/port at startup.

2. Check that the user-service consumer queue exists and is durable
   - RabbitMQ management UI -> Exchanges & Queues -> project-created exchange and user-management-project-created queue binding.

3. Validate tokens
   - Use /api/auth/login to get Admin token. Decode token and confirm claims: sub, role (or ClaimTypes.Role), projectId.
   - Call /api/debug/claims on document service and /api/user-projects on user service to see claims and UserProjects rows.

4. If user creation returns 403
   - Inspect logs for CreateUser diagnostic entry (claims printed).
   - Ensure admin token includes a role claim with value "Admin" or that the user exists in user DB and has Admin role.
   - If token sub does not match user Id in DB, IsAdmin fallback may fail; confirm sub claim equals ApplicationUser.Id.

5. If CreateUser returns 400
   - Check UserService logs for IdentityResult errors; password policy or duplicate email are common causes.

---

## End-to-end test plan (simple)

1. Start AppHost and wait for all resources to show running.
2. Get Admin token: POST /api/auth/login with seeded admin (admin@docvault.com / Admin@DocVault#2024).
3. Create project: POST document service /api/projects with Admin token (note projectId in response).
4. Wait a few seconds and confirm consumer processed event: GET user-service /api/user-projects.
5. Create user: POST /api/users (Admin token) with projectId.
6. Confirm user exists: GET /api/users and that the new user's ProjectId matches.

---

## Scope & Next Steps (for a follow-up AI task)

- Confirm and guarantee ordering: make document service hard-wait for user-service consumer registration (instead of DB+TCP readiness) if message loss remains an issue.
- Implement integration tests using Testcontainers for RabbitMQ and SQL to automate the end-to-end verification.
- Improve logging: add structured logs for event publish and consumer processing (payload and result).
- Harden security: rotate JWT Secret and address OpenTelemetry/OpenApi package advisories.
- Add a permissions matrix and UI acceptance tests to verify role-based flows.

---

## How to hand to another AI / engineer
Copy this file entirely into the prompt for the next AI session. Include the following artifacts when requesting a fix:
- Exact AppHost UI ports (AMQP tcp://localhost:<port>)
- The decoded Admin JWT payload (header + payload JSON)
- A short reproduction log: admin login, project create response, user create response and service logs around these events

With the above, an AI can reproduce the problem locally and apply a minimal, safe fix.

---

Document generated: PROJECT_DOCUMENTATION.md
