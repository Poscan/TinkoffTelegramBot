using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using telegramBot.Domain;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace telegramBot.Services
{
    public class TinkoffService
    {
        private Context _context;

        public TinkoffService(string token)
        {
            var connection = ConnectionFactory.GetConnection(token);

            _context = connection.Context;
        }

        public async Task<Portfolio> GetPortfolioAsync()
        {
            return await _context.PortfolioAsync();
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            _context = ConnectionFactory.GetConnection(token).Context;
            try
            {
                var accounts = await _context.AccountsAsync();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async IAsyncEnumerable<Currencies> GetCurrencies()
        {
            var currencies = (await _context.PortfolioCurrenciesAsync()).Currencies;

            foreach (var currency in currencies)
            {
                yield return new Currencies
                {
                    Balance = currency.Balance,
                    Currency = (int) currency.Currency
                };
            }
        }

        public async IAsyncEnumerable<Stock> GetBondsAsync()
        {
            var portfolio = await GetPortfolioAsync();
            var stocks = portfolio.Positions.Where(position => position.InstrumentType == InstrumentType.Bond);

            foreach (var stock in stocks)
            {
                //var currentPrice = GetCurrentPrice(stock.Figi);
                //var pricePerDay = GetPercentagesPerDayAsync(stock.Figi, currentPrice.Result);

                yield return new Stock
                {
                    AveragePositionPrice = stock.AveragePositionPrice.Value,
                    //CurrentPrice = currentPrice.Result,
                    CurrentPrice = stock.AveragePositionPrice.Value,
                    Lots = stock.Lots,
                    Name = stock.Name,
                    //PercentagesPerDay = decimal.Round(pricePerDay.Result, 2)
                };
            }
        }

        public async IAsyncEnumerable<Stock> GetStocksAsync()
        {
            var portfolio = await GetPortfolioAsync();
            var stocks = portfolio.Positions.Where(position => position.InstrumentType == InstrumentType.Stock);

            foreach (var stock in stocks)
            {
                var currentPrice = GetCurrentPrice(stock.Figi);
                var pricePerDay = GetPercentagesPerDayAsync(stock.Figi, currentPrice.Result);

                yield return new Stock
                {
                    AveragePositionPrice = stock.AveragePositionPrice.Value,
                    CurrentPrice = currentPrice.Result,
                    Lots = stock.Lots,
                    Name = stock.Name,
                    PercentagesPerDay = decimal.Round(pricePerDay.Result, 2)
                };
            }
        }

        private async Task<decimal> GetPercentagesPerDayAsync(string figi, decimal currentPrice)
        {
            var candleList = await _context
                .MarketCandlesAsync(figi, Today().AddHours(8), DateTime.Now.AddHours(8), CandleInterval.Hour);

            var candles = candleList.Candles;

            return currentPrice * 100 / candles.First().Open - 100;
        }

        private async Task<decimal> GetCurrentPrice(string figi)
        {
            var orderbook = await _context.MarketOrderbookAsync(figi, 0);
            return orderbook.LastPrice;
        }

        private DateTime Today()
        {
            var today = DateTime.Today;

            if (today.Day == 1 && today.Month == 1)
            {
                var year = today.Year - 1;
                return new DateTime(year, 12, DateTime.DaysInMonth(year, 12));
            }

            if (today.Day == 1)
            {
                var mounth = today.Month - 1;
                return new DateTime(today.Year, mounth, DateTime.DaysInMonth(today.Year, mounth));
            }

            return today;
        }
    }
}