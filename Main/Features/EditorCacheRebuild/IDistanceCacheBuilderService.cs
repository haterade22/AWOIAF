using System.Threading;
using DOTS.Adapters;
using DOTS.Features.EditorCacheRebuild.Phase1;
using DOTS.Features.EditorCacheRebuild.Phase2;
using DOTS.Features.EditorCacheRebuild.Validation;

namespace DOTS.Features.EditorCacheRebuild;

public interface IDistanceCacheBuilderService
{
    CacheBuildResult Build(INavigationCacheAdapter adapter, CancellationToken ct);
}

public readonly struct CacheBuildResult
{
    public readonly Phase1Result Phase1;
    public readonly Phase2Result Phase2;
    public readonly bool Cancelled;
    public readonly SmokeTestResult SmokeTest;

    public CacheBuildResult(Phase1Result phase1, Phase2Result phase2, bool cancelled, SmokeTestResult smokeTest = default)
    {
        Phase1 = phase1;
        Phase2 = phase2;
        Cancelled = cancelled;
        SmokeTest = smokeTest;
    }

    public double TotalSeconds => Phase1.ElapsedSeconds + Phase2.ElapsedSeconds;
}
