using DOTS.Adapters;

namespace DOTS.Features.EditorCacheRebuild.Diff;

public interface ISettlementDiffer
{
    SettlementDiff Compute(SettlementSnapshotFile? previous, INavigationCacheAdapter adapter, int maxChangedThreshold);
}
