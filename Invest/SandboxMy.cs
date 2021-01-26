using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace Invest
{
    class SandboxMy : IAsyncDisposable
    {       
        private readonly SandboxContext _context;
        private string _accountId;

        int countOfOperationsDay = 0;
        decimal balance = 1000;
        int countOfStock = 0;
        decimal priceOfStock = 0;
        decimal priceOfStockClosed = 0;
                
        decimal emaFast = 0;
        decimal emaFastLast = 0;
        int fastCountEma = 8;
        decimal kFast=0;

        decimal emaSlow = 0;
        decimal emaSlowLast = 0;
        int slowCountEma = 17;
        decimal kSlow = 0;

        decimal macdLast = 0;
        decimal macd = 0;
        decimal spreadMacdSignal = 0;
        decimal spreadMacdSignalLast = 0;
        decimal spreadMacdSignalGrowthInRow = 0;
        decimal spreadMacdSignalFallInRow = 0;

        decimal signal = 0;
        decimal signalLast = 0;
        int signalCount = 9;
        decimal kSignal = 0;
             
        decimal priceOfClosingPlus = 0;
        decimal priceOfClosingMinus = 0;

        int plusOperAll = 0;
        int minusOperAll = 0;

        //Queue<decimal> last5SpreqdAvg

        int countOfOperationsMonth;

        public SandboxMy(string token)
        {
            var connection = ConnectionFactory.GetSandboxConnection(token);
            _context = connection.Context;

            kFast = Math.Round(2 / (decimal)(fastCountEma+1),4);
            kSlow = Math.Round(2 / (decimal)(slowCountEma + 1), 4);
            kSignal = Math.Round(2 / (decimal)(signalCount + 1), 4);
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Начали метод");
            // register new sandbox account
            var sandboxAccount = await _context.RegisterAsync(BrokerAccountType.Tinkoff);
            _accountId = sandboxAccount.BrokerAccountId;

            // select random instrument
            var instrumentList = await _context.MarketSearchByTickerAsync("tsla");
            // var randomInstrumentIndex = Random.Next(instrumentList.Total);
            var randomInstrument = instrumentList.Instruments[0];
            Console.WriteLine($"Selected Instrument: {randomInstrument.Name}");
            Console.WriteLine();

            var k = await _context.MarketOrderbookAsync(randomInstrument.Figi, 10); // выводит стакан
            var e = await _context.PortfolioAsync(_accountId); // весь портфель


            // get candles
            //var now = DateTime.Now;           
            //now = now.AddDays(-3);
            int monthNumber = 12;
            int year = 2020;            
            int daysInMonth = DateTime.DaysInMonth(year,monthNumber);

            for (int day=1;day<=daysInMonth;day++)
            {
                countOfOperationsDay = 0;      
                emaFastLast = 0;
                emaSlowLast = 0;
                priceOfStock = 0;
                priceOfClosingPlus = 0;
                priceOfClosingMinus = 0;
                priceOfStockClosed = 0;
                emaFast = 0;
                emaSlow = 0;
                macd = 0;
                signal = 0;
                signalLast = 0;
                spreadMacdSignal = 0;
                spreadMacdSignalFallInRow = 0;
                spreadMacdSignalGrowthInRow = 0;

                int plusOper = 0;
                int minusOper = 0;

                if(day == 9)
                {

                }

                var now = new DateTime(year, monthNumber, day, 13, 55, 0);         // на 1 свечу больше, тк 1-я свеча идёт на образование EMA  
                var candleList = await _context.MarketCandlesAsync(randomInstrument.Figi, now, now.AddHours(9).AddMinutes(50), CandleInterval.FiveMinutes);
                for (int i = 1; i < candleList.Candles.Count; i++)
                {
                    if(i == 38)
                    {

                    }

                    if (emaFastLast == 0)
                    {
                        emaFastLast = candleList.Candles[i - 1].Close;
                    }
                    if (emaSlowLast == 0)
                    {
                        emaSlowLast = candleList.Candles[i - 1].Close;
                    }

                    emaFast = Math.Round(emaFastLast + (kFast * (candleList.Candles[i].Close - emaFastLast)),5);
                    emaFastLast = emaFast;

                    emaSlow = Math.Round(emaSlowLast + (kSlow * (candleList.Candles[i].Close - emaSlowLast)),5);
                    emaSlowLast = emaSlow;

                    macdLast = macd;
                    macd = emaFast - emaSlow;

                    signal = Math.Round(signalLast + (kSignal * (macd - signalLast)), 10);
                    signalLast = signal;

                    spreadMacdSignalLast = spreadMacdSignal;
                    spreadMacdSignal = macd - signal;

                    if (i < slowCountEma )
                        continue;

                    if (spreadMacdSignalLast >= spreadMacdSignal)
                    {
                        spreadMacdSignalFallInRow += 1;
                        spreadMacdSignalGrowthInRow = 0;
                    }
                    else
                    {
                        spreadMacdSignalGrowthInRow += 1;
                        spreadMacdSignalFallInRow = 0;
                    }

                    // ВЫШЕ ВЫЧИСЛЕНИЯ ВСЕКИХ НУЖНЫХ ПЕРЕМЕННЫХ
                    // комент снизу вроде как помогоает если менять число акций 
                    if ( countOfStock < 1 && spreadMacdSignalLast < 0 && spreadMacdSignal >= 0 ) // || countOfStock < 1 && spreadMacdSignalLast < 0 && spreadMacdSignal >= 0
                    {
                        countOfStock += 1;
                        countOfOperationsDay++;
                        balance -= candleList.Candles[i].Close;
                        priceOfStock = candleList.Candles[i].Close;
                        priceOfClosingPlus = Decimal.Multiply(priceOfStock, (decimal)1.002);
                        priceOfClosingMinus = Decimal.Multiply(priceOfStock, (decimal)0.998);
                    }
                   

                    //if (countOfStock != 0 && candleList.Candles[i].High > priceOfClosingPlus)
                    //{
                    //    balance += priceOfClosingPlus * countOfStock;
                    //    countOfStock = 0;     
                    //    plusOper++;

                    //}
                    if (countOfStock != 0 && candleList.Candles[i].Low < priceOfClosingMinus && spreadMacdSignalFallInRow>=2)
                    {
                        balance += priceOfClosingMinus * countOfStock;
                        countOfStock = 0;

                        if (priceOfClosingMinus > priceOfStock)
                        {
                            plusOper++;
                        }
                        else
                        {
                            minusOper++;
                        }
                    }

                    //countOfStock != 0 && spreadMacdSignalFallInRow >= 2 было изначально
                    if (countOfStock != 0 && signalLast < macdLast && signal >= macd && Math.Abs(spreadMacdSignal) > (decimal)0.06) // для бага && priceOfStock < candleList.Candles[i].Close
                    {
                        balance += candleList.Candles[i].Close * countOfStock;                        
                        countOfStock = 0;
                        if (candleList.Candles[i].Close > priceOfStock)
                        {
                            plusOper++;
                        }
                        else
                        {
                            minusOper++;
                        }
                    }

                    if (countOfStock != 0 && candleList.Candles.Count-1 == i ) 
                    {
                        balance += candleList.Candles[i].Close * countOfStock;
                        countOfStock = 0;
                        if (candleList.Candles[i].Close > priceOfStock)
                        {
                            plusOper++;
                        }
                        else
                        {
                            minusOper++;
                        }
                    }   

                    //if(maSlowPred != 0 && maFastPred !=0 && maFastPred < maSlowPred && maFast >= maSlow)
                    //{
                    //    countOfStock += 1;
                    //    countOfOperations++;
                    //    balance -= candleListSlow.Candles[i].Close;
                    //}

                    //if (maSlowPred != 0 && maFastPred != 0 && maFastPred > maSlowPred && maFast <= maSlow)
                    //{
                    //    balance += candleListSlow.Candles[i].Close * countOfStock;
                    //    countOfStock = 0;
                    //}

                    //if (candleList.Candles[i].Time.TimeOfDay.TotalHours > 20.45 && countOfStock != 0)
                    //{
                    //    balance += candleList.Candles[i].Close * countOfStock;
                    //    countOfStock = 0;
                    //}

                    // Console.WriteLine(candle);
                }

                Console.WriteLine($"В {day}-торговый день баланс составил {balance}, операций в день {countOfOperationsDay}." +
                    $" В плюс {plusOper}, в минус {minusOper}");
                countOfOperationsMonth += countOfOperationsDay;
                plusOperAll += plusOper;
                minusOperAll += minusOper;
            }

            Console.WriteLine($"Всего операций за месяц {countOfOperationsMonth}. Положительных {plusOperAll}. Отрицательных {minusOperAll}");

            k = await _context.MarketOrderbookAsync(randomInstrument.Figi, 10); // выводит стакан
            e = await _context.PortfolioAsync(_accountId); // весь портфель

            Console.WriteLine("Buy 1 lot, price - " + k.Bids[0].Price); // Реализация покупки
            //await _context.PlaceMarketOrderAsync(new MarketOrder(randomInstrument.Figi, 10, OperationType.Buy,
            //    _accountId));
            await _context.PlaceLimitOrderAsync(new LimitOrder(randomInstrument.Figi, 1, OperationType.Buy, k.Asks[0].Price, _accountId));


            await CheckBalanceAsync();
            await Task.Delay(1000);


            e = await _context.PortfolioAsync(_accountId); // весь портфель
            var r = await _context.OrdersAsync(_accountId);


            Console.WriteLine("Sell 1 lot, price - " + k.Asks[0].Price); //Реализация продажи
            //await _context.PlaceMarketOrderAsync(new MarketOrder(randomInstrument.Figi, 10, OperationType.Sell,
            //    _accountId));
            await _context.PlaceLimitOrderAsync(new LimitOrder(randomInstrument.Figi, 1, OperationType.Sell, k.Bids[0].Price, _accountId));


            await CheckBalanceAsync();
            e = await _context.PortfolioAsync(_accountId); // весь портфель
            r = await _context.OrdersAsync(_accountId);
            Console.WriteLine("Закончили метод");
        }

        private async Task CheckBalanceAsync()
        {
            var portfolio = await _context.PortfolioCurrenciesAsync(_accountId);
            Console.WriteLine("Balance " + portfolio.Currencies[2].Balance + " " + portfolio.Currencies[2].Currency);
            //foreach (var currency in portfolio.Currencies) Console.WriteLine($"{currency.Balance} {currency.Currency}");

            Console.WriteLine();
        }

        public async ValueTask DisposeAsync()
        {
            if (_accountId != null) await _context.RemoveAsync(_accountId);
        }
    }
}