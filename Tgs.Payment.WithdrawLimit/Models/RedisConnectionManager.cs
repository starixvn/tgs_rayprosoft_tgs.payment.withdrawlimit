using StackExchange.Redis;

namespace Tgs.Payment.WithdrawLimit.Models;

public class RedisConnectionManager : IDisposable, IAsyncDisposable
{
	private readonly ConnectionMultiplexer _connectionMultiplexer;
	private bool _disposedValue;
	public IDatabase Database { get; }

	public RedisConnectionManager(string connectionString)
	{
		var configurationOptions = ConfigurationOptions.Parse(connectionString);
		_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);

		Database = _connectionMultiplexer.GetDatabase();
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await _connectionMultiplexer.DisposeAsync().ConfigureAwait(false);

		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_connectionMultiplexer.Dispose();
			}

			_disposedValue = true;
		}
	}
}
