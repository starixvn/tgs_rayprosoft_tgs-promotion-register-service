using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Register.Core;
using Tgs.Promotion.Register.Core.Application;
using Tgs.Promotion.Register.Core.Enums;

namespace Tgs.Promotion.Register.UnitTest;

[TestClass]
public class SendRegisterPromotionTests
{
	[TestMethod]
	public async Task Can_Send_Register_Bonus()
	{
		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		var fakeDistributeRegisterEventRepository = Substitute.For<IDistributeRegisterEventRepository>();
		var fakePlayerBucketRepository = Substitute.For<IPlayerBucketRepository>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionJoinLockRepository = Substitute.For<IPromotionJoinLockRepository>();
		var fakePromotionDistributionRepository = Substitute.For<IPromotionDistributionRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		_ = fakePromotionDistributionRepository
			.PromotionDistributionExistsAsync(Arg.Any<Guid>(), Arg.Any<string>())
			.Returns(false);

		var promotion = new Core.Models.Promotion
		{
			From = DateTime.Now.AddDays(-1),
			To = DateTime.Now.AddDays(1),
			Status = 0,
			Unlimited = 1,
			AmountUsed = 0,
			TotalAmountLimit = 100,
			CampaignDetail = @"{""BonusAmount"":10,""TurnoverMultiple"":20}"
		};
		_ = fakePromotionRepository
			.GetPromotionWithCacheAsync(Arg.Any<Guid>())
			.Returns(promotion);

		_ = fakePromotionRepository
			.IncreaseToTotalAmountAsync(Arg.Any<Guid>(), Arg.Any<long>())
			.Returns(100);

		_ = fakePromotionJoinLockRepository
			.SetWhenNotExistsAsync(Arg.Any<string>(), Arg.Any<Guid>())
			.Returns(true);

		var promotionDistributePlanInfo =
			new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0);
		_ = fakePromotionServiceClient
			.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>())
			.Returns(promotionDistributePlanInfo);

		_ = fakePromotionServiceClient
			.CreatePromotionDistributeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>(),
				Arg.Any<decimal>(), Arg.Any<string>())
			.Returns(Guid.NewGuid());

		_ = fakeDistributeRegisterEventRepository
			.CreateEventAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<decimal>())
			.Returns(Guid.NewGuid());

		_ = fakePlayerBucketRepository
			.CreatePlayerBucketAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<BucketType>())
			.Returns(ValueTask.CompletedTask);

		var playerInfo = new PlayerInfo { PlayerCode = playerCode, RegisteredOn = DateTimeOffset.Now.Date };
		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(Arg.Any<string>())
			.Returns(playerInfo);

		var sut = new SendRegisterPromotionCommandService(fakePromotionServiceClient, fakePlayerServiceClient,
			fakePromotionRepository, fakePromotionJoinLockRepository, fakePromotionDistributionRepository,
			fakeDistributeRegisterEventRepository, fakePlayerBucketRepository,
			NullLogger<SendRegisterPromotionCommandService>.Instance);

		await sut.ExecuteAsync(new(promotionId, playerCode));

		_ = fakePlayerBucketRepository
			.Received(1)
			.CreatePlayerBucketAsync(Arg.Is(playerCode), Arg.Is(200M), Arg.Is(BucketType.Promotion));
	}

	[TestMethod]
	public async Task Can_Not_Send_Register_Bonus_When_Already_Sent()
	{
		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		var fakeDistributeRegisterEventRepository = Substitute.For<IDistributeRegisterEventRepository>();
		var fakePlayerBucketRepository = Substitute.For<IPlayerBucketRepository>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionJoinLockRepository = Substitute.For<IPromotionJoinLockRepository>();
		var fakePromotionDistributionRepository = Substitute.For<IPromotionDistributionRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		_ = fakePromotionDistributionRepository
			.PromotionDistributionExistsAsync(Arg.Any<Guid>(), Arg.Any<string>())
			.Returns(false);

		var sut = new SendRegisterPromotionCommandService(fakePromotionServiceClient, fakePlayerServiceClient,
			fakePromotionRepository, fakePromotionJoinLockRepository, fakePromotionDistributionRepository,
			fakeDistributeRegisterEventRepository, fakePlayerBucketRepository,
			NullLogger<SendRegisterPromotionCommandService>.Instance);

		await sut.ExecuteAsync(new(promotionId, playerCode));

		_ = fakePromotionDistributionRepository
			.Received(0)
			.PromotionDistributionExistsAsync(Arg.Any<Guid>(), Arg.Any<string>());
	}
}