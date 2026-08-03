namespace Wasl.Application.Common
{
    public static class MoneyHelper
    {
        public static decimal Round(decimal value)
            => Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }
}