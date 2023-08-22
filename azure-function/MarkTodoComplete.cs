using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Models;

public class MarkToDoComplete
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    [Function("MarkToDoComplete")]
    [OpenApiOperation(operationId: "MarkToDoComplete", tags: new[] { "ExecuteFunction" }, Description = "Marks a to do item as complete")]
    [OpenApiParameter(name: "id", Description = "The ID of the task to mark complete; retrieve the ID from the JSON response of the GetToDos function", Required = true, In = ParameterLocation.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TodoItem[]), Description = "Returns back the newly created to do item")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Displays an error message")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        // Load app settings
        var appSettings = AppSettings.LoadSettings();

        // Read the name and description from the request headers
        string itemId = req.Query.GetValues("id").First();

        // Define the URL to make the HTTP GET and POST request to
        string url = "https://app-api-cuniu3csaexyy.azurewebsites.net/lists/"+appSettings.ListId+"/items/" + itemId;

        // Make the HTTP GET request
        HttpResponseMessage getResponse = await httpClient.GetAsync(url);

        // Deserialize the response body to TodoItem
        TodoItem toDoItem = JsonSerializer.Deserialize<TodoItem>(await getResponse.Content.ReadAsStringAsync())!;

        // Update the state of the to do item
        toDoItem.State = "done";

        // Prepare the JSON payload
        StringContent payload = new(JsonSerializer.Serialize(toDoItem), Encoding.UTF8, "application/json");

        // Make the HTTP PUT request
        HttpResponseMessage response = await httpClient.PutAsync(url, payload);

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
