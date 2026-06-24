using System.Runtime.CompilerServices;

// Mirrors the Core / Anatomy assemblies: expose internal test seams (ConfigureServicesForTest,
// ComposeMetricsText) to the PlayMode test assembly only. The seams themselves are guarded with
// #if UNITY_INCLUDE_TESTS so they strip from player builds.
[assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]
