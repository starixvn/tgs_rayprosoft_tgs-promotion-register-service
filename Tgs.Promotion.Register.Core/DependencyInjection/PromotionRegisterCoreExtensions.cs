using System.Reflection;
using Tgs.Promotion;
using Tgs.Promotion.Register.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class PromotionRegisterCoreExtensions
{
	public static IServiceCollection AddPromotionRegisterCore(this IServiceCollection services)
	{
		_ = services.RegisterAllServiceUtilityTypes(
				Assembly.GetExecutingAssembly(),
				ServiceLifetime.Transient)
			.AddHostedService<UpdateTotalAmountWorker>()
			.AddSingleton<PromotionMemoryCache>();

		return services;
	}
}
