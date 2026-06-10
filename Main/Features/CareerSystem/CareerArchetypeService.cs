using System.Collections.Generic;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem;

public sealed class CareerArchetypeService : ICareerArchetypeService
{
    private readonly IReadOnlyDictionary<string, CareerArchetype> _careerArchetypes;

    public CareerArchetypeService(IReadOnlyDictionary<string, CareerArchetype> careerArchetypes)
    {
        _careerArchetypes = careerArchetypes ?? new Dictionary<string, CareerArchetype>();
    }

    public bool TryGetArchetype(string careerId, out CareerArchetype archetype)
    {
        if (string.IsNullOrEmpty(careerId))
        {
            archetype = default;
            return false;
        }
        return _careerArchetypes.TryGetValue(careerId, out archetype);
    }
}
