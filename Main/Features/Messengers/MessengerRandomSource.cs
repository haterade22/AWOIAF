using TaleWorlds.Core;

namespace DOTS.Features.Messengers;

public class MessengerRandomSource : IMessengerRandomSource
{
    public float NextFloat() => MBRandom.RandomFloat;
}
