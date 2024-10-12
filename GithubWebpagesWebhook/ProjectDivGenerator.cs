using Octokit;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubWebpagesWebhook
{
  public static class ProjectDivGenerator
  {
    static readonly ConcurrentDictionary<int, string> ProjectOrderDictionary = new();

    static readonly string ProjectTemplate = @"
      <div class=""card"">
      <h3 class=""card-title"">[project-title]</h3>
      <p class=""card-description""><b>Description: </b>[project-description]</p>
      <p class=""card-languages""><b>Languages: </b> [project-languages]</p>
      <p class=""card-last-commit"" data-commit-message='[commit-message-slot]' data-commit-date='[commit-date-slot]'></p>
      <a class=""card-link"" href=""[project-html-url]"" target=""_blank"">View on GitHub</a>
      </div>";

    static readonly string LanguageTemplate = @"<span class=""language [language-css]"">[project-language]</span>";

    public static async Task<string> GenerateProjectDivsAsync()
    {
      var repositories = await GithubClientWrapper.GetRepositoriesForAccessTokenAsync();

      var tasks = new List<Task>();

      for (int i = 0; i < repositories.Count; i++)
      {
        tasks.Add(GenerateAsync(i, repositories[i]));
      }

      await Task.WhenAll(tasks);

      var projectBuilder = new StringBuilder();

      foreach (var (_, content) in ProjectOrderDictionary.OrderBy(i => i.Key))
      {
        projectBuilder.AppendLine(content);
      }

      return projectBuilder.ToString();
    }

    private static async Task GenerateAsync(int orderIndex, Repository repository)
    {
      var projectStringBase = ProjectTemplate;

      projectStringBase = projectStringBase.Replace("[project-title]", GetProjectTitle(repository));
      projectStringBase = projectStringBase.Replace("[project-description]", GetProjectDescription(repository));
      projectStringBase = projectStringBase.Replace("[project-languages]", await GetProjectLanguagesAsync(repository));
      projectStringBase = await GetProjectLastCommitMessage(repository, projectStringBase);
      projectStringBase = projectStringBase.Replace("[project-html-url]", GetProjectUrl(repository));

      ProjectOrderDictionary.TryAdd(orderIndex, projectStringBase);
    }

    private static string GetProjectTitle(Repository repository)
    {
      return repository.Name;
    }

    private static string GetProjectDescription(Repository repository)
    {
      return repository.Description;
    }

    private static async Task<string> GetProjectLanguagesAsync(Repository repository)
    {
      var languages = await GithubClientWrapper.GetRepositoryLanguagesAsync(repository.Name);

      if (languages.Any())
      {
        var totalLanguageBytes = languages.Sum(i => i.NumberOfBytes);

        var languageBuilder = new StringBuilder();

        foreach (var language in languages)
        {
          var percentage = GetPercentage(totalLanguageBytes, language.NumberOfBytes);

          if (CascadingStyleSheetHelper.TryGetLanguageCss(language.Name, out var css))
          {
            languageBuilder.Append(LanguageTemplate.Replace("[language-css]", css)
                                     .Replace("[project-language]", $"{language.Name} ({percentage}%)"));
          }
          else
          {
            // Css is not know.....
            languageBuilder.Append(@$"<span class='language' style='background-color:red'>{language.Name} ({percentage}%)</span>");
          }
        }

        return languageBuilder.ToString();
      }

      return @"<span class='' style='color:gray'>No language's detected, probally configuration</span>";
    }

    private static string GetPercentage(long max, long value)
    {
      return (((float)value / (float)max) * 100f).ToString("0.00");
    }

    private static async Task<string> GetProjectLastCommitMessage(Repository repository, string projectStr)
    {
      // <p class=""card-last-commit"" data-commit-date>[project-last-commit]</p>

      var lastCommitMessage = await GithubClientWrapper.GetGitHubCommitAsync(repository.Name, repository.DefaultBranch);

      if (lastCommitMessage != null)
      {
        var message = lastCommitMessage.Commit.Message;
        var date = lastCommitMessage.Commit.Author.Date.ToString("o");

        projectStr = projectStr.Replace("[commit-message-slot]", $"<b>Last commit: </b> {message}");
        projectStr = projectStr.Replace("[commit-date-slot]", date);

        // Updated by js
        return projectStr;
      }

      return projectStr;
    }

    private static string GetProjectUrl(Repository repository)
    {
      return repository.HtmlUrl;
    }
  }
}
