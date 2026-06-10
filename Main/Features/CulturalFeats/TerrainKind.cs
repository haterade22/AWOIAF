namespace DOTS.Features.CulturalFeats;

/// <summary>
/// DOTS-owned terrain classification used by the cultural terrain-speed feats.
/// The model maps the sealed TaleWorlds <c>TerrainType</c> to this enum at the
/// boundary so <see cref="ICulturalFeatsService"/> stays free of engine types
/// (ADR-007). Only the terrains DOTS grants bonuses on are represented; every
/// other <c>TerrainType</c> maps to <see cref="None"/>.
/// </summary>
public enum TerrainKind
{
    None = 0,
    Plain,
    Forest,
    Swamp,
    Steppe,
    Desert,
    Snow,
}
