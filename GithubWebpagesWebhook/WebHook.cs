#if !DEBUG
using Azure.Storage.Blobs;
#endif
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using Octokit;

namespace GithubWebpagesWebhook
{
  public static class WebHook
  {
    [FunctionName("GithubWebHook")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      try
      {
        // Generate index html
        var webPage = await GenerateWebpageAsync();

        // Copy webpages repo from github, get index.html file path
        var tempLocalDirectory = Path.Combine(Path.GetTempPath(), "temp-clone-folder");

        Directory.CreateDirectory(tempLocalDirectory);

        var indexFilePath = await CloneWebPageRepositoryAsync(tempLocalDirectory);

        // Update repo with new index.html
        File.WriteAllText(indexFilePath, webPage);

        // Push changes
        await PushChangesAsync(tempLocalDirectory);

        // Clear local clone of repo
        Directory.Delete(tempLocalDirectory, true);

        return new OkResult();
      }
      catch (Exception e)
      {
        return new OkObjectResult(e);
      }
    }

    public static async Task<string> GenerateWebpageAsync()
    {
      var template = await GetTemplateFileAsync();

      var projects = await ProjectDivGenerator.GenerateProjectDivsAsync();

      var htmlTemplate = template
        .Replace("[user-name]", GithubClientWrapper.ClientLogin)
        .Replace("[page-content]", projects)
        .Replace("[last-update]", DateTime.Now.ToLongDateString());

      return htmlTemplate;
    }

    public static async Task<string> GetTemplateFileAsync()
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

    public static async Task<string> CloneWebPageRepositoryAsync(string tempFolder)
    {
      var client = new HttpClient();

      var contents = await GithubClientWrapper.GetAllRepositoryContentAsync();

      string indexPath = string.Empty;

      foreach (var content in contents)
      {
        if (content.Type ==  Octokit.ContentType.File)
        {
          var fileBytes = await client.GetStringAsync(content.DownloadUrl);

          var path = Path.Combine(tempFolder, content.Name);

          File.WriteAllText(path, fileBytes);

          if (content.Name.Contains("index.html"))
          {
            indexPath = path;
          }
        }
      }

      return indexPath;
    }

    public static async Task PushChangesAsync(string tempFolder)
    {
      // Get the latest commit on the main branch
      var baseRef = await GithubClientWrapper.GetReferenceAsync();

      // Prepare the new tree
      var treeBuilder = new NewTree();
      foreach (var file in Directory.GetFiles(tempFolder))
      {
        var fileName = Path.GetFileName(file);
        var content = File.ReadAllText(file);

        // Create a new tree entry for each file
        treeBuilder.Tree.Add(new NewTreeItem
        {
          Path = fileName,
          Mode = "100644", // Blob file
          Type = TreeType.Blob,
          Content = content
        });
      }

      // Finilaize the changes and push
      var newTree = await GithubClientWrapper.CreateTreeResponseAsync(treeBuilder);

      var commit = await GithubClientWrapper.CreateCommitAsync(newTree, baseRef);

      await GithubClientWrapper.UpdateRepositoryAsync(commit);
    }
  }
}
