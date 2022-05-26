namespace Tgs.Promotion.Register.Core;

public interface IPromotionJoinLockRepository
{
    ValueTask<bool> SetWhenNotExistsAsync(string playerCode, Guid promotionId);
}