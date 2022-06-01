using StackExchange.Redis;
using Tgs.Promotion.Register.Core.Models;

namespace Tgs.Promotion.Register.Core;

public interface IPromotionRepository
{
    ValueTask<Models.Promotion?> GetPromotionWithCacheAsync(Guid promotionId);
    ValueTask<long> IncreaseToTotalAmountAsync(Guid promotionId, long amount);
    ValueTask CreateTotalAmountRecordForUpdateAsync(Guid promotionId, Guid snapshotId, decimal totalAmount);
    IEnumerable<TotalUsedAmount?> GetAllTotalAmountRecordForUpdateAsync();
    ValueTask RemoveAllTotalAmountRecordForUpdateAsync();
    void RemovePromotionCacheAsync(Guid promotionId);

}