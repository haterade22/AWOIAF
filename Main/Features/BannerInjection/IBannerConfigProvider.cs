using System.Collections.Generic;

namespace DOTS.Features.BannerInjection;

public interface IBannerConfigProvider
{
    Dictionary<string, string> GetKingdomBannerKeys();
    Dictionary<string, string> GetClanBannerKeys();
}
