using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace EbayWpfUploader
{
    public class EbayListingManager
    {
        private readonly string clientId = "YOUR_SANDBOX_CLIENT_ID";
        private readonly string clientSecret = "YOUR_SANDBOX_CLIENT_SECRET";
        private string accessToken;

        public async Task InitializeAsync()
        {
            accessToken = await GetAccessTokenAsync();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);

                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "https://api.ebay.com/oauth/api_scope/sell.inventory https://api.ebay.com/oauth/api_scope/sell.account")
                });

                var response = await client.PostAsync("https://api.sandbox.ebay.com/identity/v1/oauth2/token", body);
                string json = await response.Content.ReadAsStringAsync();
                dynamic token = JsonConvert.DeserializeObject(json);
                return token.access_token;
            }
        }

        public async Task CreateListingsAsync(List<(string sku, string title, string imagePath)> items,
                                              string paymentPolicyId,
                                              string returnPolicyId,
                                              string fulfillmentPolicyId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                foreach (var item in items)
                {
                    string imageUrl = await UploadImageAsync(item.imagePath);

                    var inventoryItem = new
                    {
                        product = new
                        {
                            title = item.title,
                            description = "WPF-generated listing from sandbox",
                            aspects = new { Brand = new[] { "CopilotBrand" } },
                            mpn = "SKU-MPN-" + item.sku,
                            imageUrls = new[] { imageUrl }
                        },
                        condition = "NEW",
                        availability = new
                        {
                            shipToLocationAvailability = new { quantity = 10 }
                        }
                    };

                    var inventoryUrl = $"https://api.sandbox.ebay.com/sell/inventory/v1/inventory_item/{item.sku}";
                    var invPayload = new StringContent(JsonConvert.SerializeObject(inventoryItem), Encoding.UTF8, "application/json");
                    await client.PutAsync(inventoryUrl, invPayload);

                    var offer = new
                    {
                        sku = item.sku,
                        marketplaceId = "EBAY_US",
                        format = "FIXED_PRICE",
                        availableQuantity = 10,
                        listingDescription = "Test listing from WPF app",
                        pricingSummary = new { price = new { value = "25.00", currency = "USD" } },
                        listingPolicies = new
                        {
                            paymentPolicyId,
                            returnPolicyId,
                            fulfillmentPolicyId
                        }
                    };

                    var offerPayload = new StringContent(JsonConvert.SerializeObject(offer), Encoding.UTF8, "application/json");
                    var offerResponse = await client.PostAsync("https://api.sandbox.ebay.com/sell/inventory/v1/offer", offerPayload);
                    string offerResult = await offerResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"✅ Created offer for {item.sku}: {offerResult}");
                }
            }
        }

        private async Task<string> UploadImageAsync(string imagePath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                byte[] bytes = File.ReadAllBytes(imagePath);

                using (var content = new MultipartFormDataContent())
                {
                    using (var image = new ByteArrayContent(bytes))
                    {
                        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                        content.Add(image, "file", Path.GetFileName(imagePath));

                        var response = await client.PostAsync("https://api.sandbox.ebay.com/sell/media/v1/media_item/upload", content);
                        string result = await response.Content.ReadAsStringAsync();
                        dynamic json = JsonConvert.DeserializeObject(result);
                        return json.image.imageUrl;
                    }
                }
            }
        }
    }
}
