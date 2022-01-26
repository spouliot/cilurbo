using Octokit;

namespace Cilurbo.Services;

class GistSupport {

	static string? token;


	static string? OAuthToken {
		get {
			if (token is null)
				token = Environment.GetEnvironmentVariable ("GITHUB_OAUTH_TOKEN");

			if (token is null) {
				var home = Environment.GetEnvironmentVariable ("HOME");
				if (home is not null)
					token = File.ReadAllText (Path.Combine (home, ".cilurbo-github-token"));
			}
			return token;
		}
	}

	static public async Task<string?> Gist (string description, string fileName, string? content)
	{
		var token = OAuthToken;
		if (token is null)
			return null; // TODO log

		GitHubClient client = new (new ProductHeaderValue ("Cilurbo")) {
			Credentials = new Credentials (OAuthToken)
		};
		NewGist gist = new () {
			Description = description,
			Public = false,
		};
		gist.Files.Add (fileName, content ?? "");
		var result = await client.Gist.Create (gist);
		if (ExecSupport.Run ("open " + result.HtmlUrl) != 0) {
			// TODO Log
		}
		return result.HtmlUrl;
	}
}
