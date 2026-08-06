using System.Xml.Linq;

namespace AegiDocs.Application.Tests.Architecture;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    public static TheoryData<string, string[]> ProductionProjects =>
        new()
        {
            { "src/AegiDocs.Domain/AegiDocs.Domain.csproj", [] },
            { "src/AegiDocs.Application/AegiDocs.Application.csproj", ["AegiDocs.Domain.csproj"] },
            { "src/AegiDocs.Infrastructure.Storage/AegiDocs.Infrastructure.Storage.csproj", ["AegiDocs.Application.csproj", "AegiDocs.Domain.csproj"] },
            { "src/AegiDocs.Infrastructure.Windows/AegiDocs.Infrastructure.Windows.csproj", ["AegiDocs.Application.csproj", "AegiDocs.Domain.csproj"] },
            { "src/AegiDocs.Export/AegiDocs.Export.csproj", ["AegiDocs.Application.csproj", "AegiDocs.Domain.csproj"] },
            { "src/AegiDocs.Recorder/AegiDocs.Recorder.csproj", ["AegiDocs.Application.csproj", "AegiDocs.Export.csproj", "AegiDocs.Infrastructure.Storage.csproj", "AegiDocs.Infrastructure.Windows.csproj"] },
        };

    public static TheoryData<string, string> TestProjects =>
        new()
        {
            { "tests/AegiDocs.Domain.Tests/AegiDocs.Domain.Tests.csproj", "AegiDocs.Domain.csproj" },
            { "tests/AegiDocs.Application.Tests/AegiDocs.Application.Tests.csproj", "AegiDocs.Application.csproj" },
            { "tests/AegiDocs.Infrastructure.Storage.Tests/AegiDocs.Infrastructure.Storage.Tests.csproj", "AegiDocs.Infrastructure.Storage.csproj" },
            { "tests/AegiDocs.Infrastructure.Windows.IntegrationTests/AegiDocs.Infrastructure.Windows.IntegrationTests.csproj", "AegiDocs.Infrastructure.Windows.csproj" },
            { "tests/AegiDocs.Export.Tests/AegiDocs.Export.Tests.csproj", "AegiDocs.Export.csproj" },
        };

    [Theory]
    [MemberData(nameof(ProductionProjects))]
    public void ProductionProjectReferencesMatchTheArchitectureDecision(string relativeProjectPath, string[] expectedReferences)
    {
        var projectPath = GetRepositoryPath(relativeProjectPath);
        var actualReferences = GetProjectReferenceFileNames(projectPath);

        Assert.True(
            expectedReferences.OrderBy(reference => reference).SequenceEqual(actualReferences.OrderBy(reference => reference)),
            $"{relativeProjectPath} must reference exactly [{string.Join(", ", expectedReferences)}] according to ADR-0001, " +
            $"but references [{string.Join(", ", actualReferences)}].");
    }

    [Theory]
    [MemberData(nameof(TestProjects))]
    public void TestProjectReferencesOnlyItsAssignedProductionProject(string relativeProjectPath, string assignedProductionProject)
    {
        var projectPath = GetRepositoryPath(relativeProjectPath);
        var actualReferences = GetProjectReferenceFileNames(projectPath);

        Assert.True(
            actualReferences.SequenceEqual([assignedProductionProject]),
            $"{relativeProjectPath} must reference only {assignedProductionProject}, but references [{string.Join(", ", actualReferences)}].");
    }

    [Fact]
    public void DomainProjectHasNoProjectOrPackageReferencesAndNoWindowsOrWpfConfiguration()
    {
        const string relativeProjectPath = "src/AegiDocs.Domain/AegiDocs.Domain.csproj";
        var projectDocument = LoadProject(GetRepositoryPath(relativeProjectPath));
        var projectElements = projectDocument.Descendants().ToList();

        Assert.DoesNotContain(projectElements, element => element.Name.LocalName == "ProjectReference");
        Assert.DoesNotContain(projectElements, element => element.Name.LocalName == "PackageReference");
        Assert.DoesNotContain(
            projectElements,
            element => element.Name.LocalName is "UseWPF" or "UseWindowsForms" ||
                       element.Value.Contains("windows", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplicationProjectDoesNotReferenceInfrastructureOrRecorder()
    {
        const string relativeProjectPath = "src/AegiDocs.Application/AegiDocs.Application.csproj";
        var actualReferences = GetProjectReferenceFileNames(GetRepositoryPath(relativeProjectPath));

        Assert.DoesNotContain(actualReferences, reference => reference.StartsWith("AegiDocs.Infrastructure.", StringComparison.Ordinal));
        Assert.DoesNotContain(actualReferences, reference => string.Equals(reference, "AegiDocs.Recorder.csproj", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var solutionPath = Path.Combine(directory.FullName, "AegiDocs.slnx");
            var sourceDirectory = Path.Combine(directory.FullName, "src");

            if (File.Exists(solutionPath) && Directory.Exists(sourceDirectory))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate the AegiDocs repository root from AppContext.BaseDirectory '{AppContext.BaseDirectory}'. " +
            "Expected a parent directory containing AegiDocs.slnx and src.");
    }

    private static string GetRepositoryPath(string relativePath) =>
        Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string[] GetProjectReferenceFileNames(string projectPath) =>
        LoadProject(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileName(include!))
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

    private static XDocument LoadProject(string projectPath)
    {
        Assert.True(File.Exists(projectPath), $"Expected project file '{projectPath}' to exist.");
        return XDocument.Load(projectPath, LoadOptions.None);
    }
}
