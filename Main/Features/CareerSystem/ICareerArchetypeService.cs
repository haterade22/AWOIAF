using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem;

public interface ICareerArchetypeService
{
    bool TryGetArchetype(string careerId, out CareerArchetype archetype);
}
