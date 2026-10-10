using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GachaSystem
{
    public class Program
    {
        // 1. Declare global, static variables so they are accessible everywhere inside the app
        private static readonly Random _random = new Random();
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        
        private static readonly List<GachaItem> _itemPool = new List<GachaItem>
        {
            new GachaItem("Hack Grail", "Legendary", 0.6),
            new GachaItem("Coin Of Glory", "Epic", 3.4),
            new GachaItem("Golden Coin", "Awesome(Almost Legendary!)", 2.0),
            new GachaItem("Relic Coin", "Rare", 24.0),
            new GachaItem("Milled Coin", "Common", 70.0)
        };

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            string jsBackendUrl = Environment.GetEnvironmentVariable("GACHA_BACKEND_URL") 
                                  ?? "https://onrender.com";

            // 2. Define your endpoint route cleanly
            app.MapPost("/api/gacha/pull", async ([FromBody] UserPullRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Username))
                {
                    return Results.BadRequest("A valid Username is required to pull.");
                }

                var payload = new GachaResultPayload
                {
                    Username = request.Username,
                    Session = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow
                };

                // 3. Securely roll 10 items using the global static pool and random reference
                for (int i = 1; i <= 10; i++)
                {
                    GachaItem drawnItem = DrawItem(_itemPool);
                    payload.Items.Add(new PulledItemDto
                    {
                        PullNumber = i,
                        Name = drawnItem.Name,
                        Rarity = drawnItem.Rarity
                    });
                }

                try
                {
                    // Forward results down to your JavaScript logging pipeline
                    HttpResponseMessage response = await _httpClient.PostAsJsonAsync(jsBackendUrl, payload);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ JS Backend returned an error code: {response.StatusCode}");
                        return Results.StatusCode(500); 
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to communicate with JS Backend: {ex.Message}");
                    return Results.StatusCode(500);
                }

                return Results.Ok(payload);
            });

            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            app.Run($"http://0.0.0:{port}");
        }

        // 4. Probability calculation method marked as static
        public static GachaItem DrawItem(List<GachaItem> pool)
        {
            double totalWeight = 0;
            foreach (var item in pool) totalWeight += item.Weight;

            double roll = _random.NextDouble() * totalWeight;
            double cumulativeWeight = 0;

            foreach (var item in pool)
            {
                cumulativeWeight += item.Weight;
                if (roll <= cumulativeWeight) return item;
            }
            return pool[pool.Count - 1];
        }
    }

    // ==========================================
    // Core Data Models
    // ==========================================
    public class UserPullRequest
    {
        [JsonPropertyName("Username")] public string Username { get; set; }
    }

    public class GachaResultPayload
    {
        [JsonPropertyName("Username")] public string Username { get; set; }
        [JsonPropertyName("Session")] public string Session { get; set; }
        [JsonPropertyName("Timestamp")] public DateTime Timestamp { get; set; }
        [JsonPropertyName("Items")] public List<PulledItemDto> Items { get; set; } = new List<PulledItemDto>();
    }

    public class PulledItemDto
    {
        [JsonPropertyName("PullNumber")] public int PullNumber { get; set; }
        [JsonPropertyName("Name")] public string Name { get; set; }
        [JsonPropertyName("Rarity")] public string Rarity { get; set; }
    }

    public class GachaItem
    {
        public string Name { get; set; }
        public string Rarity { get; set; }
        public double Weight { get; set; }
        public GachaItem(string name, string rarity, double weight) { Name = name; Rarity = rarity; Weight = weight; }
    }
}
