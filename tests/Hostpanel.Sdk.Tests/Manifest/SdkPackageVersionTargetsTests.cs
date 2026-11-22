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
    /// <summary>The byte order mark, written as an escape so that no editor can drop it from the source.</summary>
    private const string ByteOrderMark = "\uFEFF";

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
        var result = Build(ByteOrderMark + "{\n  \"id\": \"demo\"\n}\n");

        result.Succeeded.Should().BeTrue(result.Output);
        result.Manifest.Should().StartWith(ByteOrderMark + "{");
        JsonDocument.Parse(result.Manifest.TrimStart(ByteOrderMark[0])).RootElement.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
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

    /// <summary>A quote and braces inside a string value do not confuse the scan: the escaped quote does not end the string.</summary>
    [Fact]
    public void Build_StringWithEscapedQuoteAndBraces_IsScannedCorrectly()
    {
        var result = Build("{\"note\": \"a \\\" } { b\", \"id\": \"x\"}");

        result.Succeeded.Should().BeTrue(result.Output);
        var root = JsonDocument.Parse(result.Manifest).RootElement;
        root.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        root.GetProperty("note").GetString().Should().Be("a \" } { b");
        root.GetProperty("id").GetString().Should().Be("x");
    }

    /// <summary>A string value that equals the member's name is a value, not the member: the real member is still added.</summary>
    [Fact]
    public void Build_ValueEqualToTheMemberName_IsNotTakenForTheMember()
    {
        var result = Build("{\"id\": \"sdkPackageVersion\"}");

        result.Succeeded.Should().BeTrue(result.Output);
        var root = JsonDocument.Parse(result.Manifest).RootElement;
        root.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        root.GetProperty("id").GetString().Should().Be("sdkPackageVersion");
    }

    /// <summary>A file that is not a JSON object fails the build with the task's message naming the file, and is not changed.</summary>
    /// <param name="content">The text of the file.</param>
    /// <param name="message">The part of the task's message that says why.</param>
    [Theory]
    [InlineData("[1, 2]", "is not a JSON object")]
    [InlineData("not json", "is not a JSON object")]
    [InlineData("{\"id\": ", "is not valid JSON: it ends early")]
    [InlineData("{\"id\": \"unclosed", "is not valid JSON: a string is not closed")]
    public void Build_FileThatIsNotAnObject_FailsWithTheTaskMessageAndLeavesItAlone(string content, string message)
    {
        var result = Build(content);

        result.Succeeded.Should().BeFalse();
        result.Output.Should().Contain("plugin.json " + message);
        result.Manifest.Should().Be(content);
    }

    /// <summary>A member of that name which is not a string fails the build instead of being overwritten.</summary>
    [Fact]
    public void Build_MemberThatIsNotAString_FailsWithTheTaskMessage()
    {
        var original = "{\n  \"sdkPackageVersion\": 3\n}\n";

        var result = Build(original);

        result.Succeeded.Should().BeFalse();
        result.Output.Should().Contain("has an \"sdkPackageVersion\" member that is not a string");
        result.Manifest.Should().Be(original);
    }

    /// <summary>Two resolved <c>Hostpanel.Sdk</c> references make the version ambiguous, so the build fails and says why.</summary>
    [Fact]
    public void Build_TwoSdkReferences_FailsAndLeavesTheManifestAlone()
    {
        var original = "{\n  \"id\": \"demo\"\n}\n";

        var result = Build(original, secondSdkReference: true);

        result.Succeeded.Should().BeFalse();
        result.Output.Should().Contain("More than one Hostpanel.Sdk reference");
        result.Manifest.Should().Be(original);
    }

    /// <summary>
    /// A project with two frameworks builds twice, and only the build of the first one stamps the manifest: the stamp is
    /// written once and the other build says it skips, so parallel builds of a plugin never write the same source file together.
    /// </summary>
    [Fact]
    public void Build_TwoTargetFrameworks_StampsOnceByTheFirstFramework()
    {
        var first = $"net{Environment.Version.Major}.0";

        var result = Build("{\n  \"id\": \"demo\"\n}\n", targetFrameworks: $"{first};net8.0");

        result.Succeeded.Should().BeTrue(result.Output);
        JsonDocument.Parse(result.Manifest).RootElement.GetProperty("sdkPackageVersion").GetString().Should().Be(ExpectedVersion);
        result.Output.Split("Hostpanel.Sdk: stamped sdkPackageVersion").Should().HaveCount(2, "the manifest is written once");
        result.Output.Split("Hostpanel.Sdk: sdkPackageVersion is stamped by the " + first + " build").Should().HaveCount(2, "the other framework build says it skips");
    }

    /// <summary>The outcome of one nested build.</summary>
    /// <param name="Succeeded">Whether the build ended with exit code zero.</param>
    /// <param name="Output">What the build wrote, standard output and error together.</param>
    /// <param name="Manifest">The text of the project's <c>plugin.json</c> after the build.</param>
    private sealed record BuildResult(bool Succeeded, string Output, string Manifest);

    /// <summary>Builds a project that imports the targets file and references the SDK assembly, then reads its manifest back.</summary>
    /// <param name="manifest">The text of the project's <c>plugin.json</c> before the build.</param>
    /// <param name="stamp">Whether the stamping is left on, or switched off with <c>HostpanelStampSdkPackageVersion</c>.</param>
    /// <param name="secondSdkReference">Whether a second copy of the SDK assembly is also among the resolved references.</param>
    /// <param name="targetFrameworks">The semicolon separated frameworks of a multi targeting project; the current runtime's single one when <see langword="null"/>.</param>
        /// <returns>The outcome of the build and the manifest as it is afterwards.</returns>
    private static BuildResult Build(
        string manifest,
        bool stamp = true,
        bool secondSdkReference = false,
        string? targetFrameworks = null)
    {
        var folder = Path.Combine(Path.GetTempPath(), "hostpanel-sdk-targets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var sdkAssembly = Path.Combine(AppContext.BaseDirectory, "Hostpanel.Sdk.dll");
            var targets = Path.Combine(AppContext.BaseDirectory, "build", "Hostpanel.Sdk.targets");
            var manifestPath = Path.Combine(folder, "plugin.json");
            File.WriteAllBytes(manifestPath, new UTF8Encoding(false).GetBytes(manifest));

            var frameworks = targetFrameworks is null
                ? $"<TargetFramework>net{Environment.Version.Major}.0</TargetFramework>"
                : $"<TargetFrameworks>{targetFrameworks}</TargetFrameworks>";
            var secondReference = string.Empty;
            if (secondSdkReference)
            {
                var copyFolder = Path.Combine(folder, "second");
                Directory.CreateDirectory(copyFolder);
                File.Copy(sdkAssembly, Path.Combine(copyFolder, "Hostpanel.Sdk.dll"));

                // A second copy of the same assembly is not a second reference to the build (references of one identity are
                // merged), so it is added to the resolved references the way a second package or project would add it.
                secondReference = $"<Target Name=\"AddSecondSdk\" AfterTargets=\"ResolveAssemblyReferences\"><ItemGroup><ReferencePath Include=\"{Path.Combine(copyFolder, "Hostpanel.Sdk.dll")}\" /></ItemGroup></Target>";
            }

            File.WriteAllText(
                Path.Combine(folder, "Fixture.csproj"),
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    {frameworks}
                    <HostpanelStampSdkPackageVersion>{(stamp ? "true" : "false")}</HostpanelStampSdkPackageVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <Reference Include="Hostpanel.Sdk"><HintPath>{sdkAssembly}</HintPath></Reference>
                  </ItemGroup>
                  {secondReference}
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
        start.ArgumentList.Add("--disable-build-servers");
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

    /// <summary>
    /// Deletes a test folder, trying again briefly because a process that has just ended may still hold a file. A folder that
    /// still cannot be deleted is left in the temp directory: this runs in a <c>finally</c>, and an exception from it would
    /// replace the failure the test is reporting.
    /// </summary>
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
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Thread.Sleep(200 * attempt);
            }
        }
    }
}
