using StackExchange.Redis;
using Tgs.Payment.WithdrawLimit;
using Tgs.Payment.WithdrawLimit.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddWithdrawLimitRepositories(
		this IServiceCollection services,
		Func<IServiceProvider, IDatabase> resolveRedisDatabase)
		=> services
			.AddTransient<IWithdrawLimitRepository>(sp => ActivatorUtilities.CreateInstance<RedisWithdrawLimitRepository>(
				sp,
				resolveRedisDatabase(sp)));
}
