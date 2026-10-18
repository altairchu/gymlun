using System.ComponentModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.Products;

/// <summary>
/// A file the person attached in the chat, as ChatGPT passes a tool parameter listed under `openai/fileParams`.
/// ChatGPT rejects a tool unless the parameter declares all four properties and requires only the first two
/// </summary>
[PublicAPI]
public record ChatFile(
    [property: JsonPropertyName("download_url"), Description("Temporary URL to download the file from.")]
    string DownloadUrl,
    [property: JsonPropertyName("file_id"), Description("Id of the file in the chat.")]
    string FileId)
{
    // Non-nullable with a default, so the schema shows a plain string ChatGPT accepts
    [JsonPropertyName("mime_type")]
    public string MimeType { get; init; } = "";

    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = "";
}
