using System.Text.Json.Serialization;

namespace SimpleTodo.Plugin.DTOs;
public class CreateTodoDTO
{

    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("description")]
    public string Description { get; set; }
}