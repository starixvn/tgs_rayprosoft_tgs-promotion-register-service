using Tgs.Promotion.Register.Core.Enums;

namespace Tgs.Promotion.Register.Core;

public interface IPlayerBucketRepository
{
    ValueTask CreatePlayerBucketAsync(string playerCode, decimal bucketAmount, BucketType bucketType);
}