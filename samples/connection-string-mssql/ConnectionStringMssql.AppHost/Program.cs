var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server container with a fixed port for build-time code generation
var sqlServer = builder.AddSqlServer("sql", port: 11433)
    .WithLifetime(ContainerLifetime.Persistent);

// Add the Northwind database (will be created automatically)
sqlServer.AddDatabase("Northwind");

builder.Build().Run();
