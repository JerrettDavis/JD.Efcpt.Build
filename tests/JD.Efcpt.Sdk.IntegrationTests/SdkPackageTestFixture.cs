using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Collection fixture that provides access to the assembly-level packed packages.
/// The actual packing happens once at assembly load via AssemblyFixture.
/// </summary>
public class SdkPackageTestFixture
{
    public string PackageOutputPath => AssemblyFixture.PackageOutputPath;
    public string SdkPackagePath => AssemblyFixture.SdkPackagePath;
    public string BuildPackagePath => AssemblyFixture.BuildPackagePath;
    public string SdkVersion => AssemblyFixture.SdkVersion;
    public string BuildVersion => AssemblyFixture.BuildVersion;

    public string GetTestFixturesPath() => AssemblyFixture.TestFixturesPath;
}

// Collection definitions for parallel test execution
// Tests in different collections run in parallel, tests within a collection run sequentially

[CollectionDefinition("SDK Net8.0 Tests")]
public class SdkNet80TestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("SDK Net9.0 Tests")]
public class SdkNet90TestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("SDK Net10.0 Tests")]
public class SdkNet100TestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("Build Package Tests")]
public class BuildPackageTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("Package Content Tests")]
public class PackageContentTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("Code Generation Tests")]
public class CodeGenerationTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

// Legacy collection for backwards compatibility
[CollectionDefinition("SDK Package Tests")]
public class SdkPackageTestCollection : ICollectionFixture<SdkPackageTestFixture> { }
