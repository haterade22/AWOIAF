using System.Collections.Generic;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem;

/// <summary>Loads + validates the career-quest definitions from <c>dots_career_quests.xml</c>.</summary>
public interface ICareerQuestConfigProvider
{
    IReadOnlyList<CareerQuestDefinition> LoadQuests();
}
