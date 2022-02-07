using SimpleGist;

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

		GistClient.OAuthToken = token;

		GistRequest request = new () {
			Description = description,
			Public = false,
		};
		request.AddFile (fileName, content ?? "");

		var result = await GistClient.CreateAsync (request);
		if (ExecSupport.Run ("open " + result.Url) != 0) {
			// TODO Log
		}
		return result.Url;
	}
}
