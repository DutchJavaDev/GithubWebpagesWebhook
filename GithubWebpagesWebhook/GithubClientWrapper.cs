using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GithubWebpagesWebhook
{
  public static class GithubClientWrapper
  {
    private static readonly GitHubClient _client;
    private static readonly string _clientLogin;
    private static readonly string _webPageRepositoryName = Environment.GetEnvironmentVariable("WebPagesRepositorieName");

    static GithubClientWrapper()
    {
      var accessToken = Environment.GetEnvironmentVariable("GithubAccessToken");
      var credentials = new Credentials(accessToken);
      _client = new GitHubClient(new ProductHeaderValue("MyApp"))
      {
        Credentials = credentials
      };
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

    public static async Task<IReadOnlyList<RepositoryContent>> GetAllRepositoryContentAsync(string branch = "main")
    {
      return await _client.Repository.Content.GetAllContentsByRef(_clientLogin, _webPageRepositoryName, "/", branch);
    }

    public static async Task<Reference> GetReferenceAsync(string branch = "main")
    {
      return await _client.Git.Reference.Get(_clientLogin, _webPageRepositoryName, $"heads/{branch}");
    }

    public static async Task<TreeResponse> CreateTreeResponseAsync(NewTree treeBuilder)
    {
      return await _client.Git.Tree.Create(_clientLogin, _webPageRepositoryName, treeBuilder);
    }

    public static async Task<Commit> CreateCommitAsync(TreeResponse newTree, Reference baseRef)
    {
      var changes = new NewCommit("Updated the repository with new changes via GitHubWebpageWebHook", newTree.Sha, new[] { baseRef.Object.Sha });

      return await _client.Git.Commit.Create(_clientLogin, _webPageRepositoryName, changes);
    }

    public static async Task UpdateRepositoryAsync(Commit commit, string branch = "main")
    {
      await _client.Git.Reference.Update(_clientLogin, _webPageRepositoryName, $"heads/{branch}", new ReferenceUpdate(commit.Sha));
    }
  }
}
