using System.Text.Json;
using Mscc.GenerativeAI;
using RoboTube;

namespace mrmoneyman
{
    public static class Brain_Gemini
    {
        public static async Task<VideoSegment> TalkWithGemini(string videoTitle)
        {
            var jsonFilePath = GeneralSettings.GetTranscriptByVideoTitle(videoTitle);
            var json = await File.ReadAllTextAsync(jsonFilePath);

            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            var systemInstructionString = Path.Combine(Directory.GetCurrentDirectory(), "Gemini", "system_instruction.txt");
            var systemInstruction = new Content(File.ReadAllText(systemInstructionString));

            IGenerativeAI genAi = new GoogleAI(apiKey);
            var model = genAi.GenerativeModel(Model.Gemini15Flash, systemInstruction: systemInstruction);
            var request = new GenerateContentRequest(json);

            var response = await model.GenerateContent(request);

            if (response == null)
            {
                Console.WriteLine("Gemini API hatası");
                return null;
            }
            if (response.Text == null)
            {
                Console.WriteLine("Gemini API Hatası:");
                return null;
            }

            var clearJson = response.Text.ToVideoSegment();
            File.WriteAllText(GeneralSettings.GetGeminiJsonPath(videoTitle), clearJson);

            return JsonSerializer.Deserialize<VideoSegment>(clearJson);
        }

        public static string ToVideoSegment(this string json)
        {
            // Gerekirse baştaki ```json ve sondaki ``` işaretlerini kaldır
            string cleanJson = json.Trim().Trim('`');
            if (cleanJson.StartsWith("json"))
            {
                cleanJson = cleanJson.Substring(4).Trim();
            }

            return cleanJson;
        }
    }


}

