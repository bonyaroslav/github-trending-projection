using Xunit;

namespace Integration.Support;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS"), "1", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Docker integration tests are disabled. Set RUN_DOCKER_TESTS=1 to enable.";
        }
    }
}
