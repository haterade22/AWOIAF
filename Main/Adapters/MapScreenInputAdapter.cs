using TaleWorlds.InputSystem;

namespace DOTS.Adapters;

public class MapScreenInputAdapter : IMapScreenInputAdapter
{
    public bool IsF6Pressed => Input.IsKeyPressed(InputKey.F6);
}
