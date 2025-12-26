# MSBuild.Sdk.SqlProj SQL Project

This SQL Project uses the **MSBuild.Sdk.SqlProj** community SDK.

**Important:** Despite the SDK name containing "SqlProj", this project file uses a **`.csproj`** extension, not `.sqlproj`.

## Project Format

- **SDK:** MSBuild.Sdk.SqlProj (community-maintained NuGet package)
- **File Extension:** `.csproj`
- **Benefits:** Cross-platform, no Visual Studio required, additional configurability

This is different from:
- **Microsoft.Build.Sql** - Microsoft's official SDK using `.sqlproj` extension
- **Legacy .sqlproj** - Traditional .NET Framework format requiring SSDT

## Build

To build the project, run the following command:

```bash
dotnet build
```

🎉 Congrats! You have successfully built the project and now have a `dacpac` to deploy anywhere.

## Publish

To publish the project, the SqlPackage CLI or the SQL Database Projects extension for Azure Data Studio/VS Code is required. The following command will publish the project to a local SQL Server instance:

```bash
./SqlPackage /Action:Publish /SourceFile:bin/Debug/DatabaseProject.dacpac /TargetServerName:localhost /TargetDatabaseName:DatabaseProject
```

Learn more about authentication and other options for SqlPackage here: https://aka.ms/sqlpackage-ref

### Install SqlPackage CLI

If you would like to use the command-line utility SqlPackage.exe for deploying the `dacpac`, you can obtain it as a dotnet tool.  The tool is available for Windows, macOS, and Linux.

```bash
dotnet tool install -g microsoft.sqlpackage
```
