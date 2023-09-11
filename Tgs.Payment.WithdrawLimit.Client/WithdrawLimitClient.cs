using Google.Protobuf;
using Tgs.Payment.WithdrawLimit.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.Clients;

internal class WithdrawLimitClient
{
	private readonly IMessageSender _messageSender;

	public WithdrawLimitClient(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<WithdrawStatistics> IncreasePlayerWithdrawStatisticsAsync(
		string playerCode,
		DateOnly date,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var request = new WithdrawRequest
		{
			PlayerCode = playerCode,
			Date = date,
			Amount = amount
		};

		var responseData = await _messageSender.RequestAsync(
			"tgs.payment.withdraw-limit.increase",
			request.ToByteArray(),
			cancellationToken).ConfigureAwait(false);

		var response = WithdrawResponse.Parser.ParseFrom(responseData.Span);

		return new WithdrawStatistics(response.Amount, response.Count);
	}

	public async ValueTask<WithdrawStatistics> DecreasePlayerWithdrawStatisticsAsync(
		string playerCode,
		DateOnly date,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var request = new WithdrawRequest
		{
			PlayerCode = playerCode,
			Date = date,
			Amount = amount
		};

		var responseData = await _messageSender.RequestAsync(
			"tgs.payment.withdraw-limit.decrease",
			request.ToByteArray(),
			cancellationToken).ConfigureAwait(false);

		var response = WithdrawResponse.Parser.ParseFrom(responseData.Span);

		return new WithdrawStatistics(response.Amount, response.Count);
	}
}
