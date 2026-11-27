using System.Reflection;
using Microsoft.AspNetCore.Components;
using NetArchTest.Rules;
using RotationTracker.Domain;

namespace RotationTracker.Tests;

public sealed class ArchitectureTests
{
    private static readonly Assembly App = typeof(Project).Assembly;

    private static readonly string[] SecretWords = ["Secret", "Password", "Token", "ApiKey", "PrivateKey", "ConnectionString"];

    [Fact]
    public void Domain_DoesNotDependOnEfCoreInfrastructureOrFeatures()
    {
        var result = Types.InAssembly(App)
            .That().ResideInNamespace("RotationTracker.Domain")
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "RotationTracker.Infrastructure", "RotationTracker.Features")
            .GetResult();

        AssertSuccess(result);
    }

    [Fact]
    public void Slices_DoNotDependOnEachOther()
    {
        var areas = App.GetTypes()
            .Select(t => t.Namespace)
            .OfType<string>()
            .Where(n => n.StartsWith("RotationTracker.Features.", StringComparison.Ordinal))
            .Select(n => string.Join('.', n.Split('.').Take(3)))
            .Distinct()
            .ToArray();

        foreach (var area in areas)
        {
            var others = areas.Where(a => a != area).ToArray();
            if (others.Length == 0)
            {
                continue;
            }

            var result = Types.InAssembly(App)
                .That().ResideInNamespace(area)
                .ShouldNot().HaveDependencyOnAny(others)
                .GetResult();

            AssertSuccess(result);
        }
    }

    // CLAUDE.md hard rule: metadata only, never credential values.
    [Fact]
    public void NoPropertyHoldsACredentialValue()
    {
        var offenders = App.GetTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(p => SecretWords.Any(w => p.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .Select(p => $"{p.DeclaringType?.FullName}.{p.Name}")
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Classes_AreSealedUnlessAbstract()
    {
        var result = Types.InAssembly(App)
            .That().AreClasses()
            .And().AreNotAbstract()
            .And().DoNotInherit(typeof(ComponentBase))
            .And().DoNotHaveName("_Imports")
            .And().DoNotResideInNamespace("RotationTracker.Infrastructure.Migrations")
            .Should().BeSealed()
            .GetResult();

        AssertSuccess(result);
    }

    // Analyzers and dotnet format skip @code in .razor files; logic goes in a .razor.cs code-behind.
    [Fact]
    public void RazorFiles_HaveNoCodeBlocks()
    {
        var src = Path.Combine(RepoRoot(), "src");
        var offenders = Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("@code", StringComparison.Ordinal)
                || File.ReadAllText(f).Contains("@functions", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(src, f))
            .ToArray();

        Assert.Empty(offenders);
    }

    private static void AssertSuccess(TestResult result)
    {
        Assert.True(result.IsSuccessful, "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RotationTracker.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root (RotationTracker.slnx) not found.");
    }
}
