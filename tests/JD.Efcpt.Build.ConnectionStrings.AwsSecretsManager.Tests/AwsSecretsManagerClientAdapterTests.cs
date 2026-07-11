using System.Text;
using Amazon.Runtime;
using Amazon.SecretsManager.Model;
using JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager;
using Xunit;

namespace JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager.Tests;

/// <summary>
/// Unit tests for <see cref="AwsSecretsManagerClientAdapter.ExtractSecretString"/> - the response
/// interpretation layer - driven entirely by mocked <see cref="BatchGetSecretValueResponse"/>
/// objects, with no real AWS client. These lock in the CRITICAL error-code discrimination fix:
/// only a genuine <c>ResourceNotFoundException</c> error code maps to "not found"; every other
/// batch error (auth, decryption, throttling) must NOT be reported as a missing secret.
/// </summary>
public sealed class AwsSecretsManagerClientAdapterTests
{
    private const string SecretId = "my-app/connection-string";

    private static BatchGetSecretValueResponse ResponseWithError(string errorCode, string message = "boom") =>
        new()
        {
            Errors = [new APIErrorType { SecretId = SecretId, ErrorCode = errorCode, Message = message }],
            SecretValues = []
        };

    private static BatchGetSecretValueResponse ResponseWithSecretString(string value) =>
        new()
        {
            Errors = [],
            SecretValues = [new SecretValueEntry { Name = SecretId, SecretString = value }]
        };

    [Fact]
    public void AccessDenied_error_does_not_throw_ResourceNotFoundException()
    {
        var response = ResponseWithError("AccessDeniedException", "User is not authorized");

        var ex = Record.Exception(() => AwsSecretsManagerClientAdapter.ExtractSecretString(SecretId, response));

        Assert.NotNull(ex);
        // Must NOT be ResourceNotFoundException (which would map to NotFound/JD0031)...
        Assert.IsNotType<ResourceNotFoundException>(ex);
        // ...and must be an AmazonServiceException so the source maps it to Failed/JD0030.
        Assert.IsType<AmazonServiceException>(ex);
        Assert.Contains("AccessDeniedException", ex!.Message);
    }

    [Theory]
    [InlineData("DecryptionFailure")]
    [InlineData("ThrottlingException")]
    [InlineData("InvalidParameterException")]
    [InlineData("InternalServiceError")]
    public void Non_not_found_error_codes_throw_generic_AmazonServiceException(string errorCode)
    {
        var response = ResponseWithError(errorCode);

        var ex = Record.Exception(() => AwsSecretsManagerClientAdapter.ExtractSecretString(SecretId, response));

        Assert.IsType<AmazonServiceException>(ex);
        Assert.IsNotType<ResourceNotFoundException>(ex);
    }

    [Fact]
    public void ResourceNotFound_error_code_throws_ResourceNotFoundException()
    {
        var response = ResponseWithError("ResourceNotFoundException", "Secrets Manager can't find the specified secret.");

        Assert.Throws<ResourceNotFoundException>(
            () => AwsSecretsManagerClientAdapter.ExtractSecretString(SecretId, response));
    }

    [Fact]
    public void Happy_path_returns_secret_string()
    {
        var response = ResponseWithSecretString("Server=aws-host;Database=AwsDb;");

        var value = AwsSecretsManagerClientAdapter.ExtractSecretString(SecretId, response);

        Assert.Equal("Server=aws-host;Database=AwsDb;", value);
    }

    [Fact]
    public void Missing_entry_throws_ResourceNotFoundException()
    {
        var response = new BatchGetSecretValueResponse { Errors = [], SecretValues = [] };

        Assert.Throws<ResourceNotFoundException>(
            () => AwsSecretsManagerClientAdapter.ExtractSecretString(SecretId, response));
    }

    [Fact]
    public void Binary_only_secret_throws_NotSupportedException()
    {
        var response = new BatchGetSecretValueResponse
        {
            Errors = [],
            SecretValues =
            [
                new SecretValueEntry
                {
                    Name = SecretId,
                    SecretString = null,
                    SecretBinary = new MemoryStream(Encoding.UTF8.GetBytes("binary-bytes"))
                }
            ]
        };

        var ex = Assert.Throws<NotSupportedException>(
            () => AwsSecretsManagerClientAdapter.ExtractSecretString(SecretId, response));
        Assert.Contains("binary", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
