using StackExchange.Redis;

namespace Tgs.Payment.WithdrawLimit.Repositories;

internal class RedisWithdrawLimitRepository(IDatabase redisDatabase) : IWithdrawLimitRepository
{
	private static readonly RedisKey _Prefix = new("Tgs:Payment:WithdrawLimit");

	private readonly IDatabase _redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));

	public async ValueTask<int> DecreaseWithdrawCountAsync(string playerCode, DateOnly date, CancellationToken cancellationToken = default)
	{
		var key = _Prefix.Append($":{playerCode}:")
			.Append(date.ToString("yyyyMMdd"))
			.Append(":Count");

		_ = await _redisDatabase.StringSetAsync(
			key,
			0D,
			TimeSpan.FromDays(2D),
			when: When.NotExists).ConfigureAwait(false);

		var result = await _redisDatabase.StringDecrementAsync(
			key).ConfigureAwait(false);

		return (int)result;
	}

	public async ValueTask<decimal> DecreaseWithdrawTotalAmountAsync(
		string playerCode,
		DateOnly date,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var key = _Prefix.Append($":{playerCode}:")
			.Append(date.ToString("yyyyMMdd"))
			.Append(":Amount");

		_ = await _redisDatabase.StringSetAsync(
			key,
			0D,
			TimeSpan.FromDays(2D),
			when: When.NotExists).ConfigureAwait(false);

		var result = await _redisDatabase.StringDecrementAsync(
			key,
			(double)amount).ConfigureAwait(false);

		return (decimal)result;
	}

	public async ValueTask<int> IncreaseWithdrawCountAsync(string playerCode, DateOnly date, CancellationToken cancellationToken = default)
	{
		var key = _Prefix.Append($":{playerCode}:")
			.Append(date.ToString("yyyyMMdd"))
			.Append(":Count");

		_ = await _redisDatabase.StringSetAsync(
			key,
			0D,
			TimeSpan.FromDays(2D),
			when: When.NotExists).ConfigureAwait(false);

		var result = await _redisDatabase.StringIncrementAsync(
			key).ConfigureAwait(false);

		return (int)result;
	}

	public async ValueTask<decimal> IncreaseWithdrawTotalAmountAsync(
		string playerCode,
		DateOnly date,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var key = _Prefix.Append($":{playerCode}:")
			.Append(date.ToString("yyyyMMdd"))
			.Append(":Amount");

		_ = await _redisDatabase.StringSetAsync(
			key,
			0D,
			TimeSpan.FromDays(2D),
			when: When.NotExists).ConfigureAwait(false);

		var result = await _redisDatabase.StringIncrementAsync(
			key,
			(double)amount).ConfigureAwait(false);

		return (decimal)result;
	}
}
