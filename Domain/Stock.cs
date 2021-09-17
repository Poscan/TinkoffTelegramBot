using System;

namespace telegramBot.Domain
{
    public class Stock
    {
        public Decimal ExpectedYield => CurrentPrice * Lots - CostPrice;
        public Decimal AveragePositionPrice { get; set; }
        public int Lots { get; set; }
        public string Name { get; set; }

        public Decimal CostPrice => AveragePositionPrice * Lots;
        public Decimal CurrentPrice { get; set; }
        public Decimal Percentages => Decimal.Round(((CurrentPrice * Lots * 100 / CostPrice) - 100), 2);
        public Decimal PercentagesPerDay { get; set; }
    }
}
