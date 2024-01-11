using Tgs.ServiceUtility;

namespace Tgs.Payment.WithdrawLimit.ApplicationServices;

internal class DecreaseWithdrawCommandService(IWithdrawLimitRepository withdrawLimitRepository) : IGetService<DecreaseWithdrawCommand, WithdrawStatistics>
{
	private readonly IWithdrawLimitRepository _withdrawLimitRepository = withdrawLimitRepository ?? throw new ArgumentNullException(nameof(withdrawLimitRepository));

	public async ValueTask<WithdrawStatistics> GetAsync(DecreaseWithdrawCommand command, CancellationToken cancellationToken = default)
	{
		var count = await _withdrawLimitRepository.DecreaseWithdrawCountAsync(
			command.PlayerCode,
			command.Date,
			cancellationToken).ConfigureAwait(false);

		try
		{
			var amount = await _withdrawLimitRepository.DecreaseWithdrawTotalAmountAsync(
				command.PlayerCode,
				command.Date,
				command.Amount,
				cancellationToken).ConfigureAwait(false);

			return new WithdrawStatistics(amount, count);
		}
		catch (Exception)
		{
			_ = await _withdrawLimitRepository.IncreaseWithdrawCountAsync(
				command.PlayerCode,
				command.Date,
				default).ConfigureAwait(true);
			throw;
		}
	}
}
