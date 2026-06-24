# Project Context - DocVault

This document describes the overall project structure and important implementation details to use as a prompt or quick reference.

## Repository
- Local path: C:\DocVault\
- Primary branch: user-management-service (example)
- Target framework: .NET 10

## High-level overview
DocVault is a microservice-style solution containing multiple backend services (examples in workspace):
- backend/document-knowledge-management - Document knowledge management API
- backend/user-management-service - User management service and identity

Each service is an ASP.NET Web API using:
- ASP.NET Core (.NET 10)
- Entity Framework Core with SQL Server
- ASP.NET Core Identity for user management
- JWT Bearer authentication
- Role-based authorization (roles: Admin, ProjectHead, User)

## Typical project structure (per service)
- <ServiceRoot>/
  - Program.cs
  - appsettings.json (contains ConnectionStrings, JwtSettings)
  - Controllers/
  - DTOs/
  - Application/
    - Interfaces/
    - Services/
    - DTOs/
  - Infrastructure/
    - Identity/
    - Services/
    - Seeders/
    - Persistence (DbContext)
  - Migrations/
  - Tests/ (optional)

Example: backend/document-knowledge-management/DocVault.DocumentKnowledgeManagement.API/
- Controllers/ProjectsController.cs (routes: api/projects)
- Program.cs (registers DbContext, Identity, authentication, ProjectService, OpenAPI/Scalar)

## Important files and details observed
- ProjectsController (path shown in workspace)
  - Route: [Route("api/projects")]
  - Endpoints:
    - POST /api/projects  (Admin)
    - GET /api/projects   (Admin)
    - GET /api/projects/{id}  (Admin,ProjectHead,User)
    - PUT /api/projects/{id}  (Admin)
  - Uses IProjectService for business logic
  - Reads claims: ClaimTypes.Email, ClaimTypes.NameIdentifier, ClaimTypes.Role

- Program.cs (DocumentKnowledgeManagement API)
  - Adds ApplicationDbContext with SQL Server
  - Configures IdentityCore<ApplicationUser> with roles
  - Configures JWT authentication using JwtSettings (SecretKey, Issuer, Audience)
  - Registers IAuthService and IProjectService
  - Adds OpenAPI/Scalar documentation (MapOpenApi, MapScalarApiReference)
  - Seeds initial data via DataSeeder.SeedAsync

## Configuration keys (common)
- ConnectionStrings: DefaultConnection
- JwtSettings:
  - SecretKey
  - Issuer
  - Audience

## Common runtime steps
1. Ensure appsettings.json (or environment variables) provide DefaultConnection and JwtSettings.
2. dotnet restore
3. dotnet ef database update (from migrations project / appropriate startup project) or let seeder create data
4. Run the API (dotnet run or via Visual Studio)
5. Use an authenticated JWT with proper roles to call protected endpoints (Admin / ProjectHead / User)

## Authentication & Authorization notes
- JWT Bearer is used. Tokens must contain role claims and name/identifier/email claims.
- Controllers often check role via [Authorize(Roles = "...")] attributes and read User claims in code.
- If controllers are not visible in UI (Scalar screen), check:
  - The API is running and reachable
  - OpenAPI/Scalar configuration is enabled in Development environment
  - Roles and claims in the JWT used by the Scalar UI have the required roles (Admin, etc.)
  - Endpoints are not blocked by authorization middleware ordering (UseAuthentication before UseAuthorization)

## Why a controller might not be visible in the Scalar UI
- The controller route is protected by role-based authorization; UI may only show endpoints available to the current authenticated user.
- OpenAPI/Scalar documentation is only mapped in Development environment in Program.cs. If running in non-Development, the UI won't show API docs.
- Missing or incorrect JwtSettings / secret causing token validation to fail in the UI.
- Controller route attribute or HTTP attribute mismatch (e.g., different route prefix expected by UI).
- Service registration missing for a dependency results in runtime errors that prevent proper discovery (check logs).

## How to use this file for prompting
- Copy relevant sections when asking for help: e.g., include Program.cs snippets, controller routes, observed behavior (what UI shows), and JWT claim samples.
- Example prompt: "Projects API endpoints not visible in Scalar UI. Program.cs maps Scalar only in Development. I'm running app in Production — what should I change?" Include path references and snippets.

## Quick checklist to debug "controller not visible" problems in UI
- [ ] API running and reachable (curl /swagger url)
- [ ] Environment is Development so MapOpenApi/MapScalarApiReference is called
- [ ] Authentication token used by UI is valid and contains required role claims
- [ ] Middleware order: UseAuthentication() before UseAuthorization()
- [ ] Services registered (IProjectService) and app doesn't crash during startup
- [ ] No exceptions in DataSeeder or during DI resolution (check logs)


If you want, I can also generate a short example prompt template you can reuse when requesting help or bug reports.
