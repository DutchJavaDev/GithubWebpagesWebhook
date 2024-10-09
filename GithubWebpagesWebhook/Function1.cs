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

      var repositories = await GithubClientWrapper.GetRepositoriesForAccessTokenAsync();

      var projectOrderDictionary = new ConcurrentDictionary<int, object>();

      // last step
      var blobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"),"html-templates","index.html");

      var content = await blobClient.DownloadContentAsync();

      return new ContentResult() 
      {
        Content = content.Value.Content.ToString(),
        ContentType = "text/html",
        StatusCode = 200,
      };
    }
  }
}
