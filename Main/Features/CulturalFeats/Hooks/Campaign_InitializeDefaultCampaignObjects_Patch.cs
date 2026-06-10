using HarmonyLib;
using DOTS.Core.Logging;
using TaleWorlds.CampaignSystem;

namespace DOTS.Features.CulturalFeats.Hooks;

[HarmonyPatch(typeof(Campaign), "InitializeDefaultCampaignObjects")]
[HarmonyPatchCategory("Patch18_CulturalFeats")]
public static class Campaign_InitializeDefaultCampaignObjects_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        DotsCulturalFeats.CreateAndRegister();
        IoC.Resolve<IModLogger>().LogInfo("CulturalFeats: all DOTS cultural feats registered");
    }
}
