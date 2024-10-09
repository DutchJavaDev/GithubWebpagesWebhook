using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
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

      //var page = await File.ReadAllTextAsync("PageGenerator/index.html");

      try
      {
        var data = new[] 
        {
          Environment.SystemDirectory,
          Environment.CurrentDirectory,
        };
        return new OkObjectResult(data);
      }
      catch (Exception e)
      {
        return new OkObjectResult(e.ToString());
      }
    }
  }
}
