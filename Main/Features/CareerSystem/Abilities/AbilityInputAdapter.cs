using TaleWorlds.InputSystem;

namespace DOTS.Features.CareerSystem.Abilities;

public class AbilityInputAdapter : IAbilityInputAdapter
{
    public bool IsActivationKeyPressed() => Input.IsKeyPressed(InputKey.V);
}
