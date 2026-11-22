using System.Diagnostics;
using System.Text.Json;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>
/// Tests of <c>build/Hostpanel.Sdk.targets</c>, the file the package ships so that building a plugin writes the version of the
/// referenced <c>Hostpanel.Sdk</c> into its <c>plugin.json</c>. Each test builds a small project that imports the file and
/// references the SDK assembly the tests run against, which is what a plugin project gets from the package.
/// </summary>
public sealed class SdkPackageVersionTargetsTests
{
    /// <summary>The SDK version as the package carries it: the assembly version without its fourth part.</summary>
    private static readonly string ExpectedVersion = typeof(Plugins.Manifest.PluginManifest).Assembly.GetName().Version!.ToString(3);

    /// <summary>A manifest without the member gets it, and the members it had are kept.</summary>
    [Fact]
    public void Build_ManifestWithoutTheMember_AddsTheReferencedSdkVersion()
    {
        var manifest = Build("{\n  \"id\": \"demo\",\n  \"version\": \"1.0.0\"\n}\n", stamp: true);

        var root = JsonDocument.Parse(manifest).RootElement;
        root.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        root.GetProperty("id").GetString().Should().Be("demo");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    /// <summary>A stale value is replaced, so a plugin rebuilt on a newer SDK never keeps the old number.</summary>
    [Fact]
    public void Build_ManifestWithAStaleValue_ReplacesIt()
    {
        var manifest = Build("{\n  \"id\": \"demo\",\n  \"sdkPackageVersion\": \"2.0.0\"\n}\n", stamp: true);

        var root = JsonDocument.Parse(manifest).RootElement;
        root.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        manifest.Split("sdkPackageVersion").Should().HaveCount(2, "the member is written once");
    }

    /// <summary>An up to date manifest is left byte for byte as it was.</summary>
    [Fact]
    public void Build_ManifestAlreadyCurrent_IsNotRewritten()
    {
        var current = "{\n  \"sdkPackageVersion\": \"" + ExpectedVersion + "\",\n  \"id\": \"demo\"\n}\n";

        Build(current, stamp: true).Should().Be(current);
    }

    /// <summary>Switching the property off leaves the manifest alone.</summary>
    [Fact]
    public void Build_StampSwitchedOff_LeavesTheManifestAlone()
    {
        var original = "{\n  \"id\": \"demo\"\n}\n";

        Build(original, stamp: false).Should().Be(original);
    }

    /// <summary>Builds a project that imports the targets file and references the SDK assembly, then reads its manifest back.</summary>
    /// <param name="manifest">The text of the project's <c>plugin.json</c> before the build.</param>
    /// <param name="stamp">Whether the stamping is left on, or switched off with <c>HostpanelStampSdkPackageVersion</c>.</param>
    /// <returns>The text of the manifest after the build.</returns>
    private static string Build(string manifest, bool stamp)
    {
        var folder = Path.Combine(Path.GetTempPath(), "hostpanel-sdk-targets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var sdkAssembly = Path.Combine(AppContext.BaseDirectory, "Hostpanel.Sdk.dll");
            var targets = Path.Combine(AppContext.BaseDirectory, "build", "Hostpanel.Sdk.targets");
            File.WriteAllText(Path.Combine(folder, "plugin.json"), manifest);
            File.WriteAllText(
                Path.Combine(folder, "Fixture.csproj"),
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net{Environment.Version.Major}.0</TargetFramework>
                    <HostpanelStampSdkPackageVersion>{(stamp ? "true" : "false")}</HostpanelStampSdkPackageVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <Reference Include="Hostpanel.Sdk"><HintPath>{sdkAssembly}</HintPath></Reference>
                  </ItemGroup>
                  <Import Project="{targets}" />
                </Project>
                """);
            File.WriteAllText(Path.Combine(folder, "Marker.cs"), "namespace Fixture; internal sealed class Marker { }");

            RunBuild(folder);
            return File.ReadAllText(Path.Combine(folder, "plugin.json"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>Runs <c>dotnet build</c> in a folder and fails the test with the build output when it does not succeed.</summary>
    /// <param name="folder">The folder holding the project.</param>
    private static void RunBuild(string folder)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("build");
        start.ArgumentList.Add("--nologo");
        start.ArgumentList.Add("-p:NuGetAudit=false");

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        process.ExitCode.Should().Be(0, output.Result + errors.Result);
    }
}
