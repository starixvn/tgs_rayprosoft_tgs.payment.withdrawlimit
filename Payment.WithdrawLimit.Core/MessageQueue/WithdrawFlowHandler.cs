using Google.Protobuf;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.MessageQueue;

internal class WithdrawFlowHandler(
	IPaymentServiceClient paymentServiceClient,
	IWithdrawLimitRepository withdrawLimitRepository,
	IBrandServiceClient brandServiceClient,
	IMessageSender messageSender)
	: IMessageHandler<ReadOnlyMemory<byte>>
{
	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = FormEventEx.Parser.ParseFrom(data.Span);

		var form = await paymentServiceClient.GetWithdrawFormAsync(
			request.FormId,
			cancellationToken).ConfigureAwait(false);

		if (form is null)
			return;

		if (form.UserType != UserType.Player)
			return;

		var brandInfo = await brandServiceClient.GetBrandInfoAsync(
			form.BrandId,
			cancellationToken).ConfigureAwait(false);

		var brandTimeOffset = TimeSpan.FromMinutes(brandInfo!.TimeSpanMinutes ?? 8 * 60);
		var previousStatus = Enum.Parse<PaymentFlowState>(request.Status);
		var date = DateOnly.FromDateTime(form.CreatedOn.ToOffset(brandTimeOffset).DateTime);
		var playerCode = form.UserId!;

		if (previousStatus == PaymentFlowState.Approved && form.Status == PaymentFlowState.Paid)
		{
			var difference = form.Amount - (form.ActualAmount ?? form.Amount);

			if (difference > 0M)
				_ = await withdrawLimitRepository.DecreaseWithdrawTotalAmountAsync(
					playerCode,
					date,
					difference,
					cancellationToken).ConfigureAwait(false);
		}

		if (previousStatus == PaymentFlowState.WaitApprove && form.Status == PaymentFlowState.Reject
			|| previousStatus == PaymentFlowState.Approved && form.Status == PaymentFlowState.Reject)
		{
			var decreaseRequest = new Protos.WithdrawRequest
			{
				Amount = form.Amount,
				Date = date,
				PlayerCode = playerCode
			};

			_ = await messageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
				"tgs.payment.withdraw-limit.decrease",
				decreaseRequest.ToByteArray(),
				cancellationToken).ConfigureAwait(false);
		}
	}
}
