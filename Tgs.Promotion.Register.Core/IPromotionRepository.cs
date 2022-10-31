using StackExchange.Redis;
using Tgs.Promotion.Register.Core.Models;

namespace Tgs.Promotion.Register.Core;

public interface IPromotionRepository
{
	ValueTask<Models.Promotion?> GetPromotionWithCacheAsync(Guid promotionId);
	ValueTask<long> IncreaseToTotalAmountAsync(Guid promotionId, long amount);
	ValueTask CreateTotalAmountRecordForUpdateAsync(Guid promotionId, Guid snapshotId, decimal totalAmount);
	IEnumerable<TotalUsedAmount?> GetAllTotalAmountRecordForUpdate();
	ValueTask RemoveAllTotalAmountRecordForUpdateAsync();
	void RemovePromotionCache(Guid promotionId);

}