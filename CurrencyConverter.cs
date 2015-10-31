using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Xml;

namespace CurrencyExchange
{
    /// <summary>
    /// This class contains some functions to manipulate currencies.
    /// It gets information from the servers of European Central Bank.
    /// To get list of available currencies, please use GetCurrencyList() method, the return type is IEnumerable string .
    /// On construction the XML file is parsed, if something goes wrong Exeption will be thrown(WebException, FormatException or XmlException).
    /// Even if there is no connection to ECB servers, default value is created for BGN / EUR convertion (the rate is constant).
    /// @author Stamo Petkov
    /// @version 1.0.0
    /// @name Currency
    /// </summary>
    public class CurrencyConverter : ICurrencyConverter
    {
        private const string SourceUrl = @"http://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
        private const string BackupSourceUrl = @"eurofxref-daily.xml";
        private Currencies _baseCurrency = Currencies.Eur;
        private readonly Dictionary<Currencies, decimal> _exchangeRates;

        /// <summary>
        /// Use this readonly property to check the actual date for the rates
        /// </summary>
        public DateTime Date { get; private set; }

        //Use this property to get or set base currency
        //Base currency is used for displaying rates table and convertions. All calculations are performed according to base currency!
        //EUR by default
        //Throws ApplicationException if value is not in currency list
        public Currencies BaseCurrency
        {
            get { return _baseCurrency; }
            set
            {
                if (_baseCurrency != value)
                {
                    _baseCurrency = value;
                    Rebase();
                }
            }
        }

        private void Rebase()
        {
            var factor = _exchangeRates[_baseCurrency];
            var keys = (from k in _exchangeRates select k.Key).ToList();
            foreach (var k in keys)
                _exchangeRates[k] /= factor;
        }

        public CurrencyConverter()
        {
            _exchangeRates = new Dictionary<Currencies, decimal>();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                ReadXml(SourceUrl);
            }
            catch 
            {
                /* If web exception, default to input file */
                ReadXml(BackupSourceUrl);
            }
            finally
            {
                _exchangeRates.Add(_baseCurrency, 1M);
            }
        }

        private void ReadXml(string url)
        {
            //tries to download XML file and create the Reader object
            using (var xml = new XmlTextReader(url))
            {
                while (xml.Read())
                {
                    if (xml.Name == "Cube")
                    {
                        if (xml.AttributeCount == 1)
                        {
                            xml.MoveToAttribute("time");
                            DateTime date;
                            if (DateTime.TryParse(xml.Value, out date))
                                Date = date;
                            else
                                throw new FormatException(string.Format("Urecognised format in time! {0}", xml.Value));
                        }
                        if (xml.AttributeCount == 2)
                        {
                            xml.MoveToAttribute("currency");
                            Currencies cur;
                            if (Enum.TryParse(xml.Value, out cur))
                            {
                                xml.MoveToAttribute("rate");
                                Decimal rate;
                                if (decimal.TryParse(xml.Value, out rate))
                                    _exchangeRates.Add(cur, rate);
                                else
                                    throw new FormatException(string.Format("Urecognised format in rate! {0}", xml.Value));
                            }
                            else
                                throw new FormatException(string.Format("Urecognised format in currency! {0}", xml.Value));
                        }
                        xml.MoveToNextAttribute();
                    }
                }
            }
        }

        /// <summary>
        /// Converts Exchange Rate Table to String
        /// </summary>
        /// <returns></returns>
        public override string ToString() 
        {
            var str = new StringBuilder();
            str.Append("Reference rates of European Central Bank\nAll rates are for 1 " + _baseCurrency + "\n\n");
            foreach (var kvp in _exchangeRates)
                str.Append(String.Format("{0}{1,15:0.0000}\n", kvp.Key, kvp.Value));

            return str.ToString();
        }

        /// <summary>
        /// Exchanges the givven amount from one currency to the other
        /// param Decimal amount The amount to be exchanged
        /// param String from Currency of the amount (three letter code)
        /// param String to Currency to witch we wish to exchange. Base currency if not specified.
        /// returns Decimal - the exchanged amount on success
        /// Throws ApplicationException if currency is not in currency list
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public decimal Exchange(decimal amount, Currencies from, Currencies to)
        {
            return amount * _exchangeRates[to] / _exchangeRates[@from];
        }

        /// <summary>
        /// Gets the cross rate between two currencies
        /// param String from first Currency (three letter code)
        /// param String to second Currency (three letter code). Base currency if not specified.
        /// returns decimal - the cross rate on success
        /// Throws ApplicationException if currency is not in currency list
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public decimal CrossRate(Currencies from)
        {
            return CrossRate(from, _baseCurrency);
        }

        /// <summary>
        /// Gets the cross rate between two currencies
        /// param String from first Currency (three letter code)
        /// param String to second Currency (three letter code). Base currency if not specified.
        /// returns decimal - the cross rate on success
        /// Throws ApplicationException if currency is not in currency list
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public decimal CrossRate(Currencies from, Currencies to)
        {
            return _exchangeRates[to] / _exchangeRates[@from];
        }
       
        /// <summary>
        /// Gets the rates table based on Base currency
        /// param string currencyList - list of comma separated Currencies to be included in the table. All currencies by default
        /// returns IEnumerable Rates containing desired currencies and rates
        /// Throws ApplicationException if currency is not in currency list
        /// </summary>
        /// <param name="currencyList"></param>
        /// <returns></returns>
        public IList<Rates> GetRatesTable(List<Currencies> currencyList)
        {
            return new List<Rates>(currencyList == null
                                ? _exchangeRates.Keys.Select(CreateRate)
                                : currencyList.Select(CreateRate));
        }

        private Rates CreateRate(Currencies currency)
        {
            return new Rates {Currency = currency, Rate = String.Format("{0:0.0000}", _exchangeRates[currency])};
        }

        /// <summary>
        /// Gets the list of currencies. If sorted is true, the returned list is sorted. False by default
        /// returns IEnumerable string  of all available currencies 
        /// </summary>
        /// <param name="sorted"></param>
        /// <returns></returns>
        public IList<Currencies> GetCurrencyList(bool sorted = false)
        {
            var list = (from rate in _exchangeRates
                        select rate.Key).ToList();

            if (sorted)
                list.Sort();

            return list;
        }
    }
}