using System.Text.Json;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.Register.Core.Application;
using Tgs.Promotion.Register.Core.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Service.MessageReply;

public class RegisterRuleReplyProcessor : IMessageProcessor
{
	private readonly ICommandService<RemovePromotionCacheCommand> _commandService;
	private readonly ILogger<RegisterRuleReplyProcessor> _logger;

	public RegisterRuleReplyProcessor(ICommandService<RemovePromotionCacheCommand> commandService,
		ILogger<RegisterRuleReplyProcessor> logger)
	{
		_commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<RegisterSetting>(request.Settings,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		_logger.LogInformation($"register setting : {settings}");

		if (settings is null)
		{
			throw new ArgumentNullException(nameof(settings));
		}

		var response = new CampaignDetailResponse { CampaignDetail = JsonSerializer.Serialize(settings) };

		await _commandService.ExecuteAsync(new(request.PromotionId), cancellationToken);

		return (ReadOnlyMemory<byte>)response.ToByteArray();
	}
}