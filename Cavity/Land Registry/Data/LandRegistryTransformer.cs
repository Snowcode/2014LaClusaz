namespace Example.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using Cavity;
    using Cavity.Collections;
    using Cavity.Data;

    public sealed class LandRegistryExtractionTransformer<T> : ITransformData<T>
        where T : KeyStringDictionary, new()
    {
        // ReSharper disable once StaticFieldInGenericType
        private static readonly string[] _address = "SAON,PAON,Street,Locality,District,Town,County".Split(',');

        public IEnumerable<T> Transform(IEnumerable<KeyStringDictionary> data)
        {
            return data.AsParallel().Select(Transform);
        }

        private static string Address(KeyStringDictionary entry)
        {
            var buffer = new MutableString();

            foreach (var value in _address.Where(column => entry.NotEmpty(column))
                                          .Select(column => entry[column]))
            {
                if (buffer.ContainsText())
                {
                    buffer.Append(',');
                }

                buffer.Append(value);
            }

            return buffer;
        }

        private static string NewBuild(string value)
        {
            switch (value)
            {
                case "Y":
                    return XmlConvert.ToString(true);
                case "N":
                    return XmlConvert.ToString(false);
            }

            throw new FormatException("{0} is an unknown old/new flag.".FormatWith(value));
        }

        private static string Price(string value)
        {
            var result = new MutableString();

            foreach (var c in value)
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        result.Append(c);
                        break;
                }
            }

            return result;
        }

        private static string PropertyType(string value)
        {
            switch (value)
            {
                case "D":
                    return "DETACHED";
                case "S":
                    return "SEMI DETACHED";
                case "T":
                    return "TERRACED";
                case "F":
                    return "FLAT/MAISONETTE";
            }

            throw new FormatException("{0} is an unknown property type.".FormatWith(value));
        }

        private static string RecordStatus(string value)
        {
            switch (value)
            {
                case "A":
                    return "ADDED";
                case "C":
                    return "CHANGED";
                case "D":
                    return "DELETED";
            }

            throw new FormatException("{0} is an unknown record status.".FormatWith(value));
        }

        private static string Subdivided(string value)
        {
            return XmlConvert.ToString(value.IsNotEmpty());
        }

        private static string Tenure(string value)
        {
            switch (value)
            {
                case "F":
                    return "FREEHOLD";
                case "L":
                    return "LEASEHOLD";
                case "U":
                    return string.Empty;
            }

            throw new FormatException("{0} is an unknown duration.".FormatWith(value));
        }

        private static T Transform(KeyStringDictionary entry)
        {
            var result = Activator.CreateInstance<T>();
            result.Add("UID", entry["Unique ID"]);
            result.Add("DAY", entry["date"].Substring(0, 10).ToDate());
            result.Add("ADDRESS", Address(entry));
            result.Add("POSTCODE", entry["Postcode"].RemoveAny(' ').ToPostcode());
            result.Add("PROPERTY TYPE", PropertyType(entry["Property type"]));
            result.Add("TENURE", Tenure(entry["Duration"]));
            result.Add("NEW BUILD", NewBuild(entry["old/new"]));
            result.Add("SUBDIVIDED", Subdivided(entry["SAON"]));
            result.Add("PRICE", Price(entry["Price"]));
            result.Add("RECORD", RecordStatus(entry["Record Status"]));

            return result;
        }
    }
}