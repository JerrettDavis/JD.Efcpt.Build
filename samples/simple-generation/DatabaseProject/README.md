# SQL Project (.sqlproj format)

This SQL Project uses the `.sqlproj` file extension.

**Important:** This project uses the `.sqlproj` extension, which can be built with either Microsoft.Build.Sql (modern) or the legacy .NET Framework format.

## Project Format

- **File Extension:** `.sqlproj`
- **Supported SDKs:** 
  - Microsoft.Build.Sql (modern, .NET SDK-based)
  - Legacy .NET Framework format (requires SSDT)

This is different from **MSBuild.Sdk.SqlProj** which uses `.csproj` or `.fsproj` extensions despite having "SqlProj" in the SDK name.

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
