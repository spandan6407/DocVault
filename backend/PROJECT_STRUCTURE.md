# Backend Project Structure

This file lists the services under `backend/` and shows the folders and files found in each service. Each file is followed by a single-line description of its purpose.

--

## document-knowledge-management (DocVault.DocumentKnowledgeManagement)

Root: backend/document-knowledge-management/

- DocVault.DocumentKnowledgeManagement.API/
  - Program.cs — Application startup: DI, DbContext, Identity, authentication and middleware configuration.
  - appsettings.json — Configuration (ConnectionStrings, JwtSettings, etc.).
  - appsettings.Development.json — Development overrides for configuration.
  - Properties/launchSettings.json — Launch profiles used by Visual Studio.
  - DocVault.DocumentKnowledgeManagement.API.csproj — API project file.
  - DocVault.DocumentKnowledgeManagement.API.http — HTTP requests collection for the API.
  - Controllers/ProjectsController.cs — Controller that exposes project-related endpoints (create, read, update).
  - Controllers/DocumentsController.cs — Controller that exposes document upload/listing endpoints.
  - Controllers/AuthController.cs — Authentication endpoints used by this API.

- DocVault.DocumentKnowledgeManagement.Application/
  - DocVault.DocumentKnowledgeManagement.Application.csproj — Application project file.
  - Class1.cs — placeholder/auto-generated file.
  - DTOs/
    - Auth/LoginRequestDto.cs — DTO for login requests.
    - Auth/LoginResponseDto.cs — DTO for login responses.
    - Auth/CurrentUserDto.cs — DTO for current authenticated user info.
    - Projects/CreateProjectDto.cs — DTO for creating a project.
    - Projects/UpdateProjectDto.cs — DTO for updating a project.
    - Projects/ProjectResponseDto.cs — DTO used to return project details.
    - Documents/UploadDocumentDto.cs — DTO for uploading documents.
    - Documents/DocumentResponseDto.cs — DTO for returning document info.
  - Interfaces/
    - IAuthService.cs — Interface for authentication related operations.
    - IDocumentService.cs — Interface for document-related business logic.
    - IProjectService.cs — Interface for project-related business logic.
  - Events/
    - Published/ProjectCreatedEvent.cs — Domain/application event published when a project is created.

- DocVault.DocumentKnowledgeManagement.Domain/
  - DocVault.DocumentKnowledgeManagement.Domain.csproj — Domain project file.
  - Class1.cs — placeholder/auto-generated file.
  - Entities/Project.cs — Domain entity representing a project.
  - Entities/Document.cs — Domain entity representing a document.

- DocVault.DocumentKnowledgeManagement.Infrastructure/
  - DocVault.DocumentKnowledgeManagement.Infrastructure.csproj — Infrastructure project file.
  - Class1.cs — placeholder/auto-generated file.
  - Identity/ApplicationDbContext.cs — EF Core DbContext for the application (Identity + app tables).
  - Identity/ApplicationUser.cs — Identity user model used by this service.
  - Migrations/* — EF Core migration files and model snapshot.
  - Seeders/DataSeeder.cs — Bootstraps initial data (roles, users, sample data).
  - Services/ProjectService.cs — Implementation of IProjectService.
  - Services/DocumentService.cs — Implementation of IDocumentService.
  - Services/AuthService.cs — Implementation of IAuthService used by this API.

--

## user-management-service (DocVault.UserManagement)

Root: backend/user-management-service/

- DocVault.UserManagement.API/
  - Program.cs — Application startup: DI, DbContext, Identity, authentication and middleware configuration for user service.
  - appsettings.json — Configuration (ConnectionStrings, JwtSettings, etc.).
  - appsettings.Development.json — Development overrides for configuration.
  - Properties/launchSettings.json — Launch profiles used by Visual Studio.
  - DocVault.UserManagement.API.csproj — API project file.
  - DocVault.UserManagement.API.http — HTTP requests collection for the API.
  - Controllers/AuthController.cs — Authentication endpoints (register, login, token issuance).
  - Controllers/UsersController.cs — User management endpoints (assign roles, list users, etc.).

- DocVault.UserManagement.Application/
  - DocVault.UserManagement.Application.csproj — Application project file.
  - DTOs/
    - Auth/LoginRequestDto.cs — DTO for login requests.
    - Auth/LoginResponseDto.cs — DTO for login responses.
    - Auth/CurrentUserDto.cs — DTO for current authenticated user info.
    - Users/CreateUserDto.cs — DTO for creating a user.
    - Users/AssignProjectHeadDto.cs — DTO used when assigning a project head to a project.
    - Users/UserResponseDto.cs — DTO returned when querying user info.
  - Interfaces/
    - IAuthService.cs — Interface for authentication-related operations.
    - IUserService.cs — Interface for user-management business logic.
  - Events/
    - Consumed/ProjectCreatedEvent.cs — Event consumed from other services when a project is created.
    - Published/ProjectHeadAssignedEvent.cs — Event published when a project head is assigned.
    - Published/UserCreatedEvent.cs — Event published after a user is created.
    - Published/UserRoleAssignedEvent.cs — Event published when a role is assigned to a user.

- DocVault.UserManagement.Domain/
  - DocVault.UserManagement.Domain.csproj — Domain project file.
  - Entities/UserProject.cs — Domain entity linking users and projects.

- DocVault.UserManagement.Infrastructure/
  - DocVault.UserManagement.Infrastructure.csproj — Infrastructure project file.
  - Identity/UserManagementDbContext.cs — EF Core DbContext for user management service.
  - Identity/ApplicationUser.cs — Identity user model used by the user service.
  - Migrations/* — EF Core migrations and model snapshot for user DB.
  - Seeders/DataSeeder.cs — Bootstraps roles and sample users.
  - Services/UserService.cs — Implementation of IUserService.
  - Services/AuthService.cs — Implementation of IAuthService used by this service.
  - Messaging/Consumers/ProjectCreatedConsumer.cs — Message consumer reacting to ProjectCreated events from other services.

--

## Notes
- This file lists files and folders discovered in the workspace at the time of generation. Some placeholder files (Class1.cs) are generated by templates and may be unused.
- For files not present or additional folders you expect, run a workspace file listing or check the repository to include them.
