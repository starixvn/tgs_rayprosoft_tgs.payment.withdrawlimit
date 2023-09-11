namespace Tgs.Payment.WithdrawLimit;

public interface IWithdrawLimitRepository
{
	ValueTask<decimal> IncreaseWithdrawTotalAmountAsync(
		string playerCode,
		DateOnly date,
		decimal amount,
		CancellationToken cancellationToken = default);

	ValueTask<decimal> DecreaseWithdrawTotalAmountAsync(
		string playerCode,
		DateOnly date,
		decimal amount,
		CancellationToken cancellationToken = default);

	ValueTask<int> IncreaseWithdrawCountAsync(string playerCode, DateOnly date, CancellationToken cancellationToken = default);

	ValueTask<int> DecreaseWithdrawCountAsync(string playerCode, DateOnly date, CancellationToken cancellationToken = default);
}
