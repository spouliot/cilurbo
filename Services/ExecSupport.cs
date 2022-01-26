using System.Runtime.InteropServices;

namespace Cilurbo.Services;

static class ExecSupport {

	// CA2101 is a false positive -> https://github.com/dotnet/roslyn-analyzers/issues/2886
	[DllImport ("libc", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true, CallingConvention = CallingConvention.Cdecl)]
	extern static int system (/*const char * */ string command);

	static public int Run (string? command)
	{
		if (String.IsNullOrEmpty (command))
			return -1;

		return system (command);
	}
}
