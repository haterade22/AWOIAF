using System.Collections.Generic;
using DOTS.Features.CharacterCreation.Models;

namespace DOTS.Features.CharacterCreation;

public interface ICultureCreationDataProvider
{
    IReadOnlyList<CultureCreationData> LoadCultures();
    CultureCreationData GetCultureData(string cultureId);
}
