#pragma warning disable SA1313
namespace NoP77svk.TibcoDV.CLI.AST
{
    public record TimeSpan(double Value, TimeUnitEnum Unit)
    {
        public System.TimeSpan AsSystemTimeSpan()
        {
            return Unit switch
            {
                TimeUnitEnum.Millisecond => System.TimeSpan.FromMilliseconds(Value),
                TimeUnitEnum.Second => System.TimeSpan.FromSeconds(Value),
                TimeUnitEnum.Minute => System.TimeSpan.FromMinutes(Value),
                TimeUnitEnum.Hour => System.TimeSpan.FromHours(Value),
                TimeUnitEnum.Day => System.TimeSpan.FromDays(Value),
                TimeUnitEnum.Week => System.TimeSpan.FromDays(Value * 7),
                _ => throw new System.ArgumentOutOfRangeException(nameof(Value), Value.ToString())
            };
        }
    }
}
