using Newtonsoft.Json;
using System.Collections.Generic;

namespace Izkalauz_WPF.Models
{
    public class Recipe
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("howToText")]
        public string HowTo { get; set; } = string.Empty;

        // A heti tervezőhöz szükséges extra mező
        public string AssignedDay { get; set; } = string.Empty;
    }

    public class RecipeResponse
    {
        [JsonProperty("items")]
        public List<Recipe> Items { get; set; } = new List<Recipe>();
    }
}