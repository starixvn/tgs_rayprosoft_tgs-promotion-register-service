using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.Register.Core.Application;
using Tgs.Promotion.Register.Core.Models;
using Tgs.Promotion.Register.Service.MessageHandlers;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Service.MessageReply;

public class RegisterMessageProcessor : IMessageProcessor
{
	private readonly ICommandService<SendRegisterPromotionCommand> _commandService;
	private readonly ILogger<RegisterMessageProcessor> _logger;

	public RegisterMessageProcessor(ILogger<RegisterMessageProcessor> logger,
		ICommandService<SendRegisterPromotionCommand> commandService)
	{
		_commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken)
	{
		var json = Encoding.UTF8.GetString(data.Span);
		var request = JsonSerializer.Deserialize<RegisterMessageData>(json)!;

		_logger.LogInformation($"register distribution request: {request}");

		if (request is null)
		{
			throw new ArgumentNullException(nameof(request));
		}

		var command =
			new SendRegisterPromotionCommand(request.PromotionId, request.PlayerCode);

		await  _commandService.ExecuteAsync(command, cancellationToken);

		var response = new RegisterMessageDataResponse(request.PromotionId,
			request.PlayerCode);

		_logger.LogInformation($"register distribution response: {response}");

		return (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));
	}
}