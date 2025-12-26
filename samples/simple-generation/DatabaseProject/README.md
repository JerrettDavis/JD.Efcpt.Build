# Traditional SQL Project using Microsoft.Build.Sql

This is a traditional SQL Server Database Project that uses **Microsoft.Build.Sql**.

**Important:** This project uses the `.sqlproj` file extension, which is the traditional format.

## Project Format

- **Format:** Traditional MSBuild (not SDK-style)
- **File Extension:** `.sqlproj`
- **Requirements:** SQL Server Data Tools or MSBuild with database build components

This is different from **MSBuild.Sdk.SqlProj** projects which use `.csproj` or `.fsproj` extensions despite having "SqlProj" in the SDK name.

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
