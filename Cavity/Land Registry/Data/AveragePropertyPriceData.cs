namespace Example.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Cavity;
    using Cavity.Collections;
    using Cavity.Data;
    using Cavity.Models;

    public sealed class AveragePropertyPriceData : DataSheet
    {
        public AveragePropertyPriceData(FileInfo source)
        {
            Source = source;
        }

        private FileInfo Source { get; set; }

        protected override IEnumerator<T> GetEnumerator<T>()
        {
            return Entries<T>().GetEnumerator();
        }

        private IEnumerable<T> Entries<T>()
            where T : KeyStringDictionary, new()
        {
            var price = new Dictionary<string, decimal>();
            var tally = new Dictionary<string, int>();

            foreach (var entry in new CsvDataSheet(Source).Where(entry => entry.NotEmpty("POSTCODE"))
                                                          .Where(entry => entry.NotEmpty("PRICE")))
            {
                BritishPostcode postcode = entry["POSTCODE"];
                price.TryAdd(postcode.Area, 0m);
                price[postcode.Area] += XmlConvert.ToDecimal(entry["PRICE"]);

                tally.TryAdd(postcode.Area, 0);
                tally[postcode.Area]++;
            }

            return from item in price.OrderBy(item => item.Key.ToPostcode())
                   let average = Math.Round(price[item.Key] / tally[item.Key], 2, MidpointRounding.AwayFromZero)
                   select new T
                              {
                                  {"POSTAL AREA", item.Key},
                                  {"AVERAGE PRICE", XmlConvert.ToString(average)},
                                  {"TALLY", XmlConvert.ToString(tally[item.Key])}
                              };
        }
    }
}