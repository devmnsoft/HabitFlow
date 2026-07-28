using System.Diagnostics.Metrics;

namespace HabitFlow.Web;

internal static class WebRuntimeDiagnostics
{
    private static readonly Meter Meter = new("HabitFlow.Web.Runtime", "6.6.1");
    internal static readonly Counter<long> NavigationFeatureFailures = Meter.CreateCounter<long>("navigation_feature_failures");
    internal static readonly Counter<long> ErrorPageCount = Meter.CreateCounter<long>("error_page_count");
}
