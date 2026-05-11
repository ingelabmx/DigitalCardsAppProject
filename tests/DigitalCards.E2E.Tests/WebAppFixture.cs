using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DigitalCards.E2E.Tests;

public sealed class WebAppFixture : IAsyncLifetime
{
    private Process? _process;

    public Uri BaseAddress { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        BaseAddress = new Uri($"http://127.0.0.1:{port}");
        var repoRoot = FindRepoRoot();
        var projectPath = Path.Combine(repoRoot, "src", "DigitalCards.Web", "DigitalCards.Web.csproj");

        var startInfo = new ProcessStartInfo("dotnet", $"run --no-launch-profile --project \"{projectPath}\" --urls {BaseAddress}")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DigitalCards__UseFakeIntegrations"] = "true";
        startInfo.Environment["DigitalCards__PersistenceProvider"] = "InMemory";

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start DigitalCards.Web.");

        using var client = new HttpClient { BaseAddress = BaseAddress };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                var response = await client.GetAsync("/health", timeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                await Task.Delay(500, timeout.Token);
            }
        }

        throw new TimeoutException("DigitalCards.Web did not become healthy in time.");
    }

    public Task DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
        }

        _process?.Dispose();
        return Task.CompletedTask;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DigitalCardsApp.Modern.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
