using ExpectedObjects;
using Google.Protobuf;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.WithdrawLimit;
using Tgs.Payment.WithdrawLimit.MessageQueue;
using Valhalla.MessageQueue;

namespace Payment.WithdrawLimit.Core.UnitTests;

public class WithdrawFlowHandlerTests
{
    [Fact]
    public async Task 當提款單已支付時會結算金額()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid()
            });

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                ActualAmount = 900M,
                Status = PaymentFlowState.Paid,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
                Arg.Is(100M));
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawCountAsync(Arg.Is("test1"), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task 當提款單已支付時ActualAmount為空_不需要補差額()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid()
            });

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                ActualAmount = null,
                Status = PaymentFlowState.Paid,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Any<DateOnly>(),
                Arg.Any<decimal>());
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawCountAsync(Arg.Is("test1"), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task 當提款單已支付時ActualAmount和Amount相等_不需要補差額()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid()
            });

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                ActualAmount = 1000M,
                Status = PaymentFlowState.Paid,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Any<DateOnly>(),
                Arg.Any<decimal>());
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawCountAsync(Arg.Is("test1"), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task 當提款單已支付時實際出款金額大於申請金額_不會補差額()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid()
            });

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                ActualAmount = 1100M,
                Status = PaymentFlowState.Paid,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime)),
                Arg.Any<decimal>());
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawCountAsync(Arg.Is("test1"), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task 當提款單在待審核被拒絕時_會復原統計()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.WaitApprove.ToString()
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid()
            });

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                Status = PaymentFlowState.Reject,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        var expectedData = new
        {
            Amount = 1000M,
            Date = DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime),
            PlayerCode = "test1"
        };

        _ = fakeMessageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
            Arg.Is("tgs.payment.withdraw-limit.decrease"),
            Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<ReadOnlyMemory<byte>>();

                var request = Tgs.Payment.WithdrawLimit.Protos.WithdrawRequest.Parser.ParseFrom(data.Span);

                return expectedData.ToExpectedObject().Matches(new
                {
                    Amount = request.Amount.ToDecimal()!.Value,
                    Date = request.Date.ToDateOnly(),
                    request.PlayerCode
                })
                    ? (ReadOnlyMemory<byte>)new Tgs.Payment.WithdrawLimit.Protos.WithdrawResponse
                    {
                        Amount = 1000M,
                        Count = 1
                    }.ToByteArray()
                    : throw new Exception();
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeMessageSender.Received(1)
            .RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
                Arg.Is("tgs.payment.withdraw-limit.decrease"),
                Arg.Any<ReadOnlyMemory<byte>>());
    }

    [Fact]
    public async Task 當提款單在等待出款時被拒絕_會復原統計()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid()
            });

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                Status = PaymentFlowState.Reject,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        var expectedData = new
        {
            Amount = 1000M,
            Date = DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromHours(8D)).DateTime),
            PlayerCode = "test1"
        };

        _ = fakeMessageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
            Arg.Is("tgs.payment.withdraw-limit.decrease"),
            Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<ReadOnlyMemory<byte>>();

                var request = Tgs.Payment.WithdrawLimit.Protos.WithdrawRequest.Parser.ParseFrom(data.Span);

                return expectedData.ToExpectedObject().Matches(new
                {
                    Amount = request.Amount.ToDecimal()!.Value,
                    Date = request.Date.ToDateOnly(),
                    request.PlayerCode
                })
                    ? (ReadOnlyMemory<byte>)new Tgs.Payment.WithdrawLimit.Protos.WithdrawResponse
                    {
                        Amount = 1000M,
                        Count = 1
                    }.ToByteArray()
                    : throw new Exception();
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeMessageSender.Received(1)
            .RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
                Arg.Is("tgs.payment.withdraw-limit.decrease"),
                Arg.Any<ReadOnlyMemory<byte>>());
    }

    [Fact]
    public async Task 只會處理玩家的提款單_其他的不會處理()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        var createdOn = DateTimeOffset.UtcNow;

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                Status = PaymentFlowState.Reject,
                UserType = UserType.Agent,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeMessageSender.Received(0)
            .SendAsync(
                Arg.Is("tgs.payment.withdraw-limit.decrease"),
                Arg.Any<ReadOnlyMemory<byte>>());
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Any<string>(),
                Arg.Any<DateOnly>(),
                Arg.Any<decimal>());
    }

    [Fact]
    public async Task 提款單處理時_會依照Brand的時區設定來將CreatedOn轉換成該時區日期來計算當日總額()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawFlowHandler(
            fakePaymentServiceClient,
            fakeWithdrawLimitRepository,
            fakeBrandServiceClient,
            fakeMessageSender);

        var request = new FormEventEx
        {
            BrandId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Status = PaymentFlowState.Approved.ToString()
        };

        var timeOffset = 5.5D * 60;
        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(request.BrandId.ToGuid()))
            .Returns(new BrandInfo
            {
                Id = request.BrandId.ToGuid(),
                TimeSpanMinutes = (int)timeOffset
            });

        var createdOn = DateTimeOffset.Parse("2022/9/13+8");

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new WithdrawFormInfo
            {
                Id = request.FormId.ToGuid(),
                BrandId = request.BrandId.ToGuid(),
                Amount = 1000M,
                ActualAmount = 900M,
                Status = PaymentFlowState.Paid,
                UserType = UserType.Player,
                UserId = "test1",
                CreatedOn = createdOn.UtcDateTime
            });

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is("test1"),
                Arg.Is(DateOnly.FromDateTime(createdOn.ToOffset(TimeSpan.FromMinutes(timeOffset)).DateTime)),
                Arg.Is(100M));
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawCountAsync(Arg.Is("test1"), Arg.Any<DateOnly>());
    }
}
