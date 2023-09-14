using Google.Protobuf;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.MessageQueue;

internal class WithdrawFlowHandler : IMessageHandler
{
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IWithdrawLimitRepository _withdrawLimitRepository;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly IMessageSender _messageSender;

	public WithdrawFlowHandler(
		IPaymentServiceClient paymentServiceClient,
		IWithdrawLimitRepository withdrawLimitRepository,
		IBrandServiceClient brandServiceClient,
		IMessageSender messageSender)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_withdrawLimitRepository = withdrawLimitRepository ?? throw new ArgumentNullException(nameof(withdrawLimitRepository));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = FormEventEx.Parser.ParseFrom(data.Span);

		var form = await _paymentServiceClient.GetWithdrawFormAsync(
			request.FormId,
			cancellationToken).ConfigureAwait(false);

		if (form is null)
			return;

		if (form.UserType != UserType.Player)
			return;

		var brandInfo = await _brandServiceClient.GetBrandInfoAsync(
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
				_ = await _withdrawLimitRepository.DecreaseWithdrawTotalAmountAsync(
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

			_ = await _messageSender.RequestAsync(
				"tgs.payment.withdraw-limit.decrease",
				decreaseRequest.ToByteArray(),
				cancellationToken).ConfigureAwait(false);
		}
	}
}
