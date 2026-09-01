using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;
public sealed class SupportV6193Tests
{
    [Theory]
    [InlineData("Low",72)] [InlineData("Medium",48)] [InlineData("High",24)] [InlineData("Critical",8)]
    public void PriorityDefinesBusinessHourSla(string priority,int hours)=>Assert.Equal(hours,SupportSla.Hours(priority));

    [Fact]
    public void SlaSkipsWeekend()
    {
        var friday=new DateTime(2026,8,28,17,0,0,DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026,8,31,1,0,0,DateTimeKind.Utc),SupportSla.Calculate(friday,"Critical"));
    }

    [Fact]
    public void UnknownPriorityUsesSafeMediumFallback()=>Assert.Equal(48,SupportSla.Hours("invalid"));
}
