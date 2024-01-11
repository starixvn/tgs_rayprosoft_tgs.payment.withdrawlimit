using Google.Protobuf;
using NSubstitute;
using Tgs.Payment.WithdrawLimit;
using Tgs.Payment.WithdrawLimit.ApplicationServices;
using Tgs.Payment.WithdrawLimit.MessageQueue;
using Tgs.Payment.WithdrawLimit.Protos;
using Tgs.ServiceUtility;

namespace Payment.WithdrawLimit.Core.UnitTests;

public class DecreaseWithdrawProcessorTests
{
    [Fact]
    public async Task 處理遞減提款金額()
    {
        // Arrange
        var fakeDecreaseService = Substitute.For<IGetService<DecreaseWithdrawCommand, WithdrawStatistics>>();

        var sut = new DecreaseWithdrawProcessor(fakeDecreaseService);

        var request = new WithdrawRequest
        {
            Amount = 1000M,
            Date = DateOnly.Parse("2023-9-8"),
            PlayerCode = "test1"
        };

        _ = fakeDecreaseService.GetAsync(
            Arg.Is(new DecreaseWithdrawCommand(
                request.PlayerCode,
                request.Date.ToDateOnly()!.Value,
                request.Amount)))
            .Returns(new WithdrawStatistics(123M, 3));

        // Act
        var result = await sut.HandleAsync("test", request.ToByteArray(), null);
        var response = WithdrawResponse.Parser.ParseFrom(result.Span);

        // Assert
        Assert.Equal(123M, (decimal)response.Amount);
        Assert.Equal(3, response.Count);
    }
}
