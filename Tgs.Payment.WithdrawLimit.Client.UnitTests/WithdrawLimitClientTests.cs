using Google.Protobuf;
using NSubstitute;
using Tgs.Payment.WithdrawLimit.Clients;
using Tgs.Payment.WithdrawLimit.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.WithdrawLimit.Client.UnitTests;

public class WithdrawLimitClientTests
{
    [Fact]
    public async Task 累計存款金額及次數()
    {
        // Arrange
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawLimitClient(fakeMessageSender);

        var expectedResponse = new WithdrawResponse
        {
            Amount = 1234M,
            Count = 5
        };

        _ = fakeMessageSender.RequestAsync(
            Arg.Is("tgs.payment.withdraw-limit.increase"),
            Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(expectedResponse.ToByteArray());

        // Act
        var actual = await sut.IncreasePlayerWithdrawStatisticsAsync(
            "test1",
            DateOnly.FromDateTime(DateTime.Now),
            1000M);

        // Assert
        _ = fakeMessageSender.Received(1)
            .RequestAsync(
                Arg.Is("tgs.payment.withdraw-limit.increase"),
                Arg.Any<ReadOnlyMemory<byte>>());
        Assert.Equal(expectedResponse.Amount.ToDecimal()!.Value, actual.TotalAmount);
        Assert.Equal(expectedResponse.Count, actual.Count);
    }

    [Fact]
    public async Task 取消累計存款金額及次數()
    {
        // Arrange
        var fakeMessageSender = Substitute.For<IMessageSender>();

        var sut = new WithdrawLimitClient(fakeMessageSender);

        var expectedResponse = new WithdrawResponse
        {
            Amount = 1234M,
            Count = 5
        };

        _ = fakeMessageSender.RequestAsync(
            Arg.Is("tgs.payment.withdraw-limit.decrease"),
            Arg.Any<ReadOnlyMemory<byte>>())
            .Returns(expectedResponse.ToByteArray());

        // Act
        var actual = await sut.DecreasePlayerWithdrawStatisticsAsync(
            "test1",
            DateOnly.FromDateTime(DateTime.Now),
            1000M);

        // Assert
        _ = fakeMessageSender.Received(1)
            .RequestAsync(
                Arg.Is("tgs.payment.withdraw-limit.decrease"),
                Arg.Any<ReadOnlyMemory<byte>>());
        Assert.Equal(expectedResponse.Amount.ToDecimal()!.Value, actual.TotalAmount);
        Assert.Equal(expectedResponse.Count, actual.Count);
    }
}
