namespace Tgs.Promotion.Register.Core;

public interface IPromotionParticipantRepository
{
    ValueTask<bool> JoinedPromotionExistsAsync(Guid promotionId, string playerCode);
}