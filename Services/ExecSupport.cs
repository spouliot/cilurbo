using CliWrap;

namespace Cilurbo.Services;

static class ExecSupport {

	static public int Run (string command, string arguments)
	{
		Task<CommandResult>? result = Task.Run (async () => await Cli.Wrap (command).WithArguments (arguments).ExecuteAsync ());
		return result.Result.ExitCode;
	}
}
