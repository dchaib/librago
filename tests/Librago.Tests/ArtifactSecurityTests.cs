namespace Librago.Tests;

public sealed class ArtifactSecurityTests
{
    [Fact]
    public void LocalConfigurationIsExplicitlyExcludedFromBuildAndDockerArtifacts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var project = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Librago", "Librago.csproj"));
        var dockerIgnore = File.ReadAllText(Path.Combine(repositoryRoot, ".dockerignore"));

        Assert.Contains("Content Update=\"appsettings.Local.json\"", project, StringComparison.Ordinal);
        Assert.Contains("CopyToOutputDirectory=\"Never\"", project, StringComparison.Ordinal);
        Assert.Contains("CopyToPublishDirectory=\"Never\"", project, StringComparison.Ordinal);
        Assert.Contains("**/appsettings.Local.json", dockerIgnore, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Librago.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("The repository root could not be located.");
    }
}
