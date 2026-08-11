var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ with fixed credentials
var rabbitmq = builder.AddRabbitMQ("rabbitmq",
    userName: builder.AddParameter("rabbitmq-user", "guest"),
    password: builder.AddParameter("rabbitmq-pass", "guest"))
    .WithDataVolume()
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// SQL Server 

var sqlPassword = builder.AddParameter("sql-password", "MyStrongPassword123!");


var sqlServer = builder.AddSqlServer("sqlserver", sqlPassword)
    
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Databases
var userDb = sqlServer.AddDatabase("DocVaultUserDB");
var docDb = sqlServer.AddDatabase("DocVaultDB");

// Azurite
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataVolume();
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var blobs = storage.AddBlobs("blobs");


// User Management Service 
var userService = builder
    .AddProject<Projects.DocVault_UserManagement_API>("user-management")
    .WithReference(rabbitmq)
    .WithReference(userDb)
    .WaitFor(rabbitmq)
    .WaitFor(userDb);

// Document Knowledge Management Service
builder
    .AddProject<Projects.DocVault_DocumentKnowledgeManagement_API>("document-knowledge")
    .WithReference(rabbitmq)
    .WithReference(docDb)
    .WithReference(blobs)
    .WithReference(userService)
    .WaitFor(rabbitmq)
    .WaitFor(docDb)
    .WaitFor(blobs)
    .WaitFor(userService);

builder.Build().Run();

