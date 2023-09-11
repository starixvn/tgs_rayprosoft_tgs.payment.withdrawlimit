using Tgs.ServiceUtility;

namespace Tgs.Payment.WithdrawLimit.ApplicationServices;

internal class IncreaseWithdrawCommandService : IGetService<IncreaseWithdrawCommand, WithdrawStatistics>
{
	private readonly IWithdrawLimitRepository _withdrawLimitRepository;

	public IncreaseWithdrawCommandService(IWithdrawLimitRepository withdrawLimitRepository)
	{
		_withdrawLimitRepository = withdrawLimitRepository ?? throw new ArgumentNullException(nameof(withdrawLimitRepository));
	}

	public async ValueTask<WithdrawStatistics> GetAsync(IncreaseWithdrawCommand command, CancellationToken cancellationToken = default)
	{
		var count = await _withdrawLimitRepository.IncreaseWithdrawCountAsync(
			command.PlayerCode,
			command.Date,
			cancellationToken).ConfigureAwait(false);
		try
		{
			var amount = await _withdrawLimitRepository.IncreaseWithdrawTotalAmountAsync(
				command.PlayerCode,
				command.Date,
				command.Amount,
				cancellationToken).ConfigureAwait(false);

			return new WithdrawStatistics(amount, count);
		}
		catch (Exception)
		{
			await _withdrawLimitRepository.DecreaseWithdrawCountAsync(
				command.PlayerCode,
				command.Date,
				default).ConfigureAwait(false);
			throw;
		}
	}
}
