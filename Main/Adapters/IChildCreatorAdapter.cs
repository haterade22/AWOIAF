namespace DOTS.Adapters;

public interface IChildCreatorAdapter
{
    void CreateChild(string templateHeroId, string clanId, bool isFemale, int age);
}
