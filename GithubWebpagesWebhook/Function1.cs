using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System;
using Azure.Core;
using System.Collections.Concurrent;
using System.IO;

namespace GithubWebpagesWebhook
{
  public static class Function1
  {
    [FunctionName("GithubWebHook")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      try
      {
        var blobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "html-templates", "index.html");

        var content = await blobClient.DownloadContentAsync();

        var projects = await ProjectDivGenerator.GenerateProjectDivsAsync();

        var htmlTemplate = content.Value.Content.ToString()
          .Replace("[user-name]", GithubClientWrapper.ClientLogin)
          .Replace("[page-content]", projects)
          .Replace("[last-update]", DateTime.Now.ToLongDateString());

        return new ContentResult()
        {
          Content = htmlTemplate,
          ContentType = "text/html",
          StatusCode = 200,
        };
      }
      catch (Exception e)
      {
        return new OkObjectResult(e);
      }
    }
  }
}
