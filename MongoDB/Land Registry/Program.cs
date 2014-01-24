namespace Example
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Cavity;
    using Cavity.Collections;
    using Cavity.Data;
    using Cavity.IO;
    using Cavity.Models;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.Linq;

    public static class Program
    {
        private static MongoDatabase Database
        {
            get
            {
                var client = new MongoClient("mongodb://localhost");
                var server = client.GetServer();
                return server.GetDatabase("Land_Registry");
            }
        }

        public static void Main()
        {
            ////Load();

            ////var destination = new FileInfo(@"C:\Development.Temp\query.csv");
            ////TempCsvFile.Create(Query(), destination);

            var destination = new FileInfo(@"C:\Development.Temp\test.csv");
            TempCsvFile.Create(Aggregate(), destination);
        }

        private static IEnumerable<BsonDocument> Entries()
        {
            var count = 0;
            var file = new FileInfo(@"C:\Development.Temp\extract.csv");
            foreach (var entry in new CsvDataSheet(file))
            {
                count++;
                if (0 == count % 100000)
                {
                    Console.WriteLine("{0:0,0}".FormatWith(count));
                }

                BritishPostcode postcode = entry["POSTCODE"];
                var result = new BsonDocument
                                 {
                                     {"UID", BsonValue.Create(Guid.Parse(entry["UID"]))},
                                     {"DAY", BsonValue.Create(entry["DAY"].ToDate().ToDateTime())},
                                     {"ADDRESS", BsonValue.Create(entry["ADDRESS"])},
                                     {"POSTCODE", BsonValue.Create((string)postcode)},
                                     {"TENURE", BsonValue.Create(entry["TENURE"])},
                                     {"NEW BUILD", BsonValue.Create(entry.NotEmpty("NEW BUILD"))},
                                     {"SUBDIVIDED", BsonValue.Create(entry.NotEmpty("SUBDIVIDED"))},
                                     {"PRICE", BsonValue.Create(entry.Empty("PRICE") ? 0 : XmlConvert.ToInt32(entry["PRICE"]))},
                                 }.AddRange(new BsonDocument
                                                {
                                                    {"POSTAL AREA", BsonValue.Create(postcode.Area)},
                                                    {"POSTAL DISTRICT", BsonValue.Create(postcode.District)},
                                                    {"POSTAL SECTOR", BsonValue.Create(postcode.Sector)},
                                                });
                yield return result;
            }
        }

        private static void Load()
        {
            var database = Database;
            ////database.DropCollection("PRICE PAID");
            ////server.DropDatabase("Land_Registry");
            if (!database.CollectionExists("PRICE PAID"))
            {
                database.CreateCollection("PRICE PAID");
            }

            var collection = database.GetCollection("PRICE PAID");
            var options = new MongoInsertOptions
                              {
                                  WriteConcern = WriteConcern.Acknowledged,
                              };
            collection.InsertBatch(Entries(), options);
        }

        private static IEnumerable<KeyStringDictionary> Query()
        {
            var total = new Dictionary<string, decimal>();
            var tally = new Dictionary<string, int>();

            var database = Database;
            var documents = new MongoQueryable<BsonDocument>(new MongoQueryProvider(database.GetCollection("PRICE PAID")));
            foreach (var document in documents)
            {
                BritishPostcode postcode = document.GetValue("POSTCODE").AsString;
                if (postcode.Unit.IsEmpty())
                {
                    continue;
                }

                var price = document.GetValue("PRICE").AsInt32;
                if (price.Is(0))
                {
                    continue;
                }

                total.TryAdd(postcode.Area, 0m);
                total[postcode.Area] += price;

                tally.TryAdd(postcode.Area, 0);
                tally[postcode.Area]++;
            }

            return from item in total.OrderBy(item => item.Key.ToPostcode())
                   let average = Math.Round(total[item.Key] / tally[item.Key], 2, MidpointRounding.AwayFromZero)
                   select new KeyStringDictionary
                              {
                                  {"POSTAL AREA", item.Key},
                                  {"AVERAGE PRICE", XmlConvert.ToString(average)},
                                  {"TALLY", XmlConvert.ToString(tally[item.Key])}
                              };
        }

        private static IEnumerable<KeyStringDictionary> Aggregate()
        {
            var database = Database;
            var documents = new MongoQueryable<BsonDocument>(new MongoQueryProvider(database.GetCollection("PRICE PAID")));
            foreach (var grp in documents.GroupBy(document => document.GetValue("POSTCODE").AsString.ToPostcode().Area))
            {
                yield return new KeyStringDictionary
                                 {
                                     {"POSTAL AREA", grp.Key},
                                     {"AVERAGE PRICE", XmlConvert.ToString(grp.Average(x => x.GetValue("PRICE").AsInt32) / grp.Count())},
                                     {"TALLY", XmlConvert.ToString(grp.Count())}
                                 };
            }
        }
    }
}