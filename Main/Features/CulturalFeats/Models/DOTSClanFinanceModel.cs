using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace DOTS.Features.CulturalFeats.Models;

public class DotsClanFinanceModel : DefaultClanFinanceModel
{
    private readonly ICulturalFeatsService _feats;

    public DotsClanFinanceModel(ICulturalFeatsService feats)
    {
        _feats = feats;
    }

    public override ExplainedNumber CalculateTownIncomeFromTariffs(
        Clan clan, Town town, bool applyWithdrawals = false)
    {
        var result = base.CalculateTownIncomeFromTariffs(clan, town, applyWithdrawals);
        _feats.ApplyTariffIncomeFeats(CultureFeatAdapter.FromOrNull(clan?.Culture), ref result);
        return result;
    }
}
