using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

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
        var directories = new[] 
        {
          Environment.SystemDirectory,
          Environment.CurrentDirectory,
        };

        var builder = new StringBuilder();

        foreach (var directory in directories) 
        {
          var files = Directory.GetFiles(directory);

          foreach (var file in files) 
          {
            builder.AppendLine(file);
          }
        }

        return new OkObjectResult(builder.ToString());
      }
      catch (Exception e)
      {
        return new OkObjectResult(e.ToString());
      }
    }
  }
}
