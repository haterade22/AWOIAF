using DOTS.Features.Diplomacy.Models;

namespace DOTS.Features.Diplomacy;

public interface IDiplomacyConfigProvider
{
    DiplomacyConfig LoadConfig();
}
