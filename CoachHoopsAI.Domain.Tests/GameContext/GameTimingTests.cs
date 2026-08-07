using CoachHoopsAI.Domain.GameContext;

namespace CoachHoopsAI.Domain.Tests.GameContext;

public class GameTimingTests
{
    private static readonly GameFormat FourByTen = new()
    {
        RegulationPeriods = 4,
        RegulationPeriodMinutes = 10,
        OvertimePeriodMinutes = 5
    };

    private static readonly GameFormat EightByFour = new()
    {
        RegulationPeriods = 8,
        RegulationPeriodMinutes = 4,
        OvertimePeriodMinutes = 4
    };

    public static IEnumerable<object[]> RegulationScenarios => new List<object[]>
    {
        // period, clockRemainingMinutes, expectedElapsedMinutes, expectedProgress
        new object[] { 1, 10, 0, 0.0 },     // start of game
        new object[] { 1, 5, 5, 0.125 },    // mid-Q1
        new object[] { 2, 0, 20, 0.50 },    // halftime
        new object[] { 3, 5, 25, 0.625 },   // mid-Q3
        new object[] { 4, 0, 40, 1.00 },    // end of regulation
    };

    [Theory]
    [MemberData(nameof(RegulationScenarios))]
    public void ElapsedGameTimeAndRegulationProgress_DuringRegulation_4x10(
        int currentPeriod, int clockRemainingMinutes, int expectedElapsedMinutes, double expectedProgress)
    {
        var timing = new GameTiming { CurrentPeriod = currentPeriod, ClockRemaining = TimeSpan.FromMinutes(clockRemainingMinutes) };

        Assert.False(timing.IsOvertime(FourByTen));
        Assert.Equal(0, timing.OvertimeNumber(FourByTen));
        Assert.Equal(TimeSpan.FromMinutes(expectedElapsedMinutes), timing.ElapsedGameTime(FourByTen));
        Assert.Equal(expectedProgress, timing.RegulationProgress(FourByTen), precision: 10);
    }

    [Fact]
    public void OT1Start_4x10_IsOvertimeWithElapsedEqualToRegulationDuration()
    {
        // Period 5 = OT1 for a 4-period format. Full 5:00 remaining = OT1 just starting.
        var timing = new GameTiming { CurrentPeriod = 5, ClockRemaining = TimeSpan.FromMinutes(5) };

        Assert.True(timing.IsOvertime(FourByTen));
        Assert.Equal(1, timing.OvertimeNumber(FourByTen));
        Assert.Equal(TimeSpan.FromMinutes(40), timing.ElapsedGameTime(FourByTen));
        Assert.Equal(1.00, timing.RegulationProgress(FourByTen), precision: 10);
    }

    [Fact]
    public void OT2Underway_4x10_ElapsedIncludesRegulationPlusCompletedAndCurrentOvertime()
    {
        // Period 6 = OT2. 2:00 remaining out of a 5:00 OT period.
        var timing = new GameTiming { CurrentPeriod = 6, ClockRemaining = TimeSpan.FromMinutes(2) };

        Assert.True(timing.IsOvertime(FourByTen));
        Assert.Equal(2, timing.OvertimeNumber(FourByTen));
        Assert.Equal(TimeSpan.FromMinutes(48), timing.ElapsedGameTime(FourByTen));
        Assert.Equal(1.00, timing.RegulationProgress(FourByTen), precision: 10);
    }

    [Fact]
    public void EasyBasketStyleFormat_8x4_ElapsedGameTimeIsCorrect()
    {
        // 8 x 4: period 6, 3:00 remaining -> 5 completed 4-minute periods + 1 minute
        // elapsed in period 6 = 21:00. Regulation (not overtime), since 6 <= 8.
        var timing = new GameTiming { CurrentPeriod = 6, ClockRemaining = TimeSpan.FromMinutes(3) };

        Assert.False(timing.IsOvertime(EightByFour));
        Assert.Equal(TimeSpan.FromMinutes(21), timing.ElapsedGameTime(EightByFour));
    }

    [Fact]
    public void RegulationProgress_NeverExceedsOneEvenIfClockGoesNegativeDueToBadData()
    {
        // Defensive clamp: if elapsed somehow exceeds regulation duration, progress
        // must still be reported within [0, 1].
        var timing = new GameTiming { CurrentPeriod = 4, ClockRemaining = TimeSpan.FromMinutes(-5) };

        var progress = timing.RegulationProgress(FourByTen);

        Assert.InRange(progress, 0.0, 1.0);
    }
}
