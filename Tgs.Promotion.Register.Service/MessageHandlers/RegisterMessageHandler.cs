using System.Text;
using System.Text.Json;
using Tgs.MessageQueue;
using Tgs.Promotion.Register.Core.Application;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Register.Service.MessageHandlers;

    public class RegisterMessage : IMessageHandler
    {
        private readonly ICommandService<SendRegisterPromotionCommand> _commandService;
        private readonly ILogger<RegisterMessage> _logger;

        public RegisterMessage(ILogger<RegisterMessage> logger,
            ICommandService<SendRegisterPromotionCommand> commandService)
        {
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            var json = Encoding.UTF8.GetString(data.Span);
            var request = JsonSerializer.Deserialize<RegisterMessageData>(json)!;

            _logger.Log(LogLevel.Information,
                new EventId(10000, nameof(RegisterMessage)),
                json,
                null,
                (state, ex) => json);

            var command =
                new SendRegisterPromotionCommand(request.PromotionId,request.PlayerCode);

            return _commandService.ExecuteAsync(command, cancellationToken);
        }
    }
