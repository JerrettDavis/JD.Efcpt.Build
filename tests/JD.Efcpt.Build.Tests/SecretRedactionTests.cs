using JD.Efcpt.Build.Core.Diagnostics;
using Xunit;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for <see cref="SecretRedaction"/>, which masks credentials before they reach build logs.
/// </summary>
public sealed class SecretRedactionTests
{
    [Fact]
    public void MaskSecrets_masks_password_key_value_pair()
    {
        var result = SecretRedaction.MaskSecrets("Server=.;Database=App;Password=secret;");

        Assert.DoesNotContain("secret", result);
        Assert.Contains("Password=***", result);
        // Non-sensitive keys stay visible.
        Assert.Contains("Server=.", result);
        Assert.Contains("Database=App", result);
    }

    [Theory]
    [InlineData("Password=hunter2;", "hunter2")]
    [InlineData("pwd=hunter2;", "hunter2")]
    [InlineData("User ID=admin;", "admin")]
    [InlineData("Uid=admin;", "admin")]
    public void MaskSecrets_masks_all_sensitive_keys(string input, string secret)
    {
        var result = SecretRedaction.MaskSecrets(input);

        Assert.DoesNotContain(secret, result);
        Assert.Contains("***", result);
    }

    [Fact]
    public void MaskSecrets_is_case_insensitive()
    {
        var result = SecretRedaction.MaskSecrets("PASSWORD=Secret;");

        Assert.DoesNotContain("Secret", result);
        Assert.Contains("***", result);
    }

    [Theory]
    [InlineData("Password=\"p@ss;word\";", "p@ss;word")]
    [InlineData("Pwd='x;y';", "x;y")]
    [InlineData("Password=\"a;b\";Server=db;", "a;b")]
    public void MaskSecrets_masks_quoted_values_containing_semicolons(string input, string secret)
    {
        // Regression: a naive [^;"]* value match matches ZERO chars against a quoted value,
        // leaving the secret intact after a fake mask. The value match must be quote-aware.
        var result = SecretRedaction.MaskSecrets(input);

        Assert.DoesNotContain(secret, result);
        Assert.Contains("***", result);
    }

    [Fact]
    public void MaskSecrets_keeps_non_secret_keys_visible_when_masking_quoted_value()
    {
        var result = SecretRedaction.MaskSecrets("Server=db;Password=\"a;b\";Database=App;");

        Assert.DoesNotContain("\"a;b\"", result);
        Assert.Contains("Server=db", result);
        Assert.Contains("Database=App", result);
    }

    [Theory]
    [InlineData("AccessToken=abc123;", "abc123")]
    [InlineData("access token=abc123;", "abc123")]
    [InlineData("AccountKey=Zm9vYmFy;", "Zm9vYmFy")]
    [InlineData("SharedAccessSignature=sv=2020&sig=deadbeef;", "deadbeef")]
    [InlineData("shared access signature=sig123;", "sig123")]
    [InlineData("SAS Token=tok999;", "tok999")]
    public void MaskSecrets_masks_cloud_provider_credential_keys(string input, string secret)
    {
        var result = SecretRedaction.MaskSecrets(input);

        Assert.DoesNotContain(secret, result);
        Assert.Contains("***", result);
    }

    [Fact]
    public void MaskSecrets_returns_empty_for_null_or_empty()
    {
        Assert.Equal(string.Empty, SecretRedaction.MaskSecrets(null));
        Assert.Equal(string.Empty, SecretRedaction.MaskSecrets(""));
    }

    [Fact]
    public void RedactConnectionString_replaces_the_whole_known_value_with_placeholder()
    {
        var cs = "Server=db;Database=App;User Id=sa;Password=SuperSecret123;";
        var commandLine = $"efcpt \"{cs}\" mssql -i config.json";

        var result = SecretRedaction.RedactConnectionString(commandLine, cs);

        Assert.DoesNotContain("SuperSecret123", result);
        Assert.DoesNotContain(cs, result);
        Assert.Contains(SecretRedaction.ConnectionStringPlaceholder, result);
        // The non-secret parts of the command line remain visible.
        Assert.Contains("efcpt", result);
        Assert.Contains("mssql -i config.json", result);
    }

    [Fact]
    public void RedactConnectionString_still_masks_password_when_value_is_unknown()
    {
        // Even without the exact connection string, residual key-value secrets are masked.
        var commandLine = "efcpt \"Server=db;Password=leaky;\" mssql";

        var result = SecretRedaction.RedactConnectionString(commandLine, connectionString: null);

        Assert.DoesNotContain("leaky", result);
        Assert.Contains("Password=***", result);
    }

    [Fact]
    public void RedactConnectionString_leaves_dacpac_paths_untouched()
    {
        var commandLine = "efcpt \"C:/proj/db.dacpac\" mssql -i config.json";

        var result = SecretRedaction.RedactConnectionString(commandLine, connectionString: null);

        Assert.Equal(commandLine, result);
    }
}
