using DOTS.Features.Messengers.Domain;

namespace DOTS.Features.Messengers;

public interface IMessengerService
{
    MessengerValidationResult CanSendMessenger(HeroSnapshot target, int playerGold);
    bool RollAccident();
    PositionUpdate AdvancePosition(MapCoord currentPosition, MapCoord targetPosition, float speed);
    float CalculateMessengerSpeed(float mapDiagonal, int travelDays);
}
