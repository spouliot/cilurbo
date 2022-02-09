using SimpleGist;

namespace Cilurbo.Services;

class GistSupport {

	static public async Task<string?> Gist (string description, string fileName, string? content)
	{
		if (GistClient.OAuthToken is null)
			return null; // TODO log

		GistRequest request = new () {
			Description = description,
			Public = false,
		};
		request.AddFile (fileName, content ?? "<empty>");

		var result = await GistClient.CreateAsync (request);
		if (result.StatusCode != System.Net.HttpStatusCode.Created) {
			// TODO log
		}
		if (ExecSupport.Run ("open", result.Url) != 0) {
			// TODO Log
		}
		return result.Url;
	}
}
