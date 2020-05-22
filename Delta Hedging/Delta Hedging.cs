using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Refinitiv.DataPlatform;
using Refinitiv.DataPlatform.Core;

// **********************************************************************************************************************
// The following C#/.NET example will utilize the Refinitiv Data Platform endpoints (Pricing and IPA - Instrument 
// Pricing Analytics) using the Refinitiv Data Platform Library for .NET to retrieve Option Delta values as a basis to 
// build delta hedging scenarios for some strategy workflows.
// **********************************************************************************************************************
namespace DeltaHedging
{
    class Program
    {
        #region Program Resources
        // Position details we are presenting
        class Position
        {
            public String Key { get; set; }
            public String Instrument { get; set; }
            public String BuySell { get; set; }
            public String CallPut { get; set; }
            public String ExerciseStyle { get; set; }
            public String Type { get; set; }
            public double DeltaPct { get; set; }
            public int Contracts { get; set; }
            public int Delta { get; set; }
            public double Strike { get; set; }
            public DateTime Expiry { get; set; }
            public string Underlying { get; set; }
            public string UnderlyingStyle { get; set; }
            public double UnderlyingPrice { get; set; }
            public double OptionPrice { get; set; }
            public int DaysToExpire { get; set; }
        }

        private static IList<Position> Positions = new List<Position>();
        private static ISession Session { get; set; }
        private static int Tag { get; set; }
        #endregion

        static void Main(string[] _)
        {
            try
            {
                Log.Level = NLog.LogLevel.Debug;

                // *************************************************
                // Connect into the platform.
                // *************************************************
                Session = Configuration.Sessions.GetSession();

                // Open the session
                Session.Open();

                // *************************************************
                // Use Cases to demonstrate Delta Hedging
                // *************************************************

                // ******************************************
                // Use Case 1 - Long Strangle
                // ******************************************
                Console.WriteLine("\n***************************\nStrategy: Long Strangle\n***************************");

                // Step 1 - Retrieve our option chain.
                //      Criteria: ~7 Months out
                //                50% Strike Around-The-Money
                var chain = new OptionChain(Session);
                var contracts = chain.GetContracts("0#AAPL*.U", 0.30, DateTime.Now.AddMonths(7));

                Console.WriteLine($"\nRetrieved a total of {contracts.Count} contracts for the underlying {chain.Underlying} with the last trade at: {chain.UnderlyingPrice}\n");

                // Step 2 - Price the options to retrieve delta
                //        - Buy ETI options
                var etiContracts = new List<JObject>();
                foreach (var contract in contracts)
                    etiContracts.Add(IPAOption.DefineETI(contract, "Buy"));

                // Price our options based on our option chain and display
                var ipaOption = new IPAOption(Session);
                var options = ipaOption.PriceOptions(etiContracts);

                OptionChain.DisplayOptionChain(options);

                // ******************************************
                // Use Case 2: Delta Neutral
                // ******************************************
                Console.WriteLine("\n***************************\nStrategy: Delta Neutral\n***************************");

                // For simpliciy, I will use the underlying details used in Use Case 1.
                string underlying = options[0]["UnderlyingRIC"].ToString();
                string style = options[0]["ExerciseStyle"].ToString();
                double underlyingPrice = options[0]["UnderlyingPrice"].ToObject<double>();

                // Our initial position - long 500 shares
                var dneutral = new DeltaNeutral(ipaOption, underlying, underlyingPrice);
                dneutral.DisplayPortfolio();

                // Add a new position to hedge my current position.
                // I will write some call options, a little bit out-of-the money with an expiry date close to a year.
                var strike = underlyingPrice * 1.075;
                var daysToExpiry = 360;

                dneutral.UpdatePortfolio(IPAOption.DefineOTC("tag1", "Sell", "CALL", underlying, underlyingPrice, strike, daysToExpiry, style, 0));
                dneutral.DisplayPositions("The following strategy can be implemented:");

                // After 60 days, let's look at our position again - price increase of 5% in this time period
                underlyingPrice *= 1.05;
                var daysInFuture = 60;
                dneutral.ModelPosition("tag1", underlyingPrice, daysInFuture);
                dneutral.DisplayPositions($"With a simulated price increase to {underlyingPrice} and {daysInFuture} days in the future:");

                // Because we are no longer near delta neutral, I will sell some Put options with a strike 5% out-of-the money
                strike = underlyingPrice * 0.95;
                dneutral.UpdatePortfolio(IPAOption.DefineOTC("tag2", "Sell", "PUT", underlying, underlyingPrice, strike, daysToExpiry, style, daysInFuture));
                dneutral.DisplayPositions("Adding a new position with a strike 5% out-of-the money:");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n**************\nFailed to execute: {e.Message}\n{e.InnerException}\n***************");
            }
        }
    }
}
