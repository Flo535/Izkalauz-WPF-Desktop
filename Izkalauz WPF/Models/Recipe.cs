using Newtonsoft.Json;
using System.Collections.Generic;

namespace Izkalauz_WPF.Models
{
    public class Recipe
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("title")] public string Name { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("category")] public string Category { get; set; } = string.Empty;
        [JsonProperty("howToText")] public string HowTo { get; set; } = string.Empty;
        [JsonProperty("imageUrl")] public string ImageUrl { get; set; } = string.Empty;

        // EZ HIÁNYZOTT:
        [JsonProperty("ingredients")]
        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

        public string AssignedDay { get; set; } = string.Empty;
    }

    // EZ IS KELL, HOGY A HOZZÁVALÓK ADATAIT KEZELNI TUDJA:
    public class Ingredient
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("quantity")] public double Quantity { get; set; }
        [JsonProperty("unit")] public string Unit { get; set; } = string.Empty;
    }

    public class RecipeResponse
    {
        [JsonProperty("items")]
        public List<Recipe> Items { get; set; } = new List<Recipe>();
    }
}