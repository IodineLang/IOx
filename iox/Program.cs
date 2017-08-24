namespace iox {
	using System.Globalization;
	using System.Threading;
	using Codeaddicts.libArgument;

	/// <summary>
	/// Main class.
	/// </summary>
	class MainClass {

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		public static void Main (string [] args) {

			// Prepare application
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			// Prepare console
			ConsoleHelper.EnableUTF8 ();

			// Read configuration
			var conf = ArgumentParser<Configuration>.Parse (args);

			// Create shell instance
			var shell = new Shell (conf);

			// Run REPL
			shell.Run ();
		}
	}
}
