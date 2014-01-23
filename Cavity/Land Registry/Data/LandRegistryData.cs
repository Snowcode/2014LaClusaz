namespace Example.Data
{
    using System.Collections.Generic;
    using System.IO;
    using Cavity.Collections;
    using Cavity.Data;
    using Cavity.IO;

    public sealed class LandRegistryData : DataSheet
    {
        public LandRegistryData(FileInfo source)
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
            using (var temp = new CurrentTempDirectory())
            {
                var file = temp.Info.ToFile("data.csv");
                using (var writers = new StreamWriterDictionary("Unique ID,Price,date,Postcode,Property type,old/new,Duration,PAON,SAON,Street,Locality,Town,District,County,Record Status"))
                {
                    foreach (var line in Source.Lines())
                    {
                        writers.Item(file).WriteLine(line);
                    }
                }

                var transformer = new LandRegistryExtractionTransformer<T>();
                foreach (var entry in new CsvDataSheet(file).Transform(transformer))
                {
                    yield return entry;
                }
            }
        }
    }
}