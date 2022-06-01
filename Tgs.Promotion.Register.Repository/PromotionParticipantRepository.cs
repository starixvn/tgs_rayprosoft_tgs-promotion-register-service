using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Register.Core;
using Tgs.Promotion.Register.Repository.Entities;

namespace Tgs.Promotion.Register.Repository;

internal class PromotionParticipantRepository : IPromotionParticipantRepository
{
    private readonly TgsContext _tgsContext;

    public PromotionParticipantRepository(TgsContext tgsContext) => _tgsContext = tgsContext;

    public async ValueTask<bool> JoinedPromotionExistsAsync(Guid promotionId, string playerCode)
        => await _tgsContext.PromotionParticipants.AnyAsync(x
            => x.PromotionId == promotionId && x.PlayerCode == playerCode);
}