namespace Tgs.Promotion.Register.Core.Application;

public record SendRegisterPromotionCommand(Guid PromotionId, string PlayerCode);