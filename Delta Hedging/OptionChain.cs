using Newtonsoft.Json.Linq;
using Refinitiv.DataPlatform.Content;
using Refinitiv.DataPlatform.Content.Data;
using Refinitiv.DataPlatform.Core;
using Refinitiv.DataPlatform.Delivery;
using Refinitiv.DataPlatform.Delivery.Request;
using System;
using System.Collections.Generic;
using ConsoleTables;

namespace DeltaHedging
{
    internal class OptionChain
    {
        private const string ChainEndpointUrl = "/data/pricing/beta3/views/chains";

        private readonly IEndpoint endpoint;
        private string root;

        public string Underlying { get; internal set; }
        public double UnderlyingPrice { get; internal set; }

        public enum OptionType
        {
            None,
            Call,
            Put
        };

        public OptionChain(ISession session)
        {
            endpoint = DeliveryFactory.CreateEndpoint(new Endpoint.Params().Session(session)
                                                                           .Url(ChainEndpointUrl));                                                                               
        }

        internal IList<string> GetContracts(string chain, double pctAroundMoney, DateTime expiry)
        {
            IList<string> optionChain = new List<string>();
            IList<string> calls = new List<string>();
            IList<string> puts = new List<string>();

            var response = endpoint.SendRequest(new Endpoint.Request.Params().WithQueryParameter("universe", chain));
            if ( response.IsSuccess)
            {
                // Extract the constituents within the chain
                var constituents = response.Data.Raw as JObject;

                // Store within a list and filter out those based on the expiry / strike specification
                foreach (JToken constituent in constituents["data"]?["constituents"] as JArray)
                {
                    // An Option Chain has the 1st constituent as the underlying - we can use this to extract our root
                    if (root is null)
                    {
                        Underlying = constituent.ToString();
                        var split = constituent?.ToString().Split('.');
                        if (split.Length > 1)
                        {
                            root = split[0];
                            UnderlyingPrice = GetPrice(Underlying);
                        }
                        else
                        {
                            Console.WriteLine($"Specified chain item {chain} may not be an Option chain.  Ignoring.");
                            break;
                        }
                    }
                    else
                    {
                        // Validate the length of the constituent - a valid constituent should be at least: (len(root) + 10) bytes.
                        if ( constituent.ToString().Length < root.Length+10 )
                        {
                            Console.WriteLine($"Specified chain item {chain} may not be an Option chain.  Ignoring.");
                            break;
                        }

                        // The variance is a measure how far Around-The-Money the strike price should be filtered.
                        double variance = UnderlyingPrice * pctAroundMoney;

                        if (KeepConstituent(constituent.ToString(), expiry, variance, out OptionType type))
                        {
                            switch (type)
                            {
                                case OptionType.Call:
                                    calls.Add(constituent.ToString());
                                    break;
                                case OptionType.Put:
                                    puts.Add(constituent.ToString());
                                    break;
                            }
                        }
                    }
                }

                // Create out final option chain
                foreach (var put in puts)
                    optionChain.Add(put);
                foreach (var call in calls)
                    optionChain.Add(call);
            }

            return optionChain;
        }

        // GetPrice
        // Retrieve the price for the specified ric.  The price retrieved will be based on the current price.  If that 
        // is unavailable, the historical price will be returned.
        private double GetPrice(string ric)
        {
            double? result = null;
            var response = Pricing.GetSnapshot(new PricingSnapshot.Params().Universe(ric)
                                                                           .WithFields("TRDPRC_1", "HST_CLOSE"));
            if (response.IsSuccess)
            {
                var value = response.Data.Prices[ric]["TRDPRC_1"];
                if ( value?.Type == JTokenType.Null )
                {
                    value = response.Data.Prices[ric]["HST_CLOSE"];
                    if (value.Type == JTokenType.Null)
                        return 0.0;
                }
                result = value?.ToObject<double>();
            }

            return result is null ? 0.0 : result.Value;
        }

        // KeepConstituent
        // Based on the filtering parameters, expiry and variance of the strike price, determine if 
        // the specified constituent is one that we want to keep.
        //
        // A proper constituent uses the following format:
        //
        // <root><month code><day><year><strike>.U  where:
        //      <root>          - Root of our underlying, Eg: AAPL
        //      <month code>    - Single character expiration month code, Eg: 'D' (April Call)
        //      <day>           - 2 digit expiration day, Eg: 15
        //      <year>          - 2 digit expiration year, Eg: 21  (2021)
        //      <strike>        - 5 digit expiration XXX.XX, Eg: 255000 (255.00)
        //
        private bool KeepConstituent(string constituent, DateTime expiry, double variance, out OptionType optionType)
        {
            // *********************************************************
            // Parse constituent name for the parts and apply filters
            // *********************************************************
            optionType = OptionType.None;

            // First, determine the root.
            var index = constituent.IndexOf(root);
            if (index < 0) return false;    // Looks like this may not be an Option Chain
            index += root.Length;

            // Next, get the month code
            char month = constituent[index];
            index += 3;

            // Then the year
            var year = constituent.Substring(index, 2);

            // Verify if the month/year is our expiry
            if (expiry.ToString("yMM") != $"{year}{Month(month, out optionType):00}")
                return false;

            // Retrieve the strike and verify if we're Around-The-Money
            var strike = Convert.ToDouble(constituent.Substring(index + 2, 5)) / 100;

            // Filter out the strikes, based on the type of contract
            switch (optionType)
            {
                case OptionType.Put:
                    if (strike > UnderlyingPrice || strike < UnderlyingPrice - variance)
                        return false;
                    break;
                case OptionType.Call:
                    if (strike < UnderlyingPrice || strike > UnderlyingPrice + variance)
                        return false;
                    break;
                default:
                    return false;
            }

            // We found a constituent for our option chain
            return true;
        }

        // Month
        // Retrieve the actual month, based on the month code.  In addition, determine the type of option based
        // on the month code.
        private int Month(char monthCode, out OptionType type)
        {
            type = monthCode < 'M' ? OptionType.Call : OptionType.Put;

            switch (monthCode)
            {
                case 'A':
                case 'M':
                    return 1;
                case 'B':
                case 'N':
                    return 2;
                case 'C':
                case 'O':
                    return 3;
                case 'D':
                case 'P':
                    return 4;
                case 'E':
                case 'Q':
                    return 5;
                case 'F':
                case 'R':
                    return 6;
                case 'G':
                case 'S':
                    return 7;
                case 'H':
                case 'T':
                    return 8;
                case 'I':
                case 'U':
                    return 9;
                case 'J':
                case 'V':
                    return 10;
                case 'K':
                case 'W':
                    return 11;
                case 'L':
                case 'X':
                    return 12;
                default:
                    type = OptionType.None;
                    return 0;
            }
        }


        // DisplayTable
        // Display the deltas and other properties from price extraction.
        public static void DisplayOptionChain(IList<IDictionary<string, JToken>> table)
        {
            if (table.Count > 0)
            {
                var console = new ConsoleTable("Option", "Type", "Expiry", "Strike", "Option Price", "Delta", "Underlying", "Style");
                foreach (var row in table)
                {
                    console.AddRow(row["InstrumentCode"], row["ExerciseType"], row["EndDate"].ToObject<DateTime>().ToString("dd-MMMM-yyyy"),
                                   $"{row["StrikePrice"],8:0.00}", $"{row["OptionPrice"],10:0.00}",
                                   $"{row["DeltaPercent"],6:0.00}", row["UnderlyingRIC"], row["ExerciseStyle"]);
                }

                console.Write(Format.MarkDown);
            }
        }
    }
}