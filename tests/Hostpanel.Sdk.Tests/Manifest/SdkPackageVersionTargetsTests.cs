using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>
/// Tests of <c>build/Hostpanel.Sdk.targets</c>, the file the package ships so that building a plugin writes the version of the
/// referenced <c>Hostpanel.Sdk</c> into its <c>plugin.json</c>. Each test builds a small project that imports the file and
/// references the SDK assembly the tests run against, which is what a plugin project gets from the package.
/// </summary>
public sealed class SdkPackageVersionTargetsTests
{
    /// <summary>How long a nested build may run before the test gives up and ends it.</summary>
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(3);

    /// <summary>The SDK version as the package carries it: the assembly version without its fourth part.</summary>
    private static readonly string ExpectedVersion = typeof(Plugins.Manifest.PluginManifest).Assembly.GetName().Version!.ToString(3);

    /// <summary>A manifest without the member gets it as the first member, and the members it had are kept.</summary>
    [Fact]
    public void Build_ManifestWithoutTheMember_AddsTheReferencedSdkVersion()
    {
        var result = Build("{\n  \"id\": \"demo\",\n  \"version\": \"1.0.0\"\n}\n");

        result.Succeeded.Should().BeTrue(result.Output);
        var root = JsonDocument.Parse(result.Manifest).RootElement;
        root.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        root.GetProperty("id").GetString().Should().Be("demo");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    /// <summary>A stale value is replaced, so a plugin rebuilt on a newer SDK never keeps the old number.</summary>
    [Fact]
    public void Build_ManifestWithAStaleValue_ReplacesIt()
    {
        var result = Build("{\n  \"id\": \"demo\",\n  \"sdkPackageVersion\": \"2.0.0\"\n}\n");

        result.Succeeded.Should().BeTrue(result.Output);
        JsonDocument.Parse(result.Manifest).RootElement.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        result.Manifest.Split("sdkPackageVersion").Should().HaveCount(2, "the member is written once");
    }

    /// <summary>An up to date manifest is left byte for byte as it was.</summary>
    [Fact]
    public void Build_ManifestAlreadyCurrent_IsNotRewritten()
    {
        var current = "{\n  \"sdkPackageVersion\": \"" + ExpectedVersion + "\",\n  \"id\": \"demo\"\n}\n";

        var result = Build(current);

        result.Succeeded.Should().BeTrue(result.Output);
        result.Manifest.Should().Be(current);
    }

    /// <summary>Switching the property off leaves the manifest alone.</summary>
    [Fact]
    public void Build_StampSwitchedOff_LeavesTheManifestAlone()
    {
        var original = "{\n  \"id\": \"demo\"\n}\n";

        var result = Build(original, stamp: false);

        result.Succeeded.Should().BeTrue(result.Output);
        result.Manifest.Should().Be(original);
    }

    /// <summary>An empty object gets the member and stays a valid object.</summary>
    [Fact]
    public void Build_EmptyObject_GetsTheMemberAlone()
    {
        var result = Build("{}");

        result.Succeeded.Should().BeTrue(result.Output);
        result.Manifest.Should().Be("{\n  \"sdkPackageVersion\": \"" + ExpectedVersion + "\"\n}");
    }

    /// <summary>A manifest with Windows line endings keeps them, in the new line as well.</summary>
    [Fact]
    public void Build_ManifestWithCrLf_KeepsTheLineEndings()
    {
        var result = Build("{\r\n  \"id\": \"demo\"\r\n}\r\n");

        result.Succeeded.Should().BeTrue(result.Output);
        result.Manifest.Should().Be("{\r\n  \"sdkPackageVersion\": \"" + ExpectedVersion + "\",\r\n  \"id\": \"demo\"\r\n}\r\n");
    }

    /// <summary>A byte order mark at the start of the file is still there afterwards.</summary>
    [Fact]
    public void Build_ManifestWithByteOrderMark_KeepsIt()
    {
        var result = Build("﻿{\n  \"id\": \"demo\"\n}\n");

        result.Succeeded.Should().BeTrue(result.Output);
        result.Manifest.Should().StartWith("﻿{");
        JsonDocument.Parse(result.Manifest.TrimStart('﻿')).RootElement.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
    }

    /// <summary>A member of the same name inside a nested object or an array is not the manifest's member and is never touched.</summary>
    [Fact]
    public void Build_NestedMemberWithTheSameName_IsLeftAlone()
    {
        var result = Build("{\n  \"extra\": { \"sdkPackageVersion\": \"1.0.0\" },\n  \"list\": [\"sdkPackageVersion\"]\n}\n");

        result.Succeeded.Should().BeTrue(result.Output);
        var root = JsonDocument.Parse(result.Manifest).RootElement;
        root.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        root.GetProperty("extra").GetProperty("sdkPackageVersion").GetString().Should().Be("1.0.0");
    }

    /// <summary>A file that is not a JSON object fails the build with a message naming the file, and is not changed.</summary>
    /// <param name="content">The text of the file.</param>
    [Theory]
    [InlineData("[1, 2]")]
    [InlineData("not json")]
    [InlineData("{\"id\": ")]
    public void Build_FileThatIsNotAnObject_FailsAndLeavesItAlone(string content)
    {
        var result = Build(content);

        result.Succeeded.Should().BeFalse();
        result.Output.Should().Contain("plugin.json");
        result.Manifest.Should().Be(content);
    }

    /// <summary>A member of that name which is not a string fails the build instead of being overwritten.</summary>
    [Fact]
    public void Build_MemberThatIsNotAString_Fails()
    {
        var original = "{\n  \"sdkPackageVersion\": 3\n}\n";

        var result = Build(original);

        result.Succeeded.Should().BeFalse();
        result.Manifest.Should().Be(original);
    }

    /// <summary>The outcome of one nested build.</summary>
    /// <param name="Succeeded">Whether the build ended with exit code zero.</param>
    /// <param name="Output">What the build wrote, standard output and error together.</param>
    /// <param name="Manifest">The text of the project's <c>plugin.json</c> after the build.</param>
    private sealed record BuildResult(bool Succeeded, string Output, string Manifest);

    /// <summary>Builds a project that imports the targets file and references the SDK assembly, then reads its manifest back.</summary>
    /// <param name="manifest">The text of the project's <c>plugin.json</c> before the build.</param>
    /// <param name="stamp">Whether the stamping is left on, or switched off with <c>HostpanelStampSdkPackageVersion</c>.</param>
    /// <returns>The outcome of the build and the manifest as it is afterwards.</returns>
    private static BuildResult Build(string manifest, bool stamp = true)
    {
        var folder = Path.Combine(Path.GetTempPath(), "hostpanel-sdk-targets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var sdkAssembly = Path.Combine(AppContext.BaseDirectory, "Hostpanel.Sdk.dll");
            var targets = Path.Combine(AppContext.BaseDirectory, "build", "Hostpanel.Sdk.targets");
            var manifestPath = Path.Combine(folder, "plugin.json");
            File.WriteAllBytes(manifestPath, new UTF8Encoding(false).GetBytes(manifest));
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

            var (succeeded, output) = RunBuild(folder);
            return new BuildResult(succeeded, output, new UTF8Encoding(false).GetString(File.ReadAllBytes(manifestPath)));
        }
        finally
        {
            DeleteFolder(folder);
        }
    }

    /// <summary>
    /// Runs <c>dotnet build</c> in a folder. No build server and no compiler server is left running, because either would keep
    /// files of the folder open and outlive the test; a build that does not end in time is killed.
    /// </summary>
    /// <param name="folder">The folder holding the project.</param>
    /// <returns>Whether the build succeeded, and its output.</returns>
    private static (bool Succeeded, string Output) RunBuild(string folder)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("build");
        start.ArgumentList.Add("--nologo");
        start.ArgumentList.Add("-nodeReuse:false");
        start.ArgumentList.Add("-p:UseSharedCompilation=false");
        start.ArgumentList.Add("-p:NuGetAudit=false");

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(BuildTimeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException($"The nested build did not finish in {BuildTimeout}.");
        }

        process.WaitForExit();
        return (process.ExitCode == 0, output.Result + errors.Result);
    }

    /// <summary>Deletes a test folder, trying again briefly because a process that has just ended may still hold a file.</summary>
    /// <param name="folder">The folder to delete.</param>
    private static void DeleteFolder(string folder)
    {
        const int attempts = 5;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
                return;
            }
            catch (IOException) when (attempt < attempts)
            {
                Thread.Sleep(200 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < attempts)
            {
                Thread.Sleep(200 * attempt);
            }
        }
    }
}
