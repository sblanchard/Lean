﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Globalization;
using QuantConnect.Logging;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Tick class is the base representation for tick data. It is grouped into a Ticks object
    /// which implements IDictionary and passed into an OnData event handler.
    /// </summary>
    public class Tick : BaseData
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        /// <summary>
        /// Type of the Tick: Trade or Quote.
        /// </summary>
        public TickType TickType = TickType.Trade;

        /// <summary>
        /// Quantity of the tick sale or quote offer.
        /// </summary>
        public int Quantity = 0;

        /// <summary>
        /// Exchange we are executing on. String short code expanded in the MarketCodes.US global dictionary
        /// </summary>
        public string Exchange = "";

        /// <summary>
        /// Sale condition for the tick.
        /// </summary>
        public string SaleCondition = "";

        /// <summary>
        /// Bool whether this is a suspicious tick
        /// </summary>
        public bool Suspicious = false;

        /// <summary>
        /// Bid Price for Tick
        /// </summary>
        /// <remarks>QuantConnect does not currently have quote data but was designed to handle ticks and quotes</remarks>
        public decimal BidPrice = 0;

        /// <summary>
        /// Asking price for the Tick quote.
        /// </summary>
        /// <remarks>QuantConnect does not currently have quote data but was designed to handle ticks and quotes</remarks>
        public decimal AskPrice = 0;

        /// <summary>
        /// Alias for "Value" - the last sale for this asset.
        /// </summary>
        public decimal LastPrice
        {
            get
            {
                return Value;
            }
        }

        // In Base Class: Last Trade Tick:
        //public decimal Price = 0;

        // In Base Class: Ticker String Symbol of the Asset
        //public string Symbol = "";

        // In Base Class: DateTime of this SnapShot
        //public DateTime Time = new DateTime();

        /******************************************************** 
        * CLASS CONSTRUCTORS
        *********************************************************/
        /// <summary>
        /// Initialize tick class with a default constructor.
        /// </summary>
        public Tick()
        {
            Value = 0;
            Time = new DateTime();
            DataType = MarketDataType.Tick;
            Symbol = "";
            TickType = TickType.Trade;
            Quantity = 0;
            Exchange = "";
            SaleCondition = "";
            Suspicious = false;
        }

        /// <summary>
        /// Cloner constructor for fill formward engine implementation. Clone the original tick into this new tick:
        /// </summary>
        /// <param name="original">Original tick we're cloning</param>
        public Tick(Tick original) 
        {
            Symbol = original.Symbol;
            Time = new DateTime(original.Time.Ticks);
            BidPrice = original.BidPrice;
            AskPrice = original.AskPrice;
            Exchange = original.Exchange;
            SaleCondition = original.SaleCondition;
            Quantity = original.Quantity;
            Suspicious = original.Suspicious;
        }

        /// <summary>
        /// Constructor for a FOREX tick where there is no last sale price. The volume in FX is so high its rare to find FX trade data.
        /// To fake this the tick contains bid-ask prices and the last price is the midpoint.
        /// </summary>
        /// <param name="time">Full date and time</param>
        /// <param name="symbol">Underlying currency pair we're trading</param>
        /// <param name="bid">FX tick bid value</param>
        /// <param name="ask">FX tick ask value</param>
        public Tick(DateTime time, string symbol, decimal bid, decimal ask)
        {
            DataType = MarketDataType.Tick;
            Time = time;
            Symbol = symbol;
            Value = bid + (ask - bid) / 2;
            TickType = TickType.Quote;
            BidPrice = bid;
            AskPrice = ask;
        }


        /// <summary>
        /// Initializer for a last-trade equity tick with bid or ask prices. 
        /// </summary>
        /// <param name="time">Full date and time</param>
        /// <param name="symbol">Underlying equity security symbol</param>
        /// <param name="bid">Bid value</param>
        /// <param name="ask">Ask value</param>
        /// <param name="last">Last trade price</param>
        public Tick(DateTime time, string symbol, decimal last, decimal bid, decimal ask)
        {
            DataType = MarketDataType.Tick;
            Time = time;
            Symbol = symbol;
            Value = last;
            TickType = TickType.Quote;
            BidPrice = bid;
            AskPrice = ask;
        }

        /// <summary>
        /// Constructor for QuantConnect FXCM Data source:
        /// </summary>
        /// <param name="symbol">Symbol for underlying asset</param>
        /// <param name="line">CSV line of data from FXCM</param>
        public Tick(string symbol, string line)
        {
            var csv = line.Split(',');
            DataType = MarketDataType.Tick;
            Symbol = symbol;
            Time = DateTime.ParseExact(csv[0], "yyyyMMdd HH:mm:ss.ffff", CultureInfo.InvariantCulture);
            Value = BidPrice + (AskPrice - BidPrice) / 2;
            TickType = TickType.Quote;
            BidPrice = Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture);
            AskPrice = Convert.ToDecimal(csv[2], CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Parse a tick data line from quantconnect zip source files.
        /// </summary>
        /// <param name="line">CSV source line of the compressed source</param>
        /// <param name="date">Base date for the tick (ticks date is stored as int milliseconds since midnight)</param>
        /// <param name="config">Subscription configuration object</param>
        /// <param name="datafeed">Datafeed for tick - live or backtesting.</param>
        public Tick(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            try
            {
                var csv = line.Split(',');

                // Which security type is this data feed:
                switch (config.Security)
                { 
                    case SecurityType.Equity:
                        Symbol = config.Symbol;
                        Time = date.Date.AddMilliseconds(Convert.ToInt64(csv[0]));
                        Value = (csv[1].ToDecimal() / 10000m) * config.PriceScaleFactor;
                        DataType = MarketDataType.Tick;
                        TickType = TickType.Trade;
                        Quantity = Convert.ToInt32(csv[2]);
                        if (csv.Length > 3)
                        {
                            Exchange = csv[3];
                            SaleCondition = csv[4];
                            Suspicious = (csv[5] == "1");
                        }
                        break;

                    case SecurityType.Forex:
                        Symbol = config.Symbol;
                        TickType = TickType.Quote;
                        Time = DateTime.ParseExact(csv[0], "yyyyMMdd HH:mm:ss.ffff", CultureInfo.InvariantCulture);
                        BidPrice = csv[1].ToDecimal();
                        AskPrice = csv[2].ToDecimal();
                        Value = BidPrice + (AskPrice - BidPrice) / 2;
                        break;
                }
            }
            catch (Exception err)
            {
                Log.Error("Error Generating Tick: " + err.Message);
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Tick implementation of reader method: read a line of data from the source and convert it to a tick object.
        /// </summary>
        /// <param name="datafeed">Source of the datafeed</param>
        /// <param name="config">Subscription configuration object for algorithm</param>
        /// <param name="line">Line from the datafeed source</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns>New Initialized tick</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            var _tick = new Tick();

            //Select the URL source of the data depending on where the system is trading.
            switch (datafeed)
            {
                //Local File System Storage and Backtesting QC Data Store Feed use same files:
                case DataFeedEndpoint.FileSystem:
                case DataFeedEndpoint.Backtesting:
                    //Create a new instance of our tradebar:
                    _tick = new Tick(config, line, date, datafeed);
                    break;
                case DataFeedEndpoint.LiveTrading:
                    break;
            }

            return _tick;
        }


        /// <summary>
        /// Get source for tick data feed - not used with QuantConnect data sources implementation.
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source request if source spread across multiple files</param>
        /// <param name="datafeed">Source of the datafeed enum</param>
        /// <returns>String source location of the file to be opened with a stream</returns>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            var source = "";
            var dataType = TickType.Trade;

            switch (datafeed)
            {
                //Backtesting S3 Endpoint:
                case DataFeedEndpoint.Backtesting:
                case DataFeedEndpoint.FileSystem:

                    var dateFormat = "yyyyMMdd";
                    if (config.Security == SecurityType.Forex)
                    {
                        dataType = TickType.Quote;
                        dateFormat = "yyMMdd";
                    }

                    source = @"../../../Data/" + config.Security.ToString().ToLower();
                    source += @"/" + config.Resolution.ToString().ToLower() + @"/" + config.MappedSymbol.ToLower() + @"/";
                    source += date.ToString(dateFormat) + "_" + dataType.ToString().ToLower() + ".zip";
                    break;

                //Live Trading Endpoint: Fake, not actually used but need for consistency with backtesting system. Set to "" so will not use subscription reader.
                case DataFeedEndpoint.LiveTrading:
                    source = "";
                    break;
            }
            return source;
        }


        /// <summary>
        /// Update the tick price information - not used.
        /// </summary>
        /// <param name="lastTrade">This trade price</param>
        /// <param name="bidPrice">Current bid price</param>
        /// <param name="askPrice">Current asking price</param>
        /// <param name="volume">Volume of this trade</param>
        public override void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume)
        {
            Value = lastTrade;
            BidPrice = bidPrice;
            AskPrice = askPrice;
            Quantity = Convert.ToInt32(volume);
        }


        /// <summary>
        /// Clone implementation for tick class:
        /// </summary>
        /// <returns>New tick object clone of the current class values.</returns>
        public override BaseData Clone()
        {
            return new Tick(this);
        }

    } // End Tick Class:
}