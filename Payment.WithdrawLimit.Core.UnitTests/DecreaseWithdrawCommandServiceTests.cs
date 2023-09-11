using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tgs.Payment.WithdrawLimit;
using Tgs.Payment.WithdrawLimit.ApplicationServices;

namespace Payment.WithdrawLimit.Core.UnitTests;

public class DecreaseWithdrawCommandServiceTests
{
    [Fact]
    public async Task 處理存款累計取消()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();

        var sut = new DecreaseWithdrawCommandService(fakeWithdrawLimitRepository);

        var command = new DecreaseWithdrawCommand("test1", DateOnly.Parse("2023-9-8"), 1000M);

        // Act
        _ = await sut.GetAsync(command);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawCountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Is(command.Date));
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Is(command.Date),
                Arg.Is(command.Amount));
    }

    [Fact]
    public void 處理存款累計取消_取消計數發生錯誤不會進行下去()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();

        var sut = new DecreaseWithdrawCommandService(fakeWithdrawLimitRepository);

        var command = new DecreaseWithdrawCommand("test1", DateOnly.Parse("2023-9-8"), 1000M);

        _ = fakeWithdrawLimitRepository.DecreaseWithdrawCountAsync(
            Arg.Is(command.PlayerCode),
            Arg.Is(command.Date))
            .Throws<Exception>();

        // Act
        Assert.ThrowsAsync<Exception>(async () => await sut.GetAsync(command));

        // Assert
        _ = fakeWithdrawLimitRepository.Received(0)
            .DecreaseWithdrawTotalAmountAsync(
                Arg.Any<string>(),
                Arg.Any<DateOnly>(),
                Arg.Any<decimal>());
    }

    [Fact]
    public void 處理存款累計取消_取消累計金額發生錯誤會復原累計次數()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();

        var sut = new DecreaseWithdrawCommandService(fakeWithdrawLimitRepository);

        var command = new DecreaseWithdrawCommand("test1", DateOnly.Parse("2023-9-8"), 1000M);

        fakeWithdrawLimitRepository.DecreaseWithdrawTotalAmountAsync(
            Arg.Is(command.PlayerCode),
            Arg.Is(command.Date),
            Arg.Is(command.Amount))
            .Throws<Exception>();

        // Act
        _ = Assert.ThrowsAsync<Exception>(async () => await sut.GetAsync(command));

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .IncreaseWithdrawCountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Is(command.Date));
    }
}
