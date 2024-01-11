using Google.Protobuf;
using Tgs.Payment.WithdrawLimit.ApplicationServices;
using Tgs.Payment.WithdrawLimit.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.MessageQueue;

internal class IncreaseWithdrawProcessor(IGetService<IncreaseWithdrawCommand, WithdrawStatistics> increaseService)
	: IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = WithdrawRequest.Parser.ParseFrom(data.Span);

		var command = new IncreaseWithdrawCommand(
			request.PlayerCode,
			request.Date.ToDateOnly()!.Value,
			request.Amount);

		var result = await increaseService.GetAsync(
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
