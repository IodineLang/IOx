namespace iox {
	using System;
	using DocoptNet;

	/// <summary>
	/// Configuration.
	/// </summary>
	public class Configuration {

		const string USAGE = @"
iox
The new and shiny Iodine REPL shell.

Usage:
    iox help
    iox version
    iox check <FILE>
    iox repl [options] <FILE>
    iox [options] [<FILE>]

Subcommands:
    help            Show this screen.
    repl            Run program and show a REPL shell afterwards.
    check           Check the syntax of the source.
    version         Show the iox and Iodine version.

Options:
    --powerlines    Enable powerline support.
    --no-cache      Disable bytecode caching.
    --no-optimize   Disable optimizations.
";

		public static Configuration Parse (string [] clargs) {
			var docopt = new Docopt ();
			var args = docopt.Apply (
				doc: USAGE,
				argv: clargs,
				help: true,
				exit: false
			);
			return new Configuration {
				Docopt = docopt,
				Repl = args ["repl"]?.IsTrue ?? false,
				Help = args ["help"]?.IsTrue ?? false,
				Check = args ["check"]?.IsTrue ?? false,
				Version = args ["version"]?.IsTrue ?? false,
				UsePowerlines = args ["--powerlines"]?.IsTrue ?? false,
				SuppressCache = args ["--no-cache"]?.IsTrue ?? false,
				SuppressOptimizer = args ["--no-optimize"]?.IsTrue ?? false,
				File = args ["<FILE>"]?.Value as string,
			};
		}

		public static void PrintHelp (bool exit, bool error = false) {
			ANSI.WriteLine (USAGE.Trim ());
			if (exit) {
				Environment.Exit (error ? 1 : 0);
			}
		}

		// Docopt
		internal Docopt Docopt;

		// Subcommands
		public bool Repl;
		public bool Help;
		public bool Check;
		public bool Version;

		// Options
		public bool UsePowerlines;
		public bool SuppressCache;
		public bool SuppressOptimizer;

		// Positional arguments
		public string File;
	}
}