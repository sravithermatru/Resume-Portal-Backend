﻿using Newtonsoft.Json;
using System.Text;

namespace Resume_Selector_Page.Services
{
    public class GenAi_Service
    {

            private readonly HttpClient _httpClient;

            private readonly string _genAiApiUrl = "https://api.openai.com/v1/engines/davinci-codex/completions";

            private readonly string _apiKey;

            public GenAi_Service(IConfiguration configuration)

            {

                _httpClient = new HttpClient();

                _apiKey = configuration["GenAI:ApiKey"];

            }

            public async Task<string> SummarizeTextAsync(string text)

            {

                var requestBody = new

                {

                    prompt = $"Summarize the following text: {text}",

                    max_tokens = 100

                };

                var request = new HttpRequestMessage

                {

                    Method = HttpMethod.Post,

                    RequestUri = new Uri(_genAiApiUrl),

                    Headers =

            {

                { "Authorization", $"Bearer {_apiKey}" },

                { "Content-Type", "application/json" }

            },

                    Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")

                };

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<GenAIResponse>(responseBody);

                return result.choices.FirstOrDefault()?.text?.Trim();

            }

            private class GenAIResponse

            {

                public List<Choice> choices { get; set; }

            }

            private class Choice

            {

                public string text { get; set; }

            }

        }


    }
