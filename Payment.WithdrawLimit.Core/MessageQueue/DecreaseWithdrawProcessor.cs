using Google.Protobuf;
using Tgs.Payment.WithdrawLimit.ApplicationServices;
using Tgs.Payment.WithdrawLimit.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.MessageQueue;

internal class DecreaseWithdrawProcessor : IMessageProcessor
{
	private readonly IGetService<DecreaseWithdrawCommand, WithdrawStatistics> _decreaseService;

	public DecreaseWithdrawProcessor(IGetService<DecreaseWithdrawCommand, WithdrawStatistics> decreaseService)
	{
		_decreaseService = decreaseService ?? throw new ArgumentNullException(nameof(decreaseService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = WithdrawRequest.Parser.ParseFrom(data.Span);

		var command = new DecreaseWithdrawCommand(request.PlayerCode, request.Date.ToDateOnly()!.Value, request.Amount);

		var result = await _decreaseService.GetAsync(
			command,
			cancellationToken).ConfigureAwait(false);

		var response = new WithdrawResponse
		{
			Amount = result.TotalAmount,
			Count = result.Count
		};

		return response.ToByteArray();
	}
}
