using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Core.Application;

internal class RemovePromotionCacheCommandService : ICommandService<RemovePromotionCacheCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public RemovePromotionCacheCommandService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(
		RemovePromotionCacheCommand command,
		CancellationToken cancellationToken = default)
	{
		_promotionRepository.RemovePromotionCache(command.PromotionId);

		return default;
	}
}
