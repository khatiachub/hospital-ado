using hospital.models;
using Mailjet.Client;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Hospital.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly MailjetClient _client;
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string? _apiSecret;
        private readonly string _userName;

        public EmailService(IConfiguration config)
        {
            _config = config;
           // string apiKey = _config["Mailjet:ApiKey"];
         //   string apiSecret = _config["Mailjet:ApiSecret"];
            _userName = _config["Mailjet:UserName"];
          //  _client = new MailjetClient(apiKey, apiSecret);
            _httpClient = new HttpClient();
            _apiKey = _config["Mailjet:ApiKey"];
            _apiSecret = _config["Mailjet:ApiSecret"];

        }
        public async Task<bool> SendEmailAsync(EmailConfiguration emailConfig)
        {
            try
            {
                var requestUri = "https://api.mailjet.com/v3.1/send";
                var requestContent = new JObject
                {
                    ["Messages"] = new JArray
                    {
                new JObject
                {
                    ["From"] = new JObject
                    {
                        ["Email"] = _userName,
                        ["Name"] = "khatia"
                    },
                    ["To"] = new JArray
                    {
                        new JObject
                        {
                            ["Email"] = emailConfig.To,
                            ["Name"] = "Recipient Name"
                        }
                    },
                    ["Subject"] = emailConfig.Subject,
                    ["TextPart"] = emailConfig.Body,
                    ["HTMLPart"] = $"<p>{emailConfig.Body}</p>"
                }
                    }
                };
                var byteArray = Encoding.ASCII.GetBytes($"{_apiKey}:{_apiSecret}");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await _httpClient.PostAsync(requestUri, new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Mailjet API call failed: {responseBody}");
                }
                else
                {
                    return true; 
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception - Response: {ex.Message}");
                return false;
            }

        }

    }
}





