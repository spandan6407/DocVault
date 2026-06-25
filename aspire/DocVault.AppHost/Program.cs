var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// SQL Server
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);

// Databases
var userDb = sqlServer.AddDatabase("DocVaultUserDB");
var docDb = sqlServer.AddDatabase("DocVaultDB");

// User Management Service
var userService = builder.AddProject<Projects.DocVault_UserManagement_API>("user-management")
    .WithReference(rabbitmq)
    .WithReference(userDb)
    .WaitFor(rabbitmq)
    .WaitFor(userDb);

// Document Knowledge Management Service
builder.AddProject<Projects.DocVault_DocumentKnowledgeManagement_API>("document-knowledge")
    .WithReference(rabbitmq)
    .WithReference(docDb)
    .WithReference(userService)
    .WaitFor(rabbitmq)
    .WaitFor(docDb)
    .WaitFor(userService);

builder.Build().Run();