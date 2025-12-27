var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server container with a fixed port for build-time code generation
var sqlServer = builder.AddSqlServer("sql", port: 11434)
    .WithLifetime(ContainerLifetime.Persistent);

// Add the MyAppDb database (will be created automatically)
sqlServer.AddDatabase("MyAppDb");

builder.Build().Run();
