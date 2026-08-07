using CoachHoopsAI.Domain.GameContext;

namespace CoachHoopsAI.Domain.Tests.GameContext;

public class GameFormatTests
{
    [Theory]
    [InlineData(4, 10, 40)] // 4 x 10
    [InlineData(4, 12, 48)] // 4 x 12
    [InlineData(4, 5, 20)]  // 4 x 5
    [InlineData(8, 4, 32)]  // 8 x 4 (EasyBasket-style)
    public void RegulationDuration_EqualsRegulationPeriodsTimesPeriodMinutes(
        int regulationPeriods, int regulationPeriodMinutes, int expectedTotalMinutes)
    {
        var format = new GameFormat
        {
            RegulationPeriods = regulationPeriods,
            RegulationPeriodMinutes = regulationPeriodMinutes,
            OvertimePeriodMinutes = 5
        };

        Assert.Equal(TimeSpan.FromMinutes(expectedTotalMinutes), format.RegulationDuration);
    }

    [Fact]
    public void Name_IsDescriptiveOnly_DoesNotAffectRegulationDuration()
    {
        var withName = new GameFormat { RegulationPeriods = 4, RegulationPeriodMinutes = 10, OvertimePeriodMinutes = 5, Name = "FIBA" };
        var withoutName = new GameFormat { RegulationPeriods = 4, RegulationPeriodMinutes = 10, OvertimePeriodMinutes = 5 };

        Assert.Equal(withoutName.RegulationDuration, withName.RegulationDuration);
    }
}
