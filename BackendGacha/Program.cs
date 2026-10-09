using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Establish the single HttpClient instance for internal communication
HttpClient httpClient = new HttpClient();
string jsBackendUrl = Environment.GetEnvironmentVariable("GACHA_BACKEND_URL") 
                      ?? "https://pack-simulator.onrender.com";

// Define the available Gacha Items securely on the server
 List<GachaItem> itemPool = new List<GachaItem>
{
    new GachaItem("Hack Grail", "Legendary", 0.6),
    new GachaItem("Coin Of Glory", "Epic", 3.4),
    new GachaItem("Golden Coin", "Awesome(Almost Legendary!)", 2.0),
    new GachaItem("Relic Coin", "Rare", 24.0),
    new GachaItem("Milled Coin", "Common", 70.0)
};

Random random = new Random();

// 1. ENDPOINT: Real users send a POST request here to execute a roll
app.MapPost("/api/gacha/pull", async ([FromBody] UserPullRequest request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest("A valid Username is required to pull.");
    }

    // 2. Generate the drops completely on the server side (No client-side manipulation)
    var payload = new GachaResultPayload
    {
        Username = request.Username,
        Session = Guid.NewGuid().ToString(),
        Timestamp = DateTime.UtcNow
    };

    for (int i = 1; i <= 10; i++)
    {
        GachaItem drawnItem = GachaEngine.DrawItem(itemPool, random);
        payload.Items.Add(new PulledItemDto
        {
            PullNumber = i,
            Name = drawnItem.Name,
            Rarity = drawnItem.Rarity
        });
    }

    try
    {
        // 3. Forward the finalized results down to your JavaScript Node/Supabase pipeline
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(jsBackendUrl, payload);
        
        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode(500); // Server-to-server logging failed
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to contact database log engine: {ex.Message}");
    }

    // 4. Return the pulled items directly back to the player who requested them
    return Results.Ok(payload);
});

// Configure the app to listen on the exact port Render specifies dynamically
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0:{port}");

// ==========================================
// Data Structures
// ==========================================
public class UserPullRequest
{
    [JsonPropertyName("Username")]
    public string Username { get; set; }
}

public class GachaResultPayload
{
    [JsonPropertyName("Username")] public string Username { get; set; }
    [JsonPropertyName("Session")] public string Session { get; set; }
    [JsonPropertyName("Timestamp")] public DateTime Timestamp { get; set; }
    [JsonPropertyName("Items")] public List<PulledItemDto> Items { get; set; } = new();
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

public static class GachaEngine
{
    public static GachaItem DrawItem(List<GachaItem> pool, Random rng)
    {
        double totalWeight = 0;
        foreach (var item in pool) totalWeight += item.Weight;
        double roll = rng.NextDouble() * totalWeight;
        double cumulativeWeight = 0;
        foreach (var item in pool)
        {
            cumulativeWeight += item.Weight;
            if (roll <= cumulativeWeight) return item;
        }
        return pool[pool.Count - 1];
    }
}
