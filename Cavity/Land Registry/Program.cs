namespace Example
{
    using System.IO;
    using Cavity.IO;
    using Example.Data;

    public static class Program
    {
        public static void Main()
        {
            //// Download pp-complete.csv from http://www.landregistry.gov.uk/market-trend-data/public-data/price-paid-data
            var download = new FileInfo(@"C:\Development.Temp\pp-complete.csv");

            Aggregate(Extract(download));
        }

        private static void Aggregate(FileInfo source)
        {
            var destination = new FileInfo(@"C:\Development.Temp\aggregate.csv");

            TempCsvFile.Create(new AveragePropertyPriceData(source), destination);
        }

        private static FileInfo Extract(FileInfo source)
        {
            var destination = new FileInfo(@"C:\Development.Temp\extract.csv");

            TempCsvFile.Create(new LandRegistryData(source), destination);

            return destination;
        }
    }
}