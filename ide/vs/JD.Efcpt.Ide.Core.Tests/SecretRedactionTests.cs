using JD.Efcpt.Ide.Core;
using Xunit;

namespace JD.Efcpt.Ide.Core.Tests;

/// <summary>
/// Exercises <see cref="SecretRedaction"/>, mirroring
/// <c>ide/vscode/src/test/unit/redact.test.ts</c> and the server-side
/// <c>JD.Efcpt.Build.Core.Tests</c> secret-redaction coverage, since this is a deliberate local
/// copy of that logic (see the remarks on <see cref="SecretRedaction"/>).
/// </summary>
public sealed class SecretRedactionTests
{
    [Fact]
    public void MaskSecrets_masks_a_password_value_leaving_other_keys_visible()
    {
        var result = SecretRedaction.MaskSecrets("Server=.;Database=App;Password=secret;");

        Assert.DoesNotContain("secret", result);
        Assert.Contains("Password=***", result);
        Assert.Contains("Server=.", result);
        Assert.Contains("Database=App", result);
    }

    [Fact]
    public void MaskSecrets_masks_a_bare_password_line()
    {
        Assert.Equal("Password=***", SecretRedaction.MaskSecrets("Password=secret"));
    }

    [Theory]
    [InlineData("pwd=hunter2;", "pwd=***;")]
    [InlineData("User Id=admin;", "User Id=***;")]
    [InlineData("UID=admin;", "UID=***;")]
    [InlineData("PASSWORD=Secret;", "PASSWORD=***;")]
    public void MaskSecrets_masks_key_variants_case_insensitively(string input, string expected)
    {
        Assert.Equal(expected, SecretRedaction.MaskSecrets(input));
    }

    [Fact]
    public void MaskSecrets_masks_a_quoted_value_containing_a_semicolon()
    {
        // A naive value match that excludes quotes would match zero chars here and leave the
        // secret intact after a fake mask.
        Assert.Equal("Password=***;", SecretRedaction.MaskSecrets("Password=\"p@ss;word\";"));
        Assert.Equal("Pwd=***;", SecretRedaction.MaskSecrets("Pwd='x;y';"));

        var mixed = SecretRedaction.MaskSecrets("Server=db;Password=\"a;b\";Database=App;");
        Assert.DoesNotContain("\"a;b\"", mixed);
        Assert.Contains("Server=db", mixed);
        Assert.Contains("Database=App", mixed);
    }

    [Fact]
    public void MaskSecrets_masks_cloud_provider_credential_keys()
    {
        Assert.DoesNotContain("abc123", SecretRedaction.MaskSecrets("AccessToken=abc123;"));
        Assert.DoesNotContain("Zm9vYmFy", SecretRedaction.MaskSecrets("AccountKey=Zm9vYmFy;"));
        Assert.DoesNotContain("deadbeef", SecretRedaction.MaskSecrets("SharedAccessSignature=sv=2020&sig=deadbeef;"));
        Assert.DoesNotContain("tok999", SecretRedaction.MaskSecrets("SAS Token=tok999;"));
    }

    [Fact]
    public void MaskSecrets_leaves_output_with_no_secrets_unchanged()
    {
        const string line = "Build succeeded.\n  2 model(s) generated\n";
        Assert.Equal(line, SecretRedaction.MaskSecrets(line));
    }

    [Fact]
    public void MaskSecrets_returns_empty_string_for_null_or_empty_input()
    {
        Assert.Equal(string.Empty, SecretRedaction.MaskSecrets(null));
        Assert.Equal(string.Empty, SecretRedaction.MaskSecrets(string.Empty));
    }

    [Fact]
    public void RedactConnectionString_replaces_the_known_connection_string_and_masks_residual_secrets()
    {
        const string connectionString = "Server=db;Database=App;User Id=sa;Password=SUPERSECRET;";
        var text = $"Connecting with {connectionString} now";

        var result = SecretRedaction.RedactConnectionString(text, connectionString);

        Assert.DoesNotContain("SUPERSECRET", result);
        Assert.Contains(SecretRedaction.ConnectionStringPlaceholder, result);
    }

    [Fact]
    public void RedactConnectionString_masks_password_even_without_a_known_connection_string()
    {
        var result = SecretRedaction.RedactConnectionString("Password=SUPERSECRET;", connectionString: null);

        Assert.DoesNotContain("SUPERSECRET", result);
        Assert.Contains("Password=***", result);
    }
}
