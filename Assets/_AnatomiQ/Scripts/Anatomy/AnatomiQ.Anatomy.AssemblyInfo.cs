using System.Runtime.CompilerServices;

// Lets the PlayMode test assembly inject test seams (e.g. BodyRenderer.ConfigureForTest) without
// exposing them in the public API — the same pattern CORE-007 uses in the Core assembly.
// If an AssemblyInfo.cs already exists in the Anatomy assembly, add only the line below to it.
[assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]
