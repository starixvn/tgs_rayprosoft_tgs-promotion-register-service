using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Register.Core.Enums;
using Tgs.Promotion.Register.Core.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Core.Application;

internal class SendRegisterPromotionCommandService : ICommandService<SendRegisterPromotionCommand>
{
	private readonly IDistributeRegisterEventRepository _distributeRegisterEventRepository;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPromotionJoinLockRepository _promotionJoinLockRepository;
	private readonly IPromotionDistributionRepository _promotionDistributionRepository;
	private readonly IPromotionRepository _promotionRepository;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ILogger<SendRegisterPromotionCommandService> _logger;

	public SendRegisterPromotionCommandService(
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient,
		IPromotionRepository promotionRepository,
		IPromotionJoinLockRepository promotionJoinLockRepository,
		IPromotionDistributionRepository promotionDistributionRepository,
		IDistributeRegisterEventRepository distributeRegisterEventRepository,
		ILogger<SendRegisterPromotionCommandService> logger)
	{
		_promotionServiceClient =
			promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionJoinLockRepository = promotionJoinLockRepository ??
			throw new ArgumentNullException(nameof(promotionJoinLockRepository));
		_promotionDistributionRepository = promotionDistributionRepository ??
			throw new ArgumentNullException(nameof(promotionDistributionRepository));
		_distributeRegisterEventRepository = distributeRegisterEventRepository ??
			throw new ArgumentNullException(nameof(distributeRegisterEventRepository));
		_logger = logger ??
			throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(
		SendRegisterPromotionCommand command,
		CancellationToken cancellationToken = default)
	{
		var promotionIsJoined =
			!await _promotionJoinLockRepository.SetWhenNotExistsAsync(command.PlayerCode,
				command.PromotionId).ConfigureAwait(false) ||
			await _promotionDistributionRepository
				.PromotionDistributionExistsAsync(command.PromotionId, command.PlayerCode).ConfigureAwait(false);

		if (promotionIsJoined)
		{
			_logger.LogError($"promotion {command.PromotionId} : {command.PlayerCode} is not found.");
			return;
		}

		var promotion = await _promotionRepository.GetPromotionWithCacheAsync(command.PromotionId).ConfigureAwait(false);

		if (promotion is null || promotion.Status != 0)
		{
			_logger.LogError($"promotion {command.PromotionId} is not found.");
			return;
		}

		if (promotion.Unlimited == 0 && promotion.AmountUsed >= promotion.TotalAmountLimit)
		{
			_logger.LogError(
				$"promotion used amount {promotion.AmountUsed} has reached the limit of total amount {promotion.TotalAmountLimit}.");
			return;
		}

		var player = await _playerServiceClient.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
		{
			_logger.LogError($"player {command.PlayerCode} is not found.");
			return;
		}

		if (player.RegisteredOn is null ||
		    player.RegisteredOn < promotion.From ||
		    player.RegisteredOn > promotion.To)
		{
			_logger.LogError("join date is not during the promotion.");
			return;
		}

		var detail = JsonSerializer.Deserialize<RegisterSetting>(promotion.CampaignDetail,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		if (detail is null)
		{
			_logger.LogError($"detail {command.PlayerCode} is not found.");
			return;
		}

		var total = await _promotionRepository.IncreaseToTotalAmountAsync(promotion.Id, (long)detail.BonusAmount);

		if (promotion.Unlimited == 1 || 
		    total < promotion.TotalAmountLimit ||
		    total - detail.BonusAmount <= promotion.TotalAmountLimit)
		{
			var planInfo =
				await _promotionServiceClient.GetPromotionDistributePlanAsync(promotion.Id, 0, cancellationToken);

			if (planInfo is null)
			{
				_logger.LogError($"detail {command.PlayerCode} is not found.");
				return;
			}

			var snapshotId = await _distributeRegisterEventRepository.CreateEventAsync(promotion.BrandId, promotion.Id,
				player.PlayerCode,
				detail.BonusAmount).ConfigureAwait(false);

			var bucketAmount = detail.BonusAmount * detail.TurnoverMultiple;
			_logger.LogInformation(
				$"{command.PlayerCode} bucket amount is {bucketAmount}. ({detail.BonusAmount} * {detail.TurnoverMultiple})");

			await _promotionServiceClient.DistributePromotionBonusAsync(planInfo.Id, player.PlayerCode, detail.BonusAmount, bucketAmount, "System", cancellationToken).ConfigureAwait(false);

			await _promotionRepository.CreateTotalAmountRecordForUpdateAsync(promotion.Id, snapshotId, total);
		}
	}
}