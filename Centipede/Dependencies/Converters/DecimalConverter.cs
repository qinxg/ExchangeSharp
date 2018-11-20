using System;
using System.Collections.Generic;
using System.Text;using Newtonsoft.Json;

namespace Centipede
{
    public class DecimalConverter : JsonConverter
    {
        private static readonly Type decimalType = typeof(decimal);

        public override bool CanConvert(Type objectType)
        {
            return (objectType == decimalType);
        }

        public override void WriteJson(JsonWriter writer, object value,
                                       JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToStringInvariant());
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType,
                                     object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public static DecimalConverter Instance { get; } = new DecimalConverter();
    }
}
