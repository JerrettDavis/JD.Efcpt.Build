using Xunit;

namespace JD.Efcpt.Build.Tests.Integration;

public sealed class ContainerStartupTests
{
    [Fact]
    public void Detects_transient_docker_registry_error_from_ci()
    {
        // The exact transient failure observed on CI when pulling mysql:8.0 from Docker Hub.
        var ex = new Exception(
            "Docker API responded with status code='InternalServerError', " +
            "response='{\"message\":\"Head \\\"https://registry-1.docker.io/v2/library/mysql/manifests/8.0\\\": unknown: \"}'");

        Assert.True(ContainerStartup.IsDockerInfrastructureFailure(ex));
    }

    [Fact]
    public void Detects_docker_daemon_unavailable()
    {
        var ex = new Exception("Cannot connect to the Docker daemon at unix:///var/run/docker.sock.");
        Assert.True(ContainerStartup.IsDockerInfrastructureFailure(ex));
    }

    [Fact]
    public void Detects_registry_rate_limit()
    {
        var ex = new InvalidOperationException(
            "wrapper", new Exception("toomanyrequests: You have reached your pull rate limit."));
        Assert.True(ContainerStartup.IsDockerInfrastructureFailure(ex));
    }

    [Fact]
    public void Does_not_treat_product_assertion_failure_as_infrastructure()
    {
        var ex = new Exception("Expected 3 tables but found 2.");
        Assert.False(ContainerStartup.IsDockerInfrastructureFailure(ex));
    }

    [Fact]
    public void Handles_null()
    {
        Assert.False(ContainerStartup.IsDockerInfrastructureFailure(null));
    }
}
