using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.WithdrawLimit.Clients;
using Tgs.Payment.WithdrawLimit.MessageQueue;
using Valhalla.MessageQueue.Configuration;
using Xunit.Abstractions;

namespace Tgs.Payment.WithdrawLimit.IntegrationTests;

public class WithdrawLimitTests(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

    [Fact]
    public async Task 累加提款統計()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        await using var web = WithdrawLimitServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddSingleton(fakeWithdrawLimitRepository)
                    .AddPhoenixToolkitsMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(
                            ProcessorTypes.IncreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.increase")
                        .AddProcessor(
                            ProcessorTypes.DecreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.decrease"))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = ActivatorUtilities.CreateInstance<WithdrawLimitClient>(web.Services);

        var playerCode = "test1";
        var date = DateOnly.FromDateTime(DateTime.Now);
        var amount = 1000M;

        _ = fakeWithdrawLimitRepository.IncreaseWithdrawCountAsync(
            Arg.Is(playerCode),
            Arg.Is(date),
            Arg.Any<CancellationToken>())
            .Returns(3);
        _ = fakeWithdrawLimitRepository.IncreaseWithdrawTotalAmountAsync(
            Arg.Is(playerCode),
            Arg.Is(date),
            Arg.Is(amount),
            Arg.Any<CancellationToken>())
            .Returns(12345M);

        var expected = new WithdrawStatistics(12345M, 3);

        // Act
        var actual = await sut.IncreasePlayerWithdrawStatisticsAsync(playerCode, date, amount);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task 取消累加提款統計()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        await using var web = WithdrawLimitServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddSingleton(fakeWithdrawLimitRepository)
                    .AddPhoenixToolkitsMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(
                            ProcessorTypes.IncreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.increase")
                        .AddProcessor(
                            ProcessorTypes.DecreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.decrease"))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")));

        var sut = ActivatorUtilities.CreateInstance<WithdrawLimitClient>(web.Services);

        var playerCode = "test1";
        var date = DateOnly.FromDateTime(DateTime.Now);
        var amount = 1000M;

        _ = fakeWithdrawLimitRepository.DecreaseWithdrawCountAsync(
            Arg.Is(playerCode),
            Arg.Is(date),
            Arg.Any<CancellationToken>())
            .Returns(3);
        _ = fakeWithdrawLimitRepository.DecreaseWithdrawTotalAmountAsync(
            Arg.Is(playerCode),
            Arg.Is(date),
            Arg.Is(amount),
            Arg.Any<CancellationToken>())
            .Returns(12345M);

        var expected = new WithdrawStatistics(12345M, 3);

        // Act
        var actual = await sut.DecreasePlayerWithdrawStatisticsAsync(playerCode, date, amount);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task 結算提款單()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        await using var web = WithdrawLimitServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeWithdrawLimitRepository)
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakeBrandServiceClient)
                    .AddPaymentClient()
                    .AddPhoenixToolkitsMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(
                            ProcessorTypes.IncreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.increase")
                        .AddProcessor(
                            ProcessorTypes.DecreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.decrease")
                        .AddHandler(
                            ProcessorTypes.WithdrawFlowHandlerType,
                            "tgs.payment.withdraw.status_ex"))
                    .ClearAllExchanges()
                    .AddNoopGlobPatternExchange("tgs.payment.withdraw.status")
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IPaymentClient>();

        var formId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var createdOn = DateTimeOffset.UtcNow;

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
            .Returns(new BrandInfo
            {
                Id = brandId
            });

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Paid,
                CreatedOn = createdOn,
                Amount = 1000M,
                ActualAmount = 900M,
                UserType = UserType.Player,
                UserId = "test1"
            });

        var tcs = new TaskCompletionSource();

        fakeWithdrawLimitRepository.When(fake => fake.DecreaseWithdrawTotalAmountAsync(
            Arg.Is("test1"),
            Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
            Arg.Is(100M),
            Arg.Any<CancellationToken>()))
            .Do(_ => tcs.SetResult());
        // Act
        await sut.EmitWithdrawFormStatusChangedEventAsync(formId, brandId, PaymentFlowState.Approved.ToString());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3D));

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
                Arg.Is(100M),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 存款單拒絕_取消結算()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        await using var web = WithdrawLimitServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureServices(services => services
                    .AddSingleton(fakeWithdrawLimitRepository)
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakeBrandServiceClient)
                    .AddPaymentClient()
                    .AddPhoenixToolkitsMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddProcessor(
                            ProcessorTypes.IncreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.increase")
                        .AddProcessor(
                            ProcessorTypes.DecreaseWithdrawProcessorType,
                            "tgs.payment.withdraw-limit.decrease")
                        .AddHandler(
                            ProcessorTypes.WithdrawFlowHandlerType,
                            "tgs.payment.withdraw.status_ex"))
                    .ClearAllExchanges()
                    .AddNoopGlobPatternExchange("tgs.payment.withdraw.status")
                    .AddDirectGlobPatternExchange("*")));

        var sut = web.Services.GetRequiredService<IPaymentClient>();

        var formId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var createdOn = DateTimeOffset.UtcNow;

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
            .Returns(new BrandInfo
            {
                Id = brandId
            });

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Reject,
                CreatedOn = createdOn,
                Amount = 1000M,
                UserType = UserType.Player,
                UserId = "test1"
            });

        var tcs = new TaskCompletionSource();

        fakeWithdrawLimitRepository.When(fake => fake.DecreaseWithdrawTotalAmountAsync(
            Arg.Is("test1"),
            Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
            Arg.Is(1000M),
            Arg.Any<CancellationToken>()))
            .Do(_ => tcs.SetResult());
        // Act
        await sut.EmitWithdrawFormStatusChangedEventAsync(formId, brandId, PaymentFlowState.Approved.ToString());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3D));

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawCountAsync(
                Arg.Is("test1"),
                Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
                Arg.Any<CancellationToken>());
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
                Arg.Is(1000M),
                Arg.Any<CancellationToken>());
    }
}
