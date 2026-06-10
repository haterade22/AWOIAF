using DOTS.Features.FactionMap.Models;

namespace DOTS.Features.FactionMap;

public interface IFactionHoverService
{
    HoverStateChange? UpdateHover(string currentHoveredFaction);
    void Reset();
}
