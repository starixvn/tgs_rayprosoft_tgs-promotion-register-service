namespace Tgs.Promotion.Register.Core;

public interface IDistributeRegisterEventRepository
{
    ValueTask<Guid> CreateEventAsync(Guid brandId, Guid promotionId, string playerCode,decimal bonusAmount);
}