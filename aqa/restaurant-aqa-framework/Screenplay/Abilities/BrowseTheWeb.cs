using Microsoft.Playwright;
using RestaurantAqaFramework.Config;
using RestaurantAqaFramework.Screenplay.Core;

namespace RestaurantAqaFramework.Screenplay.Abilities;

/// <summary>
/// Screenplay Ability wrapping a Playwright BrowserContext and Page for an Actor.
/// Supports isolated browser sessions (so Owner, Guests, and Waiter can act in parallel windows)
/// and captures screenshots, traces, and videos into TestResults/Artifacts on failure.
/// </summary>
public sealed class BrowseTheWeb : IAbility
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _tracingStarted;

    public IBrowserContext Context { get; private set; } = null!;
    public IPage Page { get; private set; } = null!;
    public string FrontendBaseUrl { get; }

    private BrowseTheWeb(string? frontendBaseUrl = null)
    {
        FrontendBaseUrl = (frontendBaseUrl ?? TestSettings.Current.FrontendBaseUrl).TrimEnd('/');
    }

    public static async Task<BrowseTheWeb> OpenNewBrowserSessionAsync(string? frontendBaseUrl = null)
    {
        var ability = new BrowseTheWeb(frontendBaseUrl);
        await ability.InitializeAsync();
        return ability;
    }

    private async Task InitializeAsync()
    {
        var settings = TestSettings.Current;
        _playwright = await Playwright.CreateAsync();

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = settings.Headless,
            SlowMo = settings.SlowMoMs
        };

        _browser = settings.Browser.ToLowerInvariant() switch
        {
            "firefox" => await _playwright.Firefox.LaunchAsync(launchOptions),
            "webkit" => await _playwright.Webkit.LaunchAsync(launchOptions),
            _ => await _playwright.Chromium.LaunchAsync(launchOptions)
        };

        var artifactsRoot = Path.GetFullPath(settings.ArtifactsDir);
        Directory.CreateDirectory(artifactsRoot);

        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        };

        if (settings.RecordVideo)
        {
            var videoDir = Path.Combine(artifactsRoot, "videos");
            Directory.CreateDirectory(videoDir);
            contextOptions.RecordVideoDir = videoDir;
        }

        Context = await _browser.NewContextAsync(contextOptions);
        Context.SetDefaultTimeout(settings.DefaultTimeoutMs);

        if (settings.CaptureTrace)
        {
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
            _tracingStarted = true;
        }

        Page = await Context.NewPageAsync();
    }

    public async Task<string?> CaptureFailureArtifactsAsync(string scenarioName)
    {
        var safeName = string.Join("_", scenarioName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var artifactsRoot = Path.GetFullPath(TestSettings.Current.ArtifactsDir);
        Directory.CreateDirectory(artifactsRoot);

        var screenshotPath = Path.Combine(artifactsRoot, $"{safeName}_{timestamp}.png");
        try
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });
        }
        catch
        {
            // Ignore if page is already closed
        }

        if (_tracingStarted)
        {
            var tracePath = Path.Combine(artifactsRoot, $"{safeName}_{timestamp}_trace.zip");
            try
            {
                await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
                _tracingStarted = false;
            }
            catch
            {
                // Ignore trace stop errors
            }
        }

        return screenshotPath;
    }

    public async ValueTask DisposeAsync()
    {
        if (_tracingStarted)
        {
            try
            {
                await Context.Tracing.StopAsync();
            }
            catch
            {
                // Ignore
            }
        }

        if (Context is not null)
            await Context.CloseAsync();

        if (_browser is not null)
            await _browser.CloseAsync();

        _playwright?.Dispose();
    }
}

