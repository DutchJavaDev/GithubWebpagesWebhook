﻿using Octokit;
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

    public static string ClientLogin { get { return _clientLogin; } }

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
  }
}
