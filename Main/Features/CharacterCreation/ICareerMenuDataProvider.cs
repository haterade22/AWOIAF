using System.Collections.Generic;
using DOTS.Features.CharacterCreation.Models;

namespace DOTS.Features.CharacterCreation;

public interface ICareerMenuDataProvider
{
    IReadOnlyList<CareerMenuOptionDefinition> LoadCareerMenuOptions();
    CareerMenuOptionDefinition GetOptionForCareer(string careerStringId);
}
