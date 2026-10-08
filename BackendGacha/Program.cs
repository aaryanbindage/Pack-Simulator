using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json; 
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace GachaSystem
{
    public class GachaItem
    {
        public string Name { get; set; }
        public string Rarity { get; set; }
        public double Prob { get; set; }

        public GachaItem(string name, string rarity, double prob)
        {
            Name = name;
            Rarity = rarity;
            Prob = prob;
        }
    }

   
    public class GachaResultPayload
{
    [JsonPropertyName("Session")] // Forces the JSON key to be exactly "Session"
    public string Session { get; set; }

    [JsonPropertyName("Timestamp")] // Forces the JSON key to be exactly "Timestamp"
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("Items")]     // Forces the JSON key to be exactly "Items"
    public List<PulledItem> Items { get; set; } = new List<PulledItem>();
}

public class PulledItem
{
    [JsonPropertyName("PullNumber")]
    public int PullNumber { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Rarity")]
    public string Rarity { get; set; }
}

    class Program
    {
        private static readonly Random _random = new Random();
        // 2. Reuse HttpClient instance per best practices to avoid socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient();

        // Note the change to 'async Task' to allow for web requests
        static async Task Main(string[] args)
        {
            List<GachaItem> itemPool = new List<GachaItem>
            {
                new GachaItem("Hack Grail", "Legendary", 0.6),
                new GachaItem("Coin Of Glory", "Epic", 3.4),
                new GachaItem("Golden Coin", "Awesome(Almost Legendary!)", 2.0),
                new GachaItem("Relic Coin", "Rare", 24.0),
                new GachaItem("Milled Coin", "Common", 70.0)
            };

            // 3. Roll the 10 items and store them in our data payload
            var payload = new GachaResultPayload
            {
                Session = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            Console.WriteLine("=== Commencing 10-Pull Gacha Draw ===");
            for (int i = 1; i <= 10; i++)
            {
                GachaItem drawnItem = DrawItem(itemPool);
                Console.WriteLine($"Pull #{i:D2}: [{drawnItem.Rarity}] {drawnItem.Name}");

                payload.Items.Add(new PulledItem()
                {
                    PullNumber = i,
                    Name = drawnItem.Name,
                    Rarity = drawnItem.Rarity
                });
            }

            // 4. Send the results to your endpoint
            string targetUrl = "https://pack-simulator.onrender.com/api/gacha/results";    
            Console.WriteLine($"\nSending results to {targetUrl}...");
            
            await SendGachaResultsAsync(targetUrl, payload);
        }

        private static async Task SendGachaResultsAsync(string url, GachaResultPayload payload)
        {
            try
            {
                
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Successfully sent gacha data to endpoint!");
                }
                else
                {
                    Console.WriteLine($"Failed to send data. Server Status Code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending the web request: {ex.Message}");
            }
        }

        public static GachaItem DrawItem(List<GachaItem> pool)
        {
            double totalBoundary = 0;
            foreach (var item in pool) totalBoundary += item.Prob;

            double roll = _random.NextDouble() * totalBoundary;
            double RollingBound = 0;

            foreach (var item in pool)
            {
                RollingBound += item.Prob;
                if (roll <= RollingBound) return item;
            }
            return pool[pool.Count - 1];
        }
    }
}
