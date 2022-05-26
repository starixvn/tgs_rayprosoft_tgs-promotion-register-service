using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.Promotion.Register.Core;
using Tgs.Promotion.Register.Repository.Entities;

namespace Tgs.Promotion.Register.Repository;

internal class PromotionDistributionRepository : IPromotionDistributionRepository
{
	private readonly TgsContext _tgsContext;

	public PromotionDistributionRepository(TgsContext tgsContext) => _tgsContext = tgsContext;

	public async ValueTask<bool> PromotionDistributionExistsAsync(Guid promotionId, string playerCode)
		=> await _tgsContext.PromotionDistributes.AnyAsync(x
			=> x.PromotionId == promotionId && x.PlayerCode == playerCode);
}