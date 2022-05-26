using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.MessageQueue;

namespace Tgs.Promotion.Register.Core;

internal class UpdateTotalAmountWorker : IHostedService, IDisposable
{
	private int executionCount = 0;
	private readonly ILogger<UpdateTotalAmountWorker> _logger;
	private Timer _timer = null!;
	private readonly IServiceProvider _serviceProvider;

	public UpdateTotalAmountWorker(
		IServiceProvider serviceProvider,
		ILogger<UpdateTotalAmountWorker> logger)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public Task StartAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timed Hosted Service running.");

		_timer = new Timer(DoWork, null, TimeSpan.Zero,
			TimeSpan.FromSeconds(5));

		return Task.CompletedTask;
	}

	private void DoWork(object? state)
	{
		using var scope = _serviceProvider.CreateScope();

		var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
		var promotionRepository = scope.ServiceProvider.GetRequiredService<IPromotionRepository>();

		var list = promotionRepository.GetAllTotalAmountRecordForUpdateAsync();

		var totalAmounts = list.GroupBy(x => new { x.PromotionId }, (group, items) =>
		{
			var snapshotId = items.Max(x => x.SnapshotId);
			var amount = items.First(x => x.SnapshotId == snapshotId);
			return new
			{
				group.PromotionId,
				SnapshotId = snapshotId,
				amount.TotalAmount
			};
		});

		foreach (var totalAmount in totalAmounts)
		{
			messageSender.PublishAsync("tgs.promotion.totalusedamount",
				Encoding.UTF8.GetBytes(JsonSerializer.Serialize(totalAmount))).ConfigureAwait(false);
		}

		promotionRepository.RemoveAllTotalAmountRecordForUpdateAsync();

		_logger.LogInformation(
			"Timed Hosted Service is working. Count: {list}", list);
	}

	public Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Timed Hosted Service is stopping.");

		_timer?.Change(Timeout.Infinite, 0);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}
}
