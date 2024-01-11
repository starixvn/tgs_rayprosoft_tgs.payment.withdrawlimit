using Google.Protobuf;
using Tgs.Payment.WithdrawLimit.ApplicationServices;
using Tgs.Payment.WithdrawLimit.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.MessageQueue;

internal class DecreaseWithdrawProcessor(IGetService<DecreaseWithdrawCommand, WithdrawStatistics> decreaseService)
	: IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = WithdrawRequest.Parser.ParseFrom(data.Span);

		var command = new DecreaseWithdrawCommand(request.PlayerCode, request.Date.ToDateOnly()!.Value, request.Amount);

		var result = await decreaseService.GetAsync(
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
