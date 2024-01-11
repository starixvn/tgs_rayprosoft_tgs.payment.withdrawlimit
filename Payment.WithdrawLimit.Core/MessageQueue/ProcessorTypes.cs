namespace Tgs.Payment.WithdrawLimit.MessageQueue;

public static class ProcessorTypes
{
	public static readonly Type IncreaseWithdrawProcessorType = typeof(IncreaseWithdrawProcessor);
	public static readonly Type DecreaseWithdrawProcessorType = typeof(DecreaseWithdrawProcessor);
	public static readonly Type WithdrawFlowHandlerType = typeof(WithdrawFlowHandler);
}
