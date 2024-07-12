using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Register.Core.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Core.Application;

internal class SendRegisterPromotionCommandService : ICommandService<SendRegisterPromotionCommand>
{
	private readonly IDistributeRegisterEventRepository _distributeRegisterEventRepository;
	private readonly ILogger<SendRegisterPromotionCommandService> _logger;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPromotionDistributionRepository _promotionDistributionRepository;
	private readonly IPromotionJoinLockRepository _promotionJoinLockRepository;
	private readonly IPromotionRepository _promotionRepository;
	private readonly IPromotionServiceClient _promotionServiceClient;

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
			_logger.LogError(
				"promotion {command.PromotionId} : {command.PlayerCode} is not found.",
				command.PromotionId,
				command.PlayerCode);
			return;
		}

		var promotion = await _promotionRepository.GetPromotionWithCacheAsync(command.PromotionId).ConfigureAwait(false);

		if (promotion is null || promotion.Status != 0)
		{
			_logger.LogError("promotion {command.PromotionId} is not found.", command.PromotionId);
			return;
		}

		if (promotion.Unlimited == 0 && promotion.AmountUsed >= promotion.TotalAmountLimit)
		{
			_logger.LogError(
				"promotion used amount {promotion.AmountUsed} has reached the limit of total amount {promotion.TotalAmountLimit}.",
				promotion.AmountUsed,
				promotion.TotalAmountLimit);
			return;
		}

		var player = await _playerServiceClient.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
		{
			_logger.LogError("player {command.PlayerCode} is not found.", command.PlayerCode);
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
			_logger.LogError("detail {command.PlayerCode} is not found.", command.PlayerCode);
			return;
		}

		var total = await _distributeRegisterEventRepository.GetTotalAmountAsync(promotion.BrandId, promotion.Id) + detail.BonusAmount;

		if (promotion.Unlimited == 1 ||
			total < promotion.TotalAmountLimit ||
			total - detail.BonusAmount <= promotion.TotalAmountLimit)
		{
			var planInfo =
				await _promotionServiceClient.GetPromotionDistributePlanAsync(promotion.Id, 0, cancellationToken);

			if (planInfo is null)
			{
				_logger.LogError("detail {command.PlayerCode} is not found.", command.PlayerCode);
				return;
			}

			var snapshotId = await _distributeRegisterEventRepository.CreateEventAsync(promotion.BrandId, promotion.Id,
				player.PlayerCode,
				detail.BonusAmount).ConfigureAwait(false);

			var bucketAmount = detail.BonusAmount * detail.TurnoverMultiple;
			_logger.LogInformation(
				"{command.PlayerCode} bucket amount is {bucketAmount}. ({detail.BonusAmount} * {detail.TurnoverMultiple})",
				command.PlayerCode,
				bucketAmount,
				detail.BonusAmount,
				detail.TurnoverMultiple);

			_ = await _promotionServiceClient.DistributePromotionBonusAsync(
				planInfo.Id,
				player.PlayerCode,
				detail.BonusAmount,
				bucketAmount,
				"System",
				null,
				[],
				cancellationToken).ConfigureAwait(false);

			var redisTotal = await _promotionRepository.IncreaseToTotalAmountAsync(promotion.Id, (long)detail.BonusAmount);

			if (total != (decimal)redisTotal)
			{
				_ = await _promotionRepository.IncreaseToTotalAmountAsync(promotion.Id, (long)(total - redisTotal));
			}

			await _promotionRepository.CreateTotalAmountRecordForUpdateAsync(promotion.Id, snapshotId, total);
		}
	}
}
