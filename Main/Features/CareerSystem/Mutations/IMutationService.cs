using DOTS.Adapters;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem.Mutations;

public interface IMutationService
{
    AbilityTemplateData MutateAbility(
        AbilityTemplateData template,
        ICareerHeroAdapter hero,
        ICareerDataService dataService,
        ICareerRegistry registry);
}
