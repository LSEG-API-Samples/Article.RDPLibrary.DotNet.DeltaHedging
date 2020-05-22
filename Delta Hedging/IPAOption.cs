using ConsoleTables;
using Newtonsoft.Json.Linq;
using Refinitiv.DataPlatform.Core;
using Refinitiv.DataPlatform.Delivery.Request;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeltaHedging
{
    public class IPAOption
    {
        private static readonly string EndpointUrl = "https://api.refinitiv.com/data/quantitative-analytics/v1/financial-contracts";
        private ISession Session { get; }
        
        public IPAOption(ISession session)
        {
            Session = session;
        }

        public IList<IDictionary<string, JToken>> PriceOptions(IEnumerable<JObject> options)
        {
            return PriceOptions(options.ToArray());
        }

        public IList<IDictionary<string, JToken>> PriceOptions(params JObject[] options)
        {
            // Our request will contain 1 or more options
            var request = new JObject()
            {
                ["fields"] = new JArray("InstrumentTag", "InstrumentCode", "ExerciseType", "ValuationDate",
                                        "EndDate", "StrikePrice", "OptionPrice", "DeltaPercent",
                                        "UnderlyingRIC", "UnderlyingPrice", "ExerciseStyle", "ErrorMessage"),
                ["universe"] = new JArray(options)
            };

            return ExtractValues(Endpoint.SendRequest(Session, EndpointUrl,
                                            new Endpoint.Request.Params().WithMethod(Endpoint.Request.Method.POST)
                                                                         .WithBodyParameters(request)));
        }

        // DefineETI
        // The IPA Contract interface allows the specification of an ETI (Exchange-Traded-Instrument) to price an 
        // existing option traded within the market.
        public static JObject DefineETI(string option, string buySell)
        {
            return new JObject()
            {
                ["instrumentType"] = "Option",
                ["instrumentDefinition"] = new JObject()
                {
                    ["instrumentTag"] = option,
                    ["instrumentCode"] = option,
                    ["underlyingType"] = "Eti",
                    ["buySell"] = buySell                   // 'Buy' or 'Sell'
                },
                ["pricingParameters"] = new JObject()
                {
                    ["underlyingTimeStamp"] = "Close"           // Use the Historical Close of the underlying to price the delta
                }
            };
        }

        // DefineOTC
        // The IPA Contract interface allows the specification of an OTC (Over-The-Counter) definition to model different 
        // scenarios to price options based on standard properties such as the strike and expiry.  In addition, the interface
        // supports other modeling properties such as price, valuation date and other capabilities to model what-if scenarios 
        // to estimate the Greeks.
        public static JObject DefineOTC(string key, string buySell, string callPut, string underlying, double underlyingPrice,
                                         double strike, int daysToExpire, string exerciseStyle, int daysInFuture)
        {
            return new JObject()
            {
                ["instrumentType"] = "Option",
                ["instrumentDefinition"] = new JObject()
                {
                    ["instrumentTag"] = key,
                    ["underlyingType"] = "Eti",
                    ["exerciseStyle"] = exerciseStyle,
                    ["strike"] = strike,
                    ["endDate"] = DateTime.Now.AddDays(daysToExpire),
                    ["buySell"] = buySell,
                    ["callPut"] = callPut,
                    ["underlyingDefinition"] = new JObject()
                    {
                        ["instrumentCode"] = underlying
                    }
                },
                ["pricingParameters"] = new JObject()
                {
                    ["underlyingPrice"] = underlyingPrice,
                    ["valuationDate"] = DateTime.Now.AddDays(daysInFuture),
                    ["underlyingTimeStamp"] = "Close",           // The Historical Close
                    ["volatilityType"] = "SVISurface"
                }
            };
        }

        // ExtractValues
        // The main Greek measurement used to drive our hedging is the Delta Percent value.  Based on the data response from our request
        // we pull out the output fields and extract the Delta value which will be used to drive our hedge.
        private static IList<IDictionary<string, JToken>> ExtractValues(IEndpointResponse response)
        {
            IList<IDictionary<string, JToken>> table = new List<IDictionary<string, JToken>>();

            if (response.IsSuccess)
            {
                var data = response.Data.Raw as JObject;
                var headers = data["headers"] as JArray;

                foreach (var option in data["data"])
                {
                    var result = new Dictionary<string, JToken>();

                    // Iterate through the headers and pull out the corresponding values
                    var index = 0;
                    foreach (var header in headers)
                        result.Add(header["name"].ToString(), option[index++]);

                    if (String.IsNullOrWhiteSpace(result["ErrorMessage"].ToString()))
                        table.Add(result);
                    else
                        Console.WriteLine($"Issue with Option. {option}");
                }
            }
            else
                Console.WriteLine(response.Status);

            return table;
        }

        // DisplayTable
        // Display the deltas and other properties from price extraction.
        public static void DisplayTable(IList<IDictionary<string, JToken>> table)
        {
            if (table.Count > 0)
            {
                var ct = new ConsoleTable(table.First().Keys.ToArray());

                foreach (var row in table)
                    ct.AddRow(row.Values.ToArray());

                ct.Write(Format.MarkDown);
            }
        }
    }
}
