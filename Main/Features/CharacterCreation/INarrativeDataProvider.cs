using System.Collections.Generic;
using DOTS.Features.CharacterCreation.Models;

namespace DOTS.Features.CharacterCreation;

public interface INarrativeDataProvider
{
    IReadOnlyList<NarrativeOptionDefinition> LoadMenuOptions(string menuName);
    IReadOnlyList<NarrativeOptionDefinition> GetMenuOptionsForCulture(string menuName, string cultureId);
}
