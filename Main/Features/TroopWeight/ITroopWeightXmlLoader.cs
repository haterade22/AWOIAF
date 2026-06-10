using System.Collections.Generic;

namespace DOTS.Features.TroopWeight;

public interface ITroopWeightXmlLoader
{
    Dictionary<string, float> GetTroopWeights();
    void ReloadWeights();
}
