using System.Runtime.CompilerServices;

// CORE-007 exposes a few internal test seams (FallbackManager.ConfigureConnectivityProvider,
// TickForTest, and the later FPS/thermal provider hooks) so PlayMode tests can drive the signal
// sources deterministically without a device or network. These stay internal — production code in
// the pillar assemblies cannot reach them; only the test assembly below can.
//
// The test assembly name must match the asmdef in Assets/_AnatomiQ/Tests/PlayMode/. If that asmdef
// is named differently, update the string here to match (otherwise the tests fail to compile with
// "inaccessible due to its protection level").
[assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]
