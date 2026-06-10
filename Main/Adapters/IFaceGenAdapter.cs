using TaleWorlds.Core;

namespace DOTS.Adapters;

public interface IFaceGenAdapter
{
    string[] GetRaceNames();
    Monster GetBaseMonsterFromRace(int race);
}
