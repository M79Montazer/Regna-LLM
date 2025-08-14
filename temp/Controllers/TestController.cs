using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using temp.dto;
using temp.Service;
using temp.Service.Interface;

namespace temp.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TestController(IPromptMakerService promptMakerService,
        IHttpClientFactory httpFactory, IConfiguration config, IRetrieverService retrieverService, IHostEnvironment hostEnvironment) : ControllerBase
    {
        private readonly string _avalAiKey = config["AvalAi:ApiKey"] ?? throw new InvalidOperationException();

        [HttpGet]
        public async Task<IActionResult> Generate([FromQuery] string prompt, [FromQuery] string npcId, [FromQuery] string playerName = "")
        {
            try
            {
                Console.WriteLine($"\r\n Player asked '{prompt}' from NPC '{npcId}\r\n'");
                var finalPrompt = promptMakerService.MakePrompt(npcId, prompt);
                var payload = new
                {
                    //model = "gpt-4.1-mini",
                    model = "gemma-3-27b-it",
                    input = finalPrompt
                };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var client = httpFactory.CreateClient("AvalAi");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _avalAiKey);

                var response = await client.PostAsync("responses", content);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return StatusCode(502, $"AvalAI error: {response.StatusCode} {err}");
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var aiResponse = doc.RootElement.GetProperty("output")[0].GetProperty("content")[0].GetProperty("text");

                var jsonResponse = aiResponse.GetString()!;
                jsonResponse = Regex.Replace(jsonResponse, "```json", "");
                jsonResponse = Regex.Replace(jsonResponse, "```", "");
                using var childDoc = JsonDocument.Parse(jsonResponse);
                var root = childDoc.RootElement;

                var utterance = root.GetProperty("utterance").GetString()!;
                var action = root.GetProperty("action").GetString()!;

                var npcPrompt = ((PromptMakerService)promptMakerService).TempList.First(a => a.NpcId == npcId);


                npcPrompt.History += NpcPrompt.PlayerName + " : " + prompt + "\r\n" + npcPrompt.NpcName + ":" +
                                     utterance + "\r\n";

                Console.Write("\r\n\r\n*************************************************\r\n" + finalPrompt + utterance
                              + "\r\nactions:" + action);

                // Start background logging (fire-and-forget)
                if (hostEnvironment.IsProduction())
                {
                    _ = Task.Run(() => LogAsync(finalPrompt, utterance + "||" + action, npcId, playerName));
                }
                //await LogAsync(finalPrompt, utterance + "||" + action, npcId, playerIp);

                // 6) Return to RPG Maker
                return Ok(new { response = utterance, action });
            }
            catch (Exception e)
            {
                return Problem(detail: e.Message);
            }
        }

        [HttpGet]
        public void Initialize()
        {
            retrieverService.CreateDb("Content/cards.json");
            retrieverService.BuildFaissIndex();
        }

        [HttpGet]
        public void Test()
        {
            var a = new EmbeddingTest();
            a.Test();
        }

        [HttpGet]
        public void Reset()
        {
            ((PromptMakerService)promptMakerService).TempList.ForEach(a => a.History = "");
        }

        private async Task LogAsync(string prompt, string response, string? npcId, string? playerName)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

                var payload = new
                {
                    prompt = prompt ?? "",
                    response = response ?? "",
                    npcId = npcId ?? "",
                    playerName = playerName ?? "",
                    time = DateTime.UtcNow.ToString("o"),
                    key = config["LogServer:SecretKey"]
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                // It's OK to fire-and-forget; but we still log non-success for debugging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var res = await client.PostAsync(config["LogServer:Address"], content, cts.Token).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"[SheetLog] non-success: {res.StatusCode} body: {body}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SheetLog] exception: {ex.Message}");
            }
        }
    }
}
