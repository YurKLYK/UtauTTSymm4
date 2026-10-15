using System.IO;
using System.Text.Json;

namespace UTAUTalkVoice;

public sealed class MoraPitchPoint
{
    public int Position { get; set; }
    public string Mora { get; set; } = "";
    public double Cents { get; set; }
}

public sealed class MoraPitchDocument
{
    public string Text { get; set; } = "";
    public string Reading { get; set; } = "";
    public List<MoraPitchPoint> Points { get; set; } = [];

    public string ManualPitchJson()
    {
        if (Points.Any(p => p.Position < 0 || !double.IsFinite(p.Cents) || Math.Abs(p.Cents) > 1200)
            || Points.Select(p => p.Position).Distinct().Count() != Points.Count)
            throw new InvalidDataException("音程補正は-1200～1200セントの範囲で指定してください");
        return JsonSerializer.Serialize(new {
            version = 1, mode = "offset", reading = Reading,
            points = Points.Select(p => new { position = p.Position, mora = p.Mora, cents = p.Cents }),
        });
    }

    public static MoraPitchDocument FromPlan(string text, string json)
    {
        using var plan = JsonDocument.Parse(json);
        return new MoraPitchDocument {
            Text = text,
            Reading = plan.RootElement.GetProperty("reading").GetString() ?? "",
            Points = plan.RootElement.GetProperty("units").EnumerateArray()
                .Where(u => u.GetProperty("role").GetString() == "mora")
                .Select(u => new MoraPitchPoint {
                    Position = u.GetProperty("position").GetInt32(),
                    Mora = u.GetProperty("mora").GetString() ?? "",
                }).DistinctBy(p => p.Position).ToList(),
        };
    }
}
