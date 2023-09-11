namespace Tgs.Payment.WithdrawLimit.MessageQueue;

public static class ProcessorTypes
{
	public readonly static Type IncreaseWithdrawProcessorType = typeof(IncreaseWithdrawProcessor);
	public readonly static Type DecreaseWithdrawProcessorType = typeof(DecreaseWithdrawProcessor);
	public readonly static Type WithdrawFlowHandlerType = typeof(WithdrawFlowHandler);
}
