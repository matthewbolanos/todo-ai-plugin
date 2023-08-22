using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Models;

namespace SimpleTodo.Plugin;

public class GetToDos
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    [Function("GetToDos")]
    [OpenApiOperation(operationId: "GetToDos", tags: new[] { "ExecuteFunction" }, Description = "Gets the tasks of the current user. This function returns a string with a list of tasks in it.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(TodoItem[]), Description = "Returns the list of todo items")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Displays an error message")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        // Load app settings
        var appSettings = AppSettings.LoadSettings();

        // Define the URL to make the HTTP request to
        string url = "https://app-api-cuniu3csaexyy.azurewebsites.net/lists/"+appSettings.ListId+"/items";

        // Make the HTTP GET request
        HttpResponseMessage response = await httpClient.GetAsync(url);

        // Prepare the response object
        var httpResponse = req.CreateResponse(HttpStatusCode.OK);

        // Check if the request was successful
        if (response.IsSuccessStatusCode)
        {
            // Read and return the response body as string
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize the response body to TodoItem[]
            TodoItem[] toDos = JsonSerializer.Deserialize<TodoItem[]>(responseBody)!;

            // Create a new array containing only the required fields
            var filteredToDos = toDos.Select(todo => new { todo.Id, todo.Name, todo.Description }).ToArray();

            // Create a string for the tasks data
            string tasksData = "These are the current to dos: \n" + string.Join("\n\n", filteredToDos.Select(todo => $"ID: {todo.Id} \n Name: {todo.Name} \n Description: {todo.Description}"));

            // Set the response content and content type
            httpResponse.WriteString(tasksData);
            httpResponse.Headers.Add("Content-Type", "text/plain");
        }
        else
        {
            httpResponse.StatusCode = HttpStatusCode.BadRequest;
            httpResponse.WriteString("Failed to make HTTP request.");
        }

        return httpResponse;
    }
}
