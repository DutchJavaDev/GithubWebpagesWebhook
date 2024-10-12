using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GithubWebpagesWebhook
{
  public static class GithubClientWrapper
  {
    private static readonly GitHubClient _client;
    private static string _clientLogin;
    public static string LocalDirectory = Path.Combine(Path.GetTempPath(), "my-cloned-repo");
    static GithubClientWrapper()
    {
      var accessToken = Environment.GetEnvironmentVariable("GithubAccessToken");
      var credentials = new Credentials(accessToken);
      _client = new GitHubClient(new ProductHeaderValue("MyApp"))
      {
        Credentials = credentials
      };
      Directory.CreateDirectory(LocalDirectory);

      var user = _client.User.Current().Result;
      _clientLogin = user.Login;
    }

    public static string ClientLogin { get { return _clientLogin; } }

    public static async Task<IReadOnlyList<Repository>> GetRepositoriesForAccessTokenAsync(bool privateReposity = false, bool forkedRepository = false)
    {
      var repositories = await _client.Repository.GetAllForCurrent(new RepositoryRequest 
      {
        Type = RepositoryType.Owner,
        Sort = RepositorySort.Pushed,
        Direction = SortDirection.Descending
      });

      return repositories.Where(i => i.Private == privateReposity && i.Fork == forkedRepository && i.Archived == false).ToList();
    }

    public static async Task<IReadOnlyList<RepositoryLanguage>> GetRepositoryLanguagesAsync(string repositoryName)
    {
      return await _client.Repository.GetAllLanguages(_clientLogin, repositoryName);
    }

    public static async Task<GitHubCommit> GetGitHubCommitAsync(string repositoryName, string branchName)
    {
      var branch = await _client.Git.Reference.Get(_clientLogin, repositoryName, $"heads/{branchName}");

      var commitMessage = await _client.Repository.Commit.Get(_clientLogin, repositoryName, branch.Object.Sha);

      return commitMessage;
    }

    public static async Task<IActionResult> CloneTest()
    {
      try
      {
        var client = new HttpClient();
        var repo = Environment.GetEnvironmentVariable("WebPagesRepositorieName");
        var branch = "main";
        // Get the repository contents
        var contents = await _client.Repository.Content.GetAllContentsByRef(_clientLogin, repo, "/", branch);

        string indexPath = string.Empty;

        foreach (var content in contents)
        {
          if (content.Type == ContentType.File)
          {
            var fileBytes = await client.GetStringAsync(content.DownloadUrl);

            var path = Path.Combine(LocalDirectory, content.Name);

            File.WriteAllText(path, fileBytes);

            if(content.Name.Contains("index.html"))
            {
              indexPath = path;
            }
          }
        }

        var blobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "html-templates", "index.html");

        var blobContent = await blobClient.DownloadContentAsync();

        var projects = await ProjectDivGenerator.GenerateProjectDivsAsync();

        var htmlTemplate = blobContent.Value.Content.ToString()
          .Replace("[user-name]", ClientLogin)
          .Replace("[page-content]", projects)
          .Replace("[last-update]", DateTime.Now.ToLongDateString());

        File.WriteAllText(indexPath, htmlTemplate);

        // Step 3a: Get the latest commit on the main branch
        var baseRef = await _client.Git.Reference.Get(_clientLogin, repo, $"heads/{branch}");

        // Step 3b: Prepare the new tree
        var treeBuilder = new NewTree();
        foreach (var file in Directory.GetFiles(LocalDirectory))
        {
          var fileName = Path.GetFileName(file);
          var content = File.ReadAllText(file);

          // Create a new tree entry for each file
          treeBuilder.Tree.Add(new NewTreeItem
          {
            Path = fileName,
            Mode = "100644",
            Type = TreeType.Blob,
            Content = content
          });
        }

        // Create a new tree from the tree builder
        var newTree = await _client.Git.Tree.Create(_clientLogin, repo, treeBuilder);

        // Step 3c: Create a new commit
        // Step 3c: Create a new commit
        var changes = new NewCommit("Updated the repository with new changes", newTree.Sha, new[] { baseRef.Object.Sha })
        {
        };

        var commit = await _client.Git.Commit.Create(_clientLogin, repo, changes);

        // Step 3d: Update the main branch to point to the new commit
        await _client.Git.Reference.Update(_clientLogin, repo, $"heads/{branch}", new ReferenceUpdate(commit.Sha));

        return new ContentResult() 
        {
          Content = "Okay",
          ContentType = "document",
          StatusCode = 200,
        };
      }
      catch (Exception ex)
      {
        return new OkObjectResult(ex);
      }
    }
  }
}
