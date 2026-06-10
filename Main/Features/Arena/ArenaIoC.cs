using DryIoc;

namespace DOTS.Features.Arena;

public static class ArenaIoC
{
    public static void RegisterArenaFeature(IContainer container)
    {
        // Phase 9b #137 — service extracted from DotsTournamentModel to satisfy rule-4
        // (no inline switch/foreach/branching in GameModel overrides).
        container.Register<ITournamentService, TournamentService>(Reuse.Singleton);
    }
}
