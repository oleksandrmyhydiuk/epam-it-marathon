namespace RestaurantAqaFramework.Config;

/// <summary>
/// Loads test configuration from .env file and/or process environment variables.
/// Process environment variables take precedence over .env file entries (for CI/CD compatibility).
/// </summary>
public sealed class TestSettings
{
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public string ApiBaseUrl { get; init; } = "https://k93iy23zq1.execute-api.eu-north-1.amazonaws.com/develop";
    public string? DefaultLocationId { get; init; }
    public string? DefaultTableId { get; init; }

    public string? OwnerEmail { get; init; }
    public string? OwnerPassword { get; init; }
    public string? OtherCustomerEmail { get; init; }
    public string? OtherCustomerPassword { get; init; }
    public string? WaiterEmail { get; init; }
    public string? WaiterPassword { get; init; }

    public string Browser { get; init; } = "chromium";
    public bool Headless { get; init; } = true;
    public float SlowMoMs { get; init; } = 0;
    public float DefaultTimeoutMs { get; init; } = 30_000;
    public bool RecordVideo { get; init; } = true;
    public bool CaptureTrace { get; init; } = true;
    public string ArtifactsDir { get; init; } = "TestResults/Artifacts";

    public bool ReportPortalEnabled { get; init; }
    public string? ReportPortalUrl { get; init; }
    public string? ReportPortalProject { get; init; }
    public string? ReportPortalApiKey { get; init; }

    private static readonly Lazy<TestSettings> Instance = new(LoadInternal);
    public static TestSettings Current => Instance.Value;

    private static TestSettings LoadInternal()
    {
        LoadDotEnvIfExists();

        return new TestSettings
        {
            FrontendBaseUrl = GetEnv("FRONTEND_BASE_URL", "http://localhost:4200").TrimEnd('/'),
            ApiBaseUrl = GetEnv("API_BASE_URL", "https://k93iy23zq1.execute-api.eu-north-1.amazonaws.com/develop").TrimEnd('/'),
            DefaultLocationId = GetOptionalEnv("DEFAULT_LOCATION_ID"),
            DefaultTableId = GetOptionalEnv("DEFAULT_TABLE_ID"),
            OwnerEmail = GetOptionalEnv("OWNER_EMAIL"),
            OwnerPassword = GetOptionalEnv("OWNER_PASSWORD"),
            OtherCustomerEmail = GetOptionalEnv("OTHER_CUSTOMER_EMAIL"),
            OtherCustomerPassword = GetOptionalEnv("OTHER_CUSTOMER_PASSWORD"),
            WaiterEmail = GetOptionalEnv("WAITER_EMAIL"),
            WaiterPassword = GetOptionalEnv("WAITER_PASSWORD"),
            Browser = GetEnv("BROWSER", "chromium"),
            Headless = bool.TryParse(GetOptionalEnv("HEADLESS"), out var headless) ? headless : true,
            SlowMoMs = float.TryParse(GetOptionalEnv("SLOW_MO_MS"), out var slowMo) ? slowMo : 0f,
            DefaultTimeoutMs = float.TryParse(GetOptionalEnv("DEFAULT_TIMEOUT_MS"), out var timeout) ? timeout : 30_000f,
            RecordVideo = !bool.TryParse(GetOptionalEnv("RECORD_VIDEO"), out var video) || video,
            CaptureTrace = !bool.TryParse(GetOptionalEnv("CAPTURE_TRACE"), out var trace) || trace,
            ArtifactsDir = GetEnv("ARTIFACTS_DIR", "TestResults/Artifacts"),
            ReportPortalEnabled = bool.TryParse(GetOptionalEnv("REPORT_PORTAL_ENABLED"), out var rp) && rp,
            ReportPortalUrl = GetOptionalEnv("REPORT_PORTAL_URL"),
            ReportPortalProject = GetOptionalEnv("REPORT_PORTAL_PROJECT"),
            ReportPortalApiKey = GetOptionalEnv("REPORT_PORTAL_API_KEY")
        };
    }

    private static void LoadDotEnvIfExists()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"))
        };

        var envPath = candidatePaths.FirstOrDefault(File.Exists);
        if (envPath is null)
            return;

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"', '\'');

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string GetEnv(string key, string fallback) =>
        GetOptionalEnv(key) ?? fallback;

    private static string? GetOptionalEnv(string key)
    {
        var value = Environment.GetEnvironmentVariable(key)?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}

