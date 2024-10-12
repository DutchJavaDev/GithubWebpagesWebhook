using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var repo = Environment.GetEnvironmentVariable("WebPagesRepositorieName");

        // Get the repository contents
        var contents = await _client.Repository.Content.GetAllContentsByRef(_clientLogin, repo, "/", "main");

        var index = contents.First(i => i.Type == ContentType.File && i.Name.Equals("index.html"));

        //foreach (var content in contents)
        //{
        //  if (content.Type == ContentType.File)
        //  {
        //    var fileBytes = Convert.FromBase64String(content.EncodedContent);
        //    File.WriteAllBytes(Path.Combine(LocalDirectory, content.Name), fileBytes);
        //  }
        //}

        return new ContentResult() 
        {
          Content = Encoding.UTF8.GetString(Convert.FromBase64String(index.EncodedContent)),
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
