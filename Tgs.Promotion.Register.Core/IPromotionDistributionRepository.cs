namespace Tgs.Promotion.Register.Core;

public interface IPromotionDistributionRepository
{
	ValueTask<bool> PromotionDistributionExistsAsync(Guid promotionId, string playerCode);
}