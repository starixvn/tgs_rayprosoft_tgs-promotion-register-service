using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using Tgs.Promotion.Register.Core;
using Tgs.Promotion.Register.Core.Models;
using Tgs.Promotion.Register.Repository.Entities;

namespace Tgs.Promotion.Register.Repository;

internal class PromotionRepository : IPromotionRepository
{
	private readonly TgsContext _tgsContext;
	private readonly PromotionMemoryCache _memoryCache;
	private readonly IRedisConnectionFactory _redisConnectionFactory;
	private const string TotalUsedAmountKey = "promotion:register:totalusedamount:{0}";

	public PromotionRepository(TgsContext tgsContext, PromotionMemoryCache memoryCache, IRedisConnectionFactory redisConnectionFactory)
	{
		_tgsContext = tgsContext;
		_memoryCache = memoryCache;
		_redisConnectionFactory = redisConnectionFactory;
	}

	public async ValueTask<Core.Models.Promotion?> GetPromotionWithCacheAsync(Guid promotionId)
	{
		return await _memoryCache.Cache.GetOrCreateAsync($"promotion:{promotionId}", async cacheEntry =>
		{
			_ = cacheEntry.SetSize(1);
			_ = cacheEntry.SetSlidingExpiration(TimeSpan.FromHours(1));
			cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

			var promotion = await _tgsContext.Promotions.FindAsync(promotionId);

			return promotion is null
				? null
				: new Core.Models.Promotion()
				{
					Id = promotion.Id,
					Name = promotion.Name,
					From = promotion.From,
					To = promotion.To,
					Status = promotion.Status,
					AutoSentPlayerBonus = promotion.AutoSentPlayerBonus,
					BrandId = promotion.BrandId,
					PromotionType = promotion.PromotionType,
					SettlementIntervalType = promotion.SettlementIntervalType,
					TotalAmountLimit = promotion.TotalAmountLimit,
					AmountUsed = promotion.AmountUsed,
					Unlimited = promotion.Unlimited,
					GameType = promotion.GameType,
					Percentage = promotion.Percentage,
					BonusAmountLimit = promotion.BonusAmountLimit,
					TurnoverMultiple = promotion.TurnoverMultiple,
					VipLevelId = promotion.VipLevelId,
					CampaignDetail = promotion.CampaignDetail
				};
		});

	}

	public async ValueTask<long> IncreaseToTotalAmountAsync(Guid promotionId, long amount)
	{
		var key = string.Format(TotalUsedAmountKey, promotionId);
		var db = _redisConnectionFactory.GetDataBase();
		var totalAmount = await db.StringIncrementAsync(key, amount);

		return totalAmount;
	}

	public async ValueTask CreateTotalAmountRecordForUpdateAsync(Guid promotionId, Guid snapshotId, decimal totalAmount)
	{
		var db = _redisConnectionFactory.GetDataBase();

		var value = new TotalUsedAmount
		{
			PromotionId = promotionId,
			SnapshotId = snapshotId,
			TotalAmount = totalAmount
		};

		_ = await db.SetAddAsync("promotion:register:totalusedamount", JsonSerializer.Serialize(value));
	}

	public IEnumerable<TotalUsedAmount?> GetAllTotalAmountRecordForUpdate()
	{
		var db = _redisConnectionFactory.GetDataBase();
		var items = db.SetMembers("promotion:register:totalusedamount");
		return items.Select(x => JsonSerializer.Deserialize<TotalUsedAmount>(x));
	}

	public async ValueTask RemoveAllTotalAmountRecordForUpdateAsync()
	{
		var db = _redisConnectionFactory.GetDataBase();
		_ = await db.KeyDeleteAsync("promotion:register:totalusedamount");
	}

	public void RemovePromotionCache(Guid promotionId)
	{
		_memoryCache.Cache.Remove($"promotion:{promotionId}");
	}
}