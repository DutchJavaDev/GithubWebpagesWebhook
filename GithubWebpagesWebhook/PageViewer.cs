#if !DEBUG
using Azure.Storage.Blobs;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GithubWebpagesWebhook
{
  public static class PageViewer
  {
    [FunctionName("PageView")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Admin, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      try
      {
        var page = await GenerateWebpageAsync();

        return new ContentResult()
        {
          Content = page,
          ContentType = "text/html",
          StatusCode = 200,
        };
      }
      catch (Exception e)
      {
        return new OkObjectResult(e);
      }
    }

    private static async Task<string> GenerateWebpageAsync()
    {
      var template = await GetTemplateFileAsync();

      var projects = await ProjectDivGenerator.GenerateProjectDivsAsync();

      var htmlTemplate = template
        .Replace("[user-name]", GithubClientWrapper.ClientLogin)
        .Replace("[page-content]", projects)
        .Replace("[last-update]", DateTime.Now.ToLongDateString());

      return htmlTemplate;
    }

    private static async Task<string> GetTemplateFileAsync()
    {
#if DEBUG
      return await File.ReadAllTextAsync("PageGenerator/index.html");
#else
      var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

      // TODO read template location from env/dynamic way
      var blobClient = new BlobClient(connectionString, "html-templates", "index.html");

      var content = await blobClient.DownloadContentAsync();

      return content.Value.Content.ToString();
#endif
    }
  }
}
