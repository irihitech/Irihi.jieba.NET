using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JiebaNet.Segmenter.Common;

/// <summary>
/// JSON helper backed by System.Text.Json source generation, so that model loading
/// works in trimmed and NativeAOT published applications.
/// </summary>
internal static class JsonHelper
{
    public static Dictionary<char, Dictionary<char, double>> DeserializeProbTable(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.CharProbTable) ??
               new Dictionary<char, Dictionary<char, double>>();
    }

    public static Dictionary<string, double> DeserializePosProbStart(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.StringProbTable) ??
               new Dictionary<string, double>();
    }

    public static Dictionary<string, Dictionary<string, double>> DeserializePosProbTrans(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.StringTransTable) ??
               new Dictionary<string, Dictionary<string, double>>();
    }

    public static Dictionary<string, Dictionary<char, double>> DeserializePosProbEmit(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.StringCharTable) ??
               new Dictionary<string, Dictionary<char, double>>();
    }

    public static Dictionary<char, List<string>> DeserializeCharStateTab(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.CharStateTab) ??
               new Dictionary<char, List<string>>();
    }
}

[JsonSerializable(typeof(Dictionary<char, Dictionary<char, double>>), TypeInfoPropertyName = "CharProbTable")]
[JsonSerializable(typeof(Dictionary<string, double>), TypeInfoPropertyName = "StringProbTable")]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, double>>), TypeInfoPropertyName = "StringTransTable")]
[JsonSerializable(typeof(Dictionary<string, Dictionary<char, double>>), TypeInfoPropertyName = "StringCharTable")]
[JsonSerializable(typeof(Dictionary<char, List<string>>), TypeInfoPropertyName = "CharStateTab")]
internal partial class JiebaJsonContext : JsonSerializerContext
{
}