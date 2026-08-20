namespace Wasl.Application.Common
{
    public static class MoneyHelper
    {
        public static decimal Round(decimal value)
            => Math.Round(value, 0, MidpointRounding.AwayFromZero);

        public static decimal RoundToIncrement(decimal value, decimal increment)
        {
            if (increment <= 0)
                return Round(value);

            return Math.Round(value / increment, 0, MidpointRounding.AwayFromZero) * increment;
        }
    }
}