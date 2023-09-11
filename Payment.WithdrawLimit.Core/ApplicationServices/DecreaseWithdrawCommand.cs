namespace Tgs.Payment.WithdrawLimit.ApplicationServices;
public record DecreaseWithdrawCommand(string PlayerCode, DateOnly Date, decimal Amount);
