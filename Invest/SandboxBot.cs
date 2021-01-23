using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace Invest
{
    class SandboxBot : IAsyncDisposable
    {
        private static readonly Random Random = new Random();
        private readonly SandboxContext _context;
        private string _accountId;
        int c = 0;

        public SandboxBot(string token)
        {
            var connection = ConnectionFactory.GetSandboxConnection(token);
            _context = connection.Context;


        }

        public async Task StartAsync()
        {
            Console.WriteLine("Начали метод");
            // register new sandbox account
            var sandboxAccount = await _context.RegisterAsync(BrokerAccountType.Tinkoff);
            _accountId = sandboxAccount.BrokerAccountId;


            if (c == 0)
            {
                foreach (var currency in new[] { Currency.Rub, Currency.Usd, Currency.Eur })
                    await _context.SetCurrencyBalanceAsync(currency, 1000, sandboxAccount.BrokerAccountId);

                c += 1;
            }          


            await CheckBalanceAsync();

            // select random instrument
            var instrumentList = await _context.MarketSearchByTickerAsync("Save");
            // var randomInstrumentIndex = Random.Next(instrumentList.Total);
            var randomInstrument = instrumentList.Instruments[0];
            Console.WriteLine($"Selected Instrument: {randomInstrument.Name}");
            Console.WriteLine();

            // get candles
            var now = DateTime.Now;
            var candleList = await _context.MarketCandlesAsync(randomInstrument.Figi, now.AddMinutes(-120), now, CandleInterval.Minute);
            foreach (var candle in candleList.Candles)
            {
               // Console.WriteLine(candle);
            }           

            var k = await _context.MarketOrderbookAsync(randomInstrument.Figi, 10); // выводит стакан
            var e = await _context.PortfolioAsync(_accountId); // весь портфель

            Console.WriteLine("Buy 1 lot, price - " + k.Bids[0].Price); // Реализация покупки
            //await _context.PlaceMarketOrderAsync(new MarketOrder(randomInstrument.Figi, 10, OperationType.Buy,
            //    _accountId));
            await _context.PlaceLimitOrderAsync(new LimitOrder(randomInstrument.Figi, 1, OperationType.Buy, k.Bids[0].Price, _accountId));


            await CheckBalanceAsync();
            await Task.Delay(1000);
            
            
            e = await _context.PortfolioAsync(_accountId); // весь портфель
            var r = await _context.OrdersAsync(_accountId);


            Console.WriteLine("Sell 1 lot, price - " + k.Asks[0].Price); //Реализация продажи
            //await _context.PlaceMarketOrderAsync(new MarketOrder(randomInstrument.Figi, 10, OperationType.Sell,
            //    _accountId));
            await _context.PlaceLimitOrderAsync(new LimitOrder(randomInstrument.Figi, 25, OperationType.Sell, k.Asks[0].Price, _accountId));


            await CheckBalanceAsync();
            e = await _context.PortfolioAsync(_accountId); // весь портфель
            r = await _context.OrdersAsync(_accountId);
            Console.WriteLine("Закончили метод");
        }

        private async Task CheckBalanceAsync()
        {
            var portfolio = await _context.PortfolioCurrenciesAsync(_accountId);
            Console.WriteLine("Balance " + portfolio.Currencies[2].Balance+" "+ portfolio.Currencies[2].Currency);
            //foreach (var currency in portfolio.Currencies) Console.WriteLine($"{currency.Balance} {currency.Currency}");

            Console.WriteLine();
        }       

        public async ValueTask DisposeAsync()
        {
            if (_accountId != null) await _context.RemoveAsync(_accountId);
        }
    }
}