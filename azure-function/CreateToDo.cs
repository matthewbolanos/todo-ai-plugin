using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Models;

namespace SimpleTodo.Plugin;

public class CreateTodo
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    [Function("CreateTodo")]
    [OpenApiOperation(operationId: "CreateTodo", tags: new[] { "ExecuteFunction" }, Description = "Creates a new to do item in the to do list")]
    [OpenApiParameter(name: "name", Description = "The name of the task", Required = true, In = ParameterLocation.Query)]
    [OpenApiParameter(name: "description", Description = "The description of the task", Required = false, In = ParameterLocation.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TodoItem[]), Description = "Returns back the newly created to do item")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Displays an error message")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        // Load app settings
        var appSettings = AppSettings.LoadSettings();

        // Read the name and description from the request headers
        string name = req.Query.GetValues("name").First();
        string description = req.Query.GetValues("description").First();

        // Read the request body and deserialize it to CreateTodoDTO
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        TodoItem newTodo = new(appSettings.ListId, name){
            Description = description,
            State = "todo"
        };

        // Define the URL to make the HTTP POST request to
        string url = "https://app-api-cuniu3csaexyy.azurewebsites.net/lists/"+appSettings.ListId+"/items";

        // Prepare the JSON payload
        StringContent payload = new(JsonSerializer.Serialize(newTodo), Encoding.UTF8, "application/json");

        // Make the HTTP POST request
        HttpResponseMessage response = await httpClient.PostAsync(url, payload);

        // Prepare the response object
        var httpResponse = req.CreateResponse(HttpStatusCode.OK);

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and return the response body as string
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize the response body to TodoItem[]
            TodoItem toDo = JsonSerializer.Deserialize<TodoItem>(responseBody)!;

            // Serialize the filtered array to JSON
            string jsonPayload = JsonSerializer.Serialize(toDo);

            // Set the response content and content type
            httpResponse.WriteString(jsonPayload);
            httpResponse.Headers.Add("Content-Type", "application/json");
        }
        else
        {
            httpResponse.StatusCode = HttpStatusCode.BadRequest;
            httpResponse.WriteString("Failed to make HTTP request.");
        }

        return httpResponse;
    }
}
