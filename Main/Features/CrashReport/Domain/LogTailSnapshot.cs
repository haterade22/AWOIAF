using System.Collections.Generic;

namespace DOTS.Features.CrashReport.Domain;

public sealed record LogTailSnapshot(
    string? DotsDebugLogPath,
    IReadOnlyList<string> DotsDebugLogTail,
    string? RglLogPath,
    IReadOnlyList<string> RglLogTail);
