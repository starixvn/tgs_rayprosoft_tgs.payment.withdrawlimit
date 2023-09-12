using Tgs.Payment.WithdrawLimit.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddWithdrawLimitClient(this IServiceCollection services)
		=> services
			.AddTransient<IWithdrawLimitClient, WithdrawLimitClient>();
}
