using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.Register.Core;

internal class UpdateTotalAmountWorker : IHostedService, IDisposable
{
	private readonly ILogger<UpdateTotalAmountWorker> _logger;
	private readonly IServiceProvider _serviceProvider;
	private Timer _timer = null!;

	public UpdateTotalAmountWorker(
		IServiceProvider serviceProvider,
		ILogger<UpdateTotalAmountWorker> logger)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public void Dispose() => _timer?.Dispose();

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timed Hosted Service running.");

		_timer = new(DoWork, null, TimeSpan.Zero,
			TimeSpan.FromSeconds(30));

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timed Hosted Service is stopping.");

		_ = (_timer?.Change(Timeout.Infinite, 0));

		return Task.CompletedTask;
	}

	private void DoWork(object? state)
	{
		using var scope = _serviceProvider.CreateScope();

		var promotionRepository = scope.ServiceProvider.GetRequiredService<IPromotionRepository>();
		var promotionServiceClient = scope.ServiceProvider.GetRequiredService<IPromotionServiceClient>();

		var list = promotionRepository.GetAllTotalAmountRecordForUpdate();

		_logger.LogInformation(
			"total amount record: {list}", list);

		var totalAmounts = list.GroupBy(x => new { x!.PromotionId }, (group, items) =>
		{
			var snapshotId = items.Max(x => x!.SnapshotId);
			var amount = items.First(x => x!.SnapshotId == snapshotId);
			return new
			{
				group.PromotionId,
				SnapshotId = snapshotId,
				amount!.TotalAmount
			};
		});

		foreach (var totalAmount in totalAmounts)
			_ = Task.Run(async () =>
				{
					_logger.LogInformation("total amount record: {JsonSerializer.Serialize(totalAmount)}", JsonSerializer.Serialize(totalAmount));

					await promotionServiceClient.UpdatePromotionUsedAmountAsync(
						totalAmount.PromotionId,
						totalAmount.TotalAmount,
						totalAmount.SnapshotId).ConfigureAwait(false);
				}
			);

		_ = Task.Run(async () => await promotionRepository.RemoveAllTotalAmountRecordForUpdateAsync().ConfigureAwait(false));

		_logger.LogInformation(
			"Timed Hosted Service is working. Count: {list}", list);
	}
}
