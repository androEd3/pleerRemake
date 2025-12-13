using System.Text.Json;
using System.Text.Json.Serialization;

namespace pleer.Models.IA;

public class IaSearchResponse
{
    [JsonPropertyName("response")]
    public IaResponse Response { get; set; }
}

public class IaResponse
{
    [JsonPropertyName("docs")]
    public List<IaDoc> Docs { get; set; } = new();
}

public class IaDoc
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("title")]
    public JsonElement Title { get; set; }  // может быть string или array

    [JsonPropertyName("creator")]
    public JsonElement Creator { get; set; }  // может быть string или array

    [JsonPropertyName("year")]
    public JsonElement Year { get; set; }  // может быть int, string или array

    [JsonPropertyName("date")]
    public JsonElement Date { get; set; }  // может быть string или array

    [JsonPropertyName("subject")]
    public JsonElement Subject { get; set; }  // может быть string или array
}

public class IaMetadataResponse
{
    [JsonPropertyName("files")]
    public List<IaFile> Files { get; set; } = new();

    [JsonPropertyName("metadata")]
    public IaMetadata Metadata { get; set; }
}

public class IaFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonPropertyName("length")]
    public string Length { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("track")]
    public string Track { get; set; }
}

public class IaMetadata
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("title")]
    public JsonElement Title { get; set; }

    [JsonPropertyName("creator")]
    public JsonElement Creator { get; set; }

    [JsonPropertyName("year")]
    public JsonElement Year { get; set; }

    [JsonPropertyName("date")]
    public JsonElement Date { get; set; }

    [JsonPropertyName("subject")]
    public JsonElement Subject { get; set; }
}