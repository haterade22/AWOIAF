using System.Collections.Generic;

namespace DOTS.Features.CrashReport.Domain;

public sealed record FrameTimingSnapshot(
    IReadOnlyList<float> RecentFrameTimesMs,
    double AverageFrameTimeMs,
    double FpsAtCrash,
    int SampleCount);
