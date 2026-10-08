using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json; 
using System.Threading.Tasks;

namespace GachaSystem
{
    public class GachaItem
    {
        public string Name { get; set; }
        public string Rarity { get; set; }
        public double Weight { get; set; }

        public GachaItem(string name, string rarity, double prob)
        {
            Name = name;
            Rarity = rarity;
            Prob = prob;
        }
    }

   
    public class GachaResultPayload
    {
        public string Session { get; set; }
        public DateTime Timestamp { get; set; }
        public List<PulledItem> Items { get; set; } = new List<PulledItem>();
    }

    public class PulledItem
    {
        public int PullNumber { get; set; }
        public string Name { get; set; }
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
                new GachaItem("Excalibur (5-Star)", "Legendary", 0.6),
                new GachaItem("Dragon Shield (4-Star)", "Epic", 5.4),
                new GachaItem("Steel Sword (3-Star)", "Rare", 24.0),
                new GachaItem("Iron Dagger (2-Star)", "Common", 70.0)
            };

            // 3. Roll the 10 items and store them in our data payload
            var payload = new GachaResultPayload
            {
                SessionId = Guid.NewGuid().ToString(),
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
            string targetUrl = "https://your-api-endpoint.com";
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

            double roll = _random.NextDouble() * totalWeight;
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
