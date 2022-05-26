using Tgs.Promotion.Register.Core;
using Tgs.Promotion.Register.Repository.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.Register.Repository;

internal class DistributeRegisterEventRepository : IDistributeRegisterEventRepository
{
    private readonly TgsContext _tgsContext;
    private readonly ISerialNumberGenerator _serialNumberGenerator;

	public DistributeRegisterEventRepository(TgsContext tgsContext, ISerialNumberGenerator serialNumberGenerator)
    {
        _tgsContext = tgsContext;
        _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
    }

	public async ValueTask<Guid> CreateEventAsync(Guid brandId, Guid promotionId, string playerCode, decimal bonusAmount)
    {
	    var id = await _serialNumberGenerator
		    .GenerateAsync()
		    .ConfigureAwait(false);

		var distributeRegisterEvent = new DistributeRegisterEvent
		{
			Id = id,
			BrandId = brandId,
			PromotionId = promotionId,
			PlayerCode = playerCode,
			Bonus = bonusAmount
		};

		_ = await _tgsContext.DistributeRegisterEvents.AddAsync(distributeRegisterEvent).ConfigureAwait(false);
		_ = await _tgsContext.SaveChangesAsync().ConfigureAwait(false);

		return distributeRegisterEvent.Id;
    }
}