#pragma warning disable IDE1006
namespace NoP77svk.TibcoDV.CLI.AST
{
    public record TimeSpan(double value, TimeUnitEnum unit)
    {
        public System.TimeSpan AsSystemTimeSpan()
        {
            return unit switch
            {
                TimeUnitEnum.Millisecond => System.TimeSpan.FromMilliseconds(value),
                TimeUnitEnum.Second => System.TimeSpan.FromSeconds(value),
                TimeUnitEnum.Minute => System.TimeSpan.FromMinutes(value),
                TimeUnitEnum.Hour => System.TimeSpan.FromHours(value),
                TimeUnitEnum.Day => System.TimeSpan.FromDays(value),
                TimeUnitEnum.Week => System.TimeSpan.FromDays(value * 7),
                _ => throw new System.ArgumentOutOfRangeException(nameof(value), value.ToString())
            };
        }
    }
}
