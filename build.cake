#tool "nuget:?package=DependencyCheck.Runner.Tool&include=./**/dependency-check.sh&include=./**/dependency-check.bat"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solution = "Cake.DependencyCheck.sln";
var appName = "Cake.DependencyCheck";

var apiKey = EnvironmentVariable("NUGET_API_KEY") ?? "abcdef0123456789";
var buildNumber = EnvironmentVariable("APPVEYOR_BUILD_NUMBER") ?? "0";

var version = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME") ?? "1.2.0";

Setup(context =>
{
    if (!DirectoryExists("nuget"))
    {
        CreateDirectory("nuget");
    }
});

Task("Clean")
    .Does(() =>
    {
        CleanDirectory("nuget");
    });

Task("Restore")
    .Does(() =>
    {
        NuGetRestore(solution);
    });

Task("Build")
    .Does(() =>
    {
        MSBuild(solution, new MSBuildSettings
        {
            Verbosity = Verbosity.Minimal,
            Configuration = configuration
        });
    });

Task("Test")
    .Does(() =>
    {
        var testProjects = GetFiles("./test/**/*.csproj");
        foreach (var testProject in testProjects)
        {
            var projectFile = MakeAbsolute(testProject).ToString();
            var dotNetTestSettings = new DotNetCoreTestSettings
            {
                Configuration = configuration,
                NoBuild = true
            };

            DotNetCoreTest(projectFile, dotNetTestSettings);
        }
    });

Task("Pack")
    .Does(() =>
    {
        var nuGetPackSettings = new NuGetPackSettings
        {
            Id = appName,
            Version = version,
            Title = appName,
            Authors = new[] { "Burak İnce" },
            Owners = new[] { "Burak İnce", "cake-contrib" },
            Description = "OWASP Dependency-Check is a utility that identifies project dependencies and checks if there are any known, publicly disclosed, vulnerabilities.",
            Summary = "OWASP Dependency-Check is a utility that identifies project dependencies and checks if there are any known, publicly disclosed, vulnerabilities.",
            IconUrl = new Uri("https://cdn.rawgit.com/cake-contrib/graphics/a5cf0f881c390650144b2243ae551d5b9f836196/png/cake-contrib-medium.png"),
            ProjectUrl = new Uri("https://github.com/burakince/Cake.DependencyCheck"),
            LicenseUrl = new Uri("https://github.com/burakince/Cake.DependencyCheck/blob/master/LICENSE"),
            Tags = new [] { "Cake", "OWASP", "DependencyCheck", "Security" },
            RequireLicenseAcceptance = false,
            Symbols = false,
            NoPackageAnalysis = true,
            Files = new [] 
            {
                new NuSpecContent
                {
                    Source = "netstandard1.6/Cake.DependencyCheck.dll",
                    Target = "lib/netstandard1.6"
                },
                new NuSpecContent
                {
                    Source = "netstandard1.6/Cake.DependencyCheck.xml",
                    Target = "lib/netstandard1.6"
                },
                new NuSpecContent
                {
                    Source = "net45/Cake.DependencyCheck.dll",
                    Target = "lib/net45"
                },
                new NuSpecContent
                {
                    Source = "net45/Cake.DependencyCheck.xml",
                    Target = "lib/net45"
                },
                new NuSpecContent
                {
                    Source = "net46/Cake.DependencyCheck.dll",
                    Target = "lib/net46"
                },
                new NuSpecContent
                {
                    Source = "net46/Cake.DependencyCheck.xml",
                    Target = "lib/net46"
                }
            },
            BasePath = "./src/Cake.DependencyCheck/bin/release",
            OutputDirectory = "./nuget"
        };

        NuGetPack(nuGetPackSettings);
    });

Task("Update-Appveyor-Build-Version")
    .Does(() =>
    {
        if (AppVeyor.IsRunningOnAppVeyor)
        {
            AppVeyor.UpdateBuildVersion(version + string.Concat("+", buildNumber));
        }
        else
        {
            Information("Not running on AppVeyor");
        }
    });

Task("Publish")
    .Does(() =>
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Could not resolve Nuget API key.");
        };

        var packagePath = "./nuget/" + appName + "." + version + ".nupkg";
        Information("Publishing: {0}", packagePath);
        NuGetPush(packagePath, new NuGetPushSettings
        {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = apiKey
        });
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

Task("AppVeyor")
    .IsDependentOn("Default")
    .IsDependentOn("Update-Appveyor-Build-Version")
    .IsDependentOn("Publish");

RunTarget(target);
