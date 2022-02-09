// For latest version, see https://github.com/spouliot/SimpleGist/blob/main/SimpleGist.cs
//
// MIT License
//
// Copyright (c) 2022 Sebastien Pouliot
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SimpleGist;

public class GistClient {

	static readonly HttpClient client = new ();

	static GistClient ()
	{
		client.BaseAddress = new Uri ("https://api.github.com/");
		client.DefaultRequestHeaders.Accept.Clear ();
		client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/vnd.github.v3+json"));
		client.DefaultRequestHeaders.UserAgent.Add (new ProductInfoHeaderValue ("SimpleGist", "1.1"));

		var token = Environment.GetEnvironmentVariable ("GITHUB_OAUTH_TOKEN");
		if (token is null) {
			var home = Environment.GetEnvironmentVariable ("HOME");
			if (home is not null) {
				var gist = Path.Combine (home, ".gist");
				if (File.Exists (gist))
					token = File.ReadAllText (gist);
			}
		}
		OAuthToken = token;
	}

	public static string? OAuthToken {
		get {
			return client.DefaultRequestHeaders.Authorization?.Parameter;
		}
		set {
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue ("token", value);
		}
	}

	public static async Task<GistResponse> CreateAsync (GistRequest request)
	{
		if (request is null)
			throw new ArgumentNullException (nameof (request));

		StringBuilder sb = new ();
		sb.AppendLine ("{");
		sb.Append ("\t\"description\": \"").Append (request.Description).AppendLine ("\",");
		sb.Append ("\t\"public\": ").Append (request.Public ? "true" : "false").AppendLine (",");
		sb.AppendLine ("\t\"files\": {");
		foreach (var file in request.Files) {
			sb.Append ("\t\t\"").Append (file.Key).AppendLine ("\": {");
			sb.Append ("\t\t\t\"content\": \"").Append (file.Value).AppendLine ("\"");
			sb.AppendLine ("\t\t}");
		}
		sb.AppendLine ("\t}");
		sb.AppendLine ("}");

		var response = await client.PostAsync ("gists", new StringContent (sb.ToString ()));
		return new (response.StatusCode, await response.Content.ReadAsStringAsync ());
	}
}

public class GistRequest {

	public string? Description { get; set; }
	public bool Public { get; set; }

	public Dictionary<string, string> Files { get; private set; }

	public GistRequest ()
	{
		Files = new ();
	}

	public void AddFile (string name, string content)
	{
		if (content.IndexOfAny (new [] { '\r', '\n', '\t', '"' }) != -1) {
			StringBuilder sb = new ();
			foreach (var c in content) {
				switch (c) {
				case '\r':
					sb.Append ("\\r");
					break;
				case '\n':
					sb.Append ("\\n");
					break;
				case '\t':
					sb.Append ("\\t");
					break;
				case '"':
					sb.Append ("\\\"");
					break;
				default:
					sb.Append (c);
					break;
				}
			}
			content = sb.ToString ();
		}
		Files.Add (name, content);
	}
}

public class GistResponse {

	public HttpStatusCode StatusCode { get; private set; }

	public string Url { get; private set; }

	internal GistResponse (HttpStatusCode status, string content)
	{
		StatusCode = status;
		switch (status) {
		case HttpStatusCode.Created:
			// quite hackish - get the first `html_url` from the JSON response
			int first = content.IndexOf ("\"html_url\"", StringComparison.Ordinal);
			int start = content.IndexOf ('"', first + "\"html_url\"".Length + 1);
			int end = content.IndexOf ('"', start + 1);
			Url = content [(start + 1)..end];
			break;
		case HttpStatusCode.BadRequest:
		case HttpStatusCode.Unauthorized:
		case HttpStatusCode.UnprocessableEntity:
			Url = $"https://github.com/spouliot/SimpleGist/wiki#{status}";
			break;
		default:
			Url = "https://github.com/spouliot/SimpleGist/wiki#Errors";
			break;
		}
	}
}
