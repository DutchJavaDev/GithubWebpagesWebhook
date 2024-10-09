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
    private static string _clientLogin;
    static GithubClientWrapper()
    {
      var accessToken = Environment.GetEnvironmentVariable("GithubAccessToken");
      var credentials = new Credentials(accessToken);
      _client = new GitHubClient(new ProductHeaderValue("MyApp"))
      {
        Credentials = credentials
      };
    }

    public static async Task<IReadOnlyList<Repository>> GetRepositoriesForAccessTokenAsync(bool privateReposity = false, bool forkedRepository = false)
    {
      var repositories = await _client.Repository.GetAllForCurrent(new RepositoryRequest 
      {
        Type = RepositoryType.Owner,
        Sort = RepositorySort.Pushed,
        Direction = SortDirection.Descending
      });

      // This should be moved.....
      var user = await _client.User.Current();
      _clientLogin = user.Login;

      return repositories.Where(i => i.Private == privateReposity && i.Fork == forkedRepository).ToList();
    }

    public static async Task<IReadOnlyList<RepositoryLanguage>> GetRepositoryLanguagesAsync(string repositoryName)
    {
      return await _client.Repository.GetAllLanguages(_clientLogin, repositoryName);
    }
  }
}
