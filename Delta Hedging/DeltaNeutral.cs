using ConsoleTables;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DeltaHedging
{
    public class DeltaNeutral
    {
        private IList<Position> Portfolio = new List<Position>();
        private IPAOption IPAOption { get; }

        public DeltaNeutral(IPAOption ipaOption, string underlying, double underlyingPrice)
        {
            IPAOption = ipaOption;
            Portfolio.Add(new Position()
            {
                Instrument = underlying,
                UnderlyingPrice = underlyingPrice,
                Type = "Long Shares",
                DeltaPct = 1.0,
                Contracts = 500
            });
        }

        public void DisplayPortfolio()
        {
            var table = new ConsoleTable("Instrument", "Close", "Shares", "Position", "Delta");

            foreach (var position in Portfolio)
                table.AddRow(position.Instrument, position.UnderlyingPrice, position.Contracts, position.Type, position.DeltaPct);

            table.Write(Format.MarkDown);
        }

        public void UpdatePortfolio(JObject otcOption)
        {
            // Retrieve Data for our specified positions...
            var options = IPAOption.PriceOptions(otcOption);

            if (options.Count > 0)
            {
                var data = options[0];

                var buysell = otcOption["instrumentDefinition"]?["buySell"].ToString();
                var expiry = data["EndDate"].ToObject<DateTime>();
                var days = (expiry - data["ValuationDate"].ToObject<DateTime>()).Days;
                var key = data["InstrumentTag"].ToString();
                var type = $"{days}d {(buysell == "Buy" ? "Long" : "Short")} {data["ExerciseType"].ToString()}";

                var position = FindPosition(key);
                if (position == null)
                {
                    var underlying = data["UnderlyingRIC"].ToString();
                    var strike = data["StrikePrice"].Value<double>();
                    var callput = data["ExerciseType"].ToString();

                    Portfolio.Add(new Position()
                    {
                        Key = key,
                        Underlying = underlying,
                        Instrument = $"OTC:{underlying}.{callput[0]}{strike:00000.00}",
                        Type = type,
                        ExerciseStyle = data["ExerciseStyle"].ToString(),
                        Strike = strike,
                        BuySell = buysell,
                        CallPut = callput,
                        DeltaPct = data["DeltaPercent"].ToObject<double>(),
                        DaysToExpire = days,
                        Contracts = 0
                    });
                }
                else
                {
                    position.Type = type;
                    position.DeltaPct = data["DeltaPercent"].ToObject<double>();
                }
            }

            BalancePositions(0);
        }

        private void BalancePositions(double exposure)
        {
            int initialPosition = 0;
            int positions = 0;

            foreach (var position in Portfolio)
            {
                // Determine the initial position
                if (position.Contracts > 0)
                {
                    position.Delta = (int)(position.DeltaPct * position.Contracts * (position.DeltaPct < 1 ? 100 : 1));
                    initialPosition += position.Delta;
                    positions++;
                }
                else
                {
                    var contractPosition = (initialPosition * (1 - exposure)) / (Portfolio.Count - positions);
                    var contracts = Math.Abs(Math.Round(contractPosition / (position.DeltaPct * 100)));
                    position.Contracts = (int)contracts;
                    position.Delta = (int)Math.Round(position.DeltaPct * contracts * 100);
                }
            }
        }

        public void ModelPosition(string key, double underlyingPrice, int daysInFuture)
        {
            var position = FindPosition(key);
            if (position != null)
            {
                UpdatePortfolio(IPAOption.DefineOTC(key, position.BuySell, position.CallPut, position.Underlying, underlyingPrice,
                                                    position.Strike, position.DaysToExpire, position.ExerciseStyle, daysInFuture));
            }
        }

        private Position FindPosition(string key)
        {
            foreach (var position in Portfolio)
            {
                if (position.Key?.CompareTo(key) == 0)
                    return position;
            }

            return null;
        }

        public void DisplayPositions(string title)
        {
            var table = new ConsoleTable("Instrument", "Strike", "Position", "Delta", "Contracts Traded", "Position Delta");
            var netDelta = 0;
            foreach (var position in Portfolio)
            {
                netDelta += position.Delta;
                table.AddRow(position.Instrument, (position.Strike > 0 ? $"{position.Strike:0.###}" : ""), position.Type,
                             $"{position.DeltaPct:0.###}", position.Contracts, position.Delta);
            }

            // Summary
            table.AddRow("", "", "", "", "", "______________");
            table.AddRow("", "", "", "", "Net Position Delta", netDelta);

            Console.WriteLine($"\n{title}\n");
            table.Write(Format.MarkDown);
        }
    }
}

internal class Position
{
    public string Key { get; set; }
    public string Instrument { get; set; }
    public string BuySell { get; set; }
    public string CallPut { get; set; }
    public string ExerciseStyle { get; set; }
    public string Type { get; set; }
    public double DeltaPct { get; set; }
    public int Contracts { get; set; }
    public int Delta { get; set; }
    public double Strike { get; set; }
    public string Underlying { get; set; }
    public double UnderlyingPrice { get; set; }
    public int DaysToExpire { get; set; }
}
