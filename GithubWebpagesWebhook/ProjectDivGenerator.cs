using Octokit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubWebpagesWebhook
{
  public static class ProjectDivGenerator
  {
    static ConcurrentDictionary<int, string> ProjectOrderDictionary = new();

    static readonly string ProjectTemplate = @"<div class=""card"">
      <h3 class=""card-title"">[project-title]</h3>
      <p class=""card-description"">[project-description]</p>
      <p class=""card-languages"">Languages: [project-languages]</p>
      <p class=""card-last-commit"">[project-last-commit]</p>
      <a class=""card-link"" href=""[project-html-url]"" target=""_blank"">View on GitHub</a>
    </div>";

    static readonly string LanguageTemplate = @"<span class=""[language-css]"">[project-language]</span>";

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

    public static async Task GenerateAsync(int orderIndex, Repository repository)
    {
      var projectStringBase = ProjectTemplate;
      projectStringBase = projectStringBase.Replace("[project-title]", SetProjectTitle(repository));
    }

    private static string SetProjectTitle(Repository repository)
    {
      return repository.Name;
    }

    private static async Task<string> SetProjectLanguagesAsync(Repository repository)
    {
      var languages = await GithubClientWrapper.GetRepositoryLanguagesAsync(repository.Name);


      if (languages.Any())
      {
        var totalLanguageBytes = languages.Sum(i => i.NumberOfBytes);

        // Copy css dictionary from poc

        return string.Empty;
      }

      return @"<span class='' style='color:gray'>No language detected, probally configuration</span>";
    }
  }
}
