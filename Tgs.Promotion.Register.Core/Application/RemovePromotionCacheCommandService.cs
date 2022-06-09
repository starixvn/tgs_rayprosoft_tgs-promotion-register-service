using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Register.Core.Enums;
using Tgs.Promotion.Register.Core.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Core.Application;

internal class RemovePromotionCacheCommandService : ICommandService<RemovePromotionCacheCommand>
{
	private readonly ILogger<SendRegisterPromotionCommandService> _logger;
	private readonly IPromotionRepository _promotionRepository;

	public RemovePromotionCacheCommandService(
		IPromotionRepository promotionRepository,
		ILogger<SendRegisterPromotionCommandService> logger)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_logger = logger ??
			throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask ExecuteAsync(
		RemovePromotionCacheCommand command,
		CancellationToken cancellationToken = default)
	{
		_promotionRepository.RemovePromotionCacheAsync(command.PromotionId);

		return default;
	}
}