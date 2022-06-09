using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Register.Core;
using Tgs.Promotion.Register.Core.Enums;
using Tgs.Promotion.Register.Repository.Entities;

namespace Tgs.Promotion.Register.Repository
{
    public class PlayerBucketRepository : IPlayerBucketRepository
    {
	    private readonly TgsContext _tgsContext;

        public PlayerBucketRepository(TgsContext tgsContext)
        {
	        _tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
        }

        public async ValueTask CreatePlayerBucketAsync(string playerCode, decimal bucketAmount, BucketType bucketType)
        {
	        var bucket = new PlayerBucket
	        {
				Id = Guid.NewGuid(),
		        PlayerCode = playerCode,
		        BucketAmount = bucketAmount,
		        BucketType = (short)bucketType,
		        IsFull = false,
		        CreatedOn = DateTime.UtcNow
	        };

	        _ = await _tgsContext.PlayerBuckets.AddAsync(bucket).ConfigureAwait(false);
	        _ = await _tgsContext.SaveChangesAsync().ConfigureAwait(false);
        }
	}
}
