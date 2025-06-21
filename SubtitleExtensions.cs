using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;

public static class SubtitleExtensions
{
    public class SubtitleEntry
    {
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; }
    }

    public static string ToSubtitleJsonList(this string subtitleBlock)
    {
        var result = new List<SubtitleEntry>();
        var lines = subtitleBlock.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        var regex = new Regex(@"\[(\d{2}):(\d{2}):(\d{2})\.(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2})\.(\d{3})\](.*)");

        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                double start = TimeToSeconds(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value);
                double end = TimeToSeconds(match.Groups[5].Value, match.Groups[6].Value, match.Groups[7].Value, match.Groups[8].Value);
                string text = match.Groups[9].Value.Trim();

                result.Add(new SubtitleEntry
                {
                    Start = start,
                    End = end,
                    Text = text
                });
            }
        }

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

    }

    private static double TimeToSeconds(string hh, string mm, string ss, string ms)
    {
        return int.Parse(hh) * 3600 + int.Parse(mm) * 60 + int.Parse(ss) + int.Parse(ms) / 1000.0;
    }
}