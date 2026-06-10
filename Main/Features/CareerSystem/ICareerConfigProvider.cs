using System.Collections.Generic;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem;

public interface ICareerConfigProvider
{
    IReadOnlyList<CareerDefinition> LoadCareers();
    IReadOnlyList<CareerChoiceGroupDefinition> LoadChoiceGroups();
    IReadOnlyList<CareerChoiceDefinition> LoadChoices();
    int GetMaxPerkPoints();
    AbilityTemplateData GetAbilityTemplate(string templateId);
    AbilityTuningConfig GetAbilityTuning();
}
