using Microsoft.Extensions.Caching.Memory;

namespace Tgs.Promotion.Register.Core;

public class PromotionMemoryCache
{
	public MemoryCache Cache { get; } = new(
		new MemoryCacheOptions { SizeLimit = 1024 });
}