using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CurrencyExchange
{
    [TestFixture]
    public class CurrencyTest
    {
        [Test]
        public void Test01()
        {
            var ratetable = new CurrencyConverter {BaseCurrency = Currencies.BGN};

            Console.WriteLine("----------- ToString() Test -----------\n");
            Console.WriteLine(ratetable);
            Console.WriteLine();
            Console.WriteLine("-------- GetCurrencyList() Test -------\n");
            var currencyList = ratetable.GetCurrencyList().ToArray();

            foreach (var item in currencyList)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine();
            Console.WriteLine(" Exchange(5M, \"EUR \", \"usd\") Test \n");
            Console.WriteLine(ratetable.Exchange(5M, Currencies.EUR, Currencies.USD));
            Console.WriteLine();
            Console.WriteLine("-------- CrossRate(\"EUR\") Test -------\n");
            Console.WriteLine(ratetable.CrossRate(Currencies.EUR));
            Console.WriteLine();
            Console.WriteLine("GetRatesTable(\"eur, bgn; usd,gbp CHF  \") Test\n");
            var customRates = ratetable.GetRatesTable(new List<Currencies>(new[] { Currencies.EUR, Currencies.BGN, Currencies.USD, Currencies.GBP, Currencies.CHF }));
            foreach (var rate in customRates)
            {
                Console.WriteLine("{0} = {1}", rate.Currency, rate.Rate);
            }
        }
    }
}
