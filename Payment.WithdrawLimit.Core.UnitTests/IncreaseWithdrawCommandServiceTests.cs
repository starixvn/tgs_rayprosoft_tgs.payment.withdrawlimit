using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tgs.Payment.WithdrawLimit;
using Tgs.Payment.WithdrawLimit.ApplicationServices;

namespace Payment.WithdrawLimit.Core.UnitTests;

public class IncreaseWithdrawCommandServiceTests
{
    [Fact]
    public async Task 處理存款累計()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();

        var sut = new IncreaseWithdrawCommandService(fakeWithdrawLimitRepository);

        var command = new IncreaseWithdrawCommand("test1", DateOnly.Parse("2023-9-7"), 1000M);

        // Act
        _ = await sut.GetAsync(command);

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .IncreaseWithdrawCountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Is(command.Date));
        _ = fakeWithdrawLimitRepository.Received(1)
            .IncreaseWithdrawTotalAmountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Is(command.Date),
                Arg.Is(command.Amount));
    }

    [Fact]
    public async Task 處理存款累計_計算次數發生錯誤_不會再繼續執行()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();

        var sut = new IncreaseWithdrawCommandService(fakeWithdrawLimitRepository);

        var command = new IncreaseWithdrawCommand("test1", DateOnly.Parse("2023-9-7"), 1000M);

        _ = fakeWithdrawLimitRepository.IncreaseWithdrawCountAsync(
            Arg.Is(command.PlayerCode),
            Arg.Is(command.Date))
            .Throws<Exception>();

        // Act
        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.GetAsync(command));

        // Assert
        _ = fakeWithdrawLimitRepository.Received(0)
            .IncreaseWithdrawTotalAmountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Any<DateOnly>(),
                Arg.Any<decimal>());
    }

    [Fact]
    public async Task 處理存款累計_計算總額發生錯誤_會復原次數計算()
    {
        // Arrange
        var fakeWithdrawLimitRepository = Substitute.For<IWithdrawLimitRepository>();

        var sut = new IncreaseWithdrawCommandService(fakeWithdrawLimitRepository);

        var command = new IncreaseWithdrawCommand("test1", DateOnly.Parse("2023-9-7"), 1000M);

        _ = fakeWithdrawLimitRepository.IncreaseWithdrawTotalAmountAsync(
            Arg.Is(command.PlayerCode),
            Arg.Is(command.Date),
            Arg.Is(command.Amount))
            .Throws<Exception>();

        // Act
        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.GetAsync(command));

        // Assert
        _ = fakeWithdrawLimitRepository.Received(1)
            .DecreaseWithdrawCountAsync(
                Arg.Is(command.PlayerCode),
                Arg.Is(command.Date));
    }
}
