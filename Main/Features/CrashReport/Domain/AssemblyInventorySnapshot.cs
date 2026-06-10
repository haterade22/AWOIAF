using System.Collections.Generic;

namespace DOTS.Features.CrashReport.Domain;

public sealed record AssemblyInventorySnapshot(
    IReadOnlyList<AssemblySnapshot> Assemblies);

public sealed record AssemblySnapshot(
    string Name,
    string Version,
    string? Location,
    bool IsInGac);
