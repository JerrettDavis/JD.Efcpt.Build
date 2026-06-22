using DotNet.Testcontainers.Containers;
using Xunit;

namespace JD.Efcpt.Build.Tests.Integration;

/// <summary>
/// Helpers for starting Testcontainers-backed integration tests in a way that is
/// resilient to transient Docker/registry infrastructure failures.
/// </summary>
/// <remarks>
/// These integration tests require a working Docker daemon and the ability to pull
/// images from a registry (e.g. Docker Hub). CI runners periodically hit transient
/// registry errors such as
/// <c>Docker API responded with status code='InternalServerError', response='{"message":"Head ... manifests/8.0: unknown: "}'</c>.
/// Such failures are environmental, not product defects, so they should mark the test
/// as <b>skipped</b> (with a clear reason) rather than failing the build. Genuine
/// product failures (assertion failures, schema-reader bugs) still fail as normal.
/// </remarks>
public static class ContainerStartup
{
    /// <summary>
    /// Starts the supplied container, converting transient Docker/registry
    /// infrastructure failures into a test skip via <see cref="SkipException"/>.
    /// </summary>
    public static async Task StartOrSkipAsync(IContainer container)
    {
        try
        {
            await container.StartAsync();
        }
        catch (Exception ex) when (IsDockerInfrastructureFailure(ex))
        {
            throw new SkipException(
                "Skipping container-based integration test: Docker/registry is unavailable " +
                $"or returned a transient error ({ex.GetType().Name}: {Flatten(ex)}).");
        }
    }

    /// <summary>
    /// Determines whether an exception (or any inner exception) represents a transient
    /// Docker daemon / registry infrastructure failure rather than a product defect.
    /// </summary>
    public static bool IsDockerInfrastructureFailure(Exception? ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().Name;
            if (typeName is "DockerApiException" or "DockerContainerNotFoundException")
            {
                return true;
            }

            var message = current.Message ?? string.Empty;
            if (message.Contains("registry-1.docker.io", StringComparison.OrdinalIgnoreCase)
                || message.Contains("manifests", StringComparison.OrdinalIgnoreCase)
                || message.Contains("InternalServerError", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Docker API responded", StringComparison.OrdinalIgnoreCase)
                || message.Contains("toomanyrequests", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Docker is either not running", StringComparison.OrdinalIgnoreCase)
                || message.Contains("error pulling image", StringComparison.OrdinalIgnoreCase)
                || message.Contains("failed to resolve reference", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Flatten(Exception ex)
    {
        var inner = ex;
        while (inner.InnerException is not null)
        {
            inner = inner.InnerException;
        }

        return inner.Message;
    }
}
