using Google.Protobuf;
using NSubstitute;
using Tgs.Payment.WithdrawLimit;
using Tgs.Payment.WithdrawLimit.ApplicationServices;
using Tgs.Payment.WithdrawLimit.MessageQueue;
using Tgs.Payment.WithdrawLimit.Protos;
using Tgs.ServiceUtility;

namespace Payment.WithdrawLimit.Core.UnitTests;

public class IncreaseWithdrawProcessorTests
{
    [Fact]
    public async Task 處理遞增儲值統計()
    {
        // Arrange
        var fakeIncreaseService = Substitute.For<IGetService<IncreaseWithdrawCommand, WithdrawStatistics>>();

        var sut = new IncreaseWithdrawProcessor(fakeIncreaseService);

        var request = new WithdrawRequest
        {
            Amount = 100M,
            Date = DateOnly.Parse("2023-9-8"),
            PlayerCode = "test1"
        };

        _ = fakeIncreaseService.GetAsync(
            Arg.Is(new IncreaseWithdrawCommand(
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
