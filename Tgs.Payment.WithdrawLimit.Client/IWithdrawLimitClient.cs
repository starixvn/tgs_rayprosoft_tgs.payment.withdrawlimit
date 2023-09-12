namespace Tgs.Payment.WithdrawLimit.Clients;

public interface IWithdrawLimitClient
{
	ValueTask<WithdrawStatistics> DecreasePlayerWithdrawStatisticsAsync(string playerCode, DateOnly date, decimal amount, CancellationToken cancellationToken = default);
	ValueTask<WithdrawStatistics> IncreasePlayerWithdrawStatisticsAsync(string playerCode, DateOnly date, decimal amount, CancellationToken cancellationToken = default);
}