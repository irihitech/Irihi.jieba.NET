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
    public static IDictionary<char, IDictionary<char, double>> DeserializeProbTable(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.CharProbTable);
    }

    public static IDictionary<string, double> DeserializePosProbStart(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.StringProbTable);
    }

    public static IDictionary<string, IDictionary<string, double>> DeserializePosProbTrans(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.StringTransTable);
    }

    public static IDictionary<string, IDictionary<char, double>> DeserializePosProbEmit(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.StringCharTable);
    }

    public static IDictionary<char, List<string>> DeserializeCharStateTab(string json)
    {
        return JsonSerializer.Deserialize(json, JiebaJsonContext.Default.CharStateTab);
    }
}

[JsonSerializable(typeof(IDictionary<char, IDictionary<char, double>>), TypeInfoPropertyName = "CharProbTable")]
[JsonSerializable(typeof(IDictionary<string, double>), TypeInfoPropertyName = "StringProbTable")]
[JsonSerializable(typeof(IDictionary<string, IDictionary<string, double>>), TypeInfoPropertyName = "StringTransTable")]
[JsonSerializable(typeof(IDictionary<string, IDictionary<char, double>>), TypeInfoPropertyName = "StringCharTable")]
[JsonSerializable(typeof(IDictionary<char, List<string>>), TypeInfoPropertyName = "CharStateTab")]
internal partial class JiebaJsonContext : JsonSerializerContext
{
}