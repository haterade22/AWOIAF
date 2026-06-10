using DryIoc;
using DOTS.Features.EditorCacheRebuild.Caching;
using DOTS.Features.EditorCacheRebuild.Checkpoint;
using DOTS.Features.EditorCacheRebuild.Diff;
using DOTS.Features.EditorCacheRebuild.Phase1;
using DOTS.Features.EditorCacheRebuild.Phase2;
using DOTS.Features.EditorCacheRebuild.Validation;

namespace DOTS.Features.EditorCacheRebuild;

public static class EditorCacheRebuildIoC
{
    public static void RegisterEditorCacheRebuildFeature(IContainer container)
    {
        container.Register<ICacheRebuildConfigProvider, CacheRebuildConfigProvider>(Reuse.Singleton);
        container.Register<IPathReuseCache, PathReuseCache>(Reuse.Singleton);
        container.Register<IPersistentPathCache, PersistentPathCache>(Reuse.Singleton);
        container.Register<SerialPhase1Builder>(Reuse.Singleton);
        container.Register<ParallelPhase1Builder>(Reuse.Singleton);
        container.Register<SerialPhase2Builder>(Reuse.Singleton);
        container.Register<ParallelPhase2Builder>(Reuse.Singleton);
        container.Register<ISmokeTestGate, SmokeTestGate>(Reuse.Singleton);
        container.Register<IValidationReportWriter, ValidationReportWriter>(Reuse.Singleton);
        container.Register<ICheckpointSerializer, CheckpointSerializer>(Reuse.Singleton);
        container.Register<ISettlementSnapshotStore, SettlementSnapshotStore>(Reuse.Singleton);
        container.Register<ISettlementDiffer, SettlementDiffer>(Reuse.Singleton);
        container.Register<IDistanceCacheBuilderService, CacheBuilderService>(Reuse.Singleton);
        container.Register<IRuntimeCacheRebuildService, RuntimeCacheRebuildService>(Reuse.Singleton);
    }
}
