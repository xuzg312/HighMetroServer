using System.Text.Json.Serialization;
using HighMetroServer.Parameters;

namespace HighMetroServer.Aot;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(AppConfig))]
public partial class AppConfigJsonContext : JsonSerializerContext
{
}