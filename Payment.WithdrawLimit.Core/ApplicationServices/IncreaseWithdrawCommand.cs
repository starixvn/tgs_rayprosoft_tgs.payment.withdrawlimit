namespace Tgs.Payment.WithdrawLimit.ApplicationServices;
public record IncreaseWithdrawCommand(string PlayerCode, DateOnly Date, decimal Amount);
