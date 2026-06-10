using DOTS.Adapters;

namespace DOTS.Features.EditorCacheRebuild.Diff;

public interface ISettlementSnapshotStore
{
    SettlementSnapshotFile? TryLoad(string filePath);

    void Save(string filePath, INavigationCacheAdapter adapter);
}
