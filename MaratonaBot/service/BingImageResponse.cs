namespace SimilarProducts.Services
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class BingImageResponse
    {
        [JsonProperty("_type")]
        public string Type { get; set; }
        [JsonProperty("visuallySimilarImages")]
        public ValueList<BingImageVisuallySimilarImage> VisuallySimilarImages { get; set; }

    }

    public class ValueList<T>
    {
        public List<T> Value { get; set; }
    }
}