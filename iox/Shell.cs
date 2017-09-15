namespace iox {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using Iodine.Runtime;
	using static ANSIColor;
	using static PowerlineBuilder;

	public class Shell {

		/// <summary>
		/// The iodine context.
		/// </summary>
		internal readonly IodineContext Iodine;

		/// <summary>
		/// The prompt.
		/// </summary>
		readonly Prompt Prompt;

		/// <summary>
		/// The hinter.
		/// </summary>
		readonly HinterWrapper Hinter;

		/// <summary>
		/// The configuration.
		/// </summary>
		readonly Configuration Conf;

		/// <summary>
		/// The assembly version.
		/// </summary>
		readonly Version AssemblyVersion;

		/// <summary>
		/// The last buffered user input.
		/// </summary>
		string BufferedInput;

		/// <summary>
		/// Whether the shell should exit.
		/// </summary>
		bool shouldExit;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:iox.Shell"/> class.
		/// </summary>
		public Shell (Configuration conf) {
			Conf = conf;

			// Get assembly version
			AssemblyVersion = ReflectionHelper.GetVersion ();

			// Define builtin exit function
			var iodineHookExit = new BuiltinMethodCallback ((vm, self, arguments) => {
				shouldExit = true;
				return IodineNull.Instance;
			}, null);
			iodineHookExit.SetAttribute ("__doc__", new IodineString ("Exits the iox REPL shell."));

			// Define builtin help function
			var iodineHookHelp = new BuiltinMethodCallback ((vm, self, arguments) => {
				// Test argument count
				if (arguments.Length == 0) {
					ANSI.WriteLine ("Please pass an object to the help function!");
					return IodineNull.Instance;
				}

				// Get the target object
				var target = arguments [0];

				// Test __doc__ attribute existence
				if (!target.HasAttribute ("__doc__") || !(target.GetAttribute (vm, "__doc__") is IodineString)) {
					ANSI.WriteLine ($"The specified {White}{target.TypeDef.Name}{Default} does not provide any documentation :(");
					return IodineNull.Instance;
				}

				// Write documentation
				ANSI.WriteLine (((IodineString) target.GetAttribute (vm, "__doc__")).Value);
				return IodineNull.Instance;
			}, null);
			iodineHookHelp.SetAttribute ("__doc__", new IodineString ("Prints the documentation for the specified object."));

			// Setup Iodine context
			Iodine = new IodineContext ();
			Iodine.ExposeGlobal ("exit", iodineHookExit);
			Iodine.ExposeGlobal ("help", iodineHookHelp);
			Iodine.AddSearchPaths ("./iodine/modules");
			if (Environment.GetEnvironmentVariable ("IODINE_MODULES") != null) {
				Iodine.AddSearchPaths (Environment.GetEnvironmentVariable ("IODINE_MODULES"));
			}
			if (Environment.GetEnvironmentVariable ("IODINE_HOME") != null) {
				Iodine.AddSearchPaths (Path.Combine (Environment.GetEnvironmentVariable ("IODINE_HOME"), "modules"));
			}

			// Set prompt
			Prompt = new Prompt ("λ");
			if (Conf.UsePowerlines) {
				Prompt = new Prompt (Powerline ().Segment ($"IoX {AssemblyVersion.ToString (2)} ", fg: DarkCyan, bg: White).ToAnsiString ());
			}

			// Set hinter
			Hinter = new HinterWrapper (this);
		}

		/// <summary>
		/// Enter the REPL.
		/// </summary>
		public void Run () {

			// Apply Iodine context options
			Iodine.Engine.Context.ShouldCache = !Conf.SuppressCache;
			Iodine.Engine.Context.ShouldOptimize = !Conf.SuppressOptimizer;

			// Check 'help'
			if (Conf.Help) {
				Configuration.PrintHelp (exit: true, error: false);
			}

			// Check 'version'
			else if (Conf.Version) {
				ANSI.WriteLine ($"IOx {Versions.IoxFullVersion} (Iodine {Versions.IodineFullVersion} {Versions.IodineBuildDate})");
				Environment.Exit (0);
			}

			// Check 'check'
			else if (Conf.Check) {
				RunCheck ();
				Environment.Exit (0);
			}

			// Check 'repl'
			else if (Conf.Repl) {
				RunFile (repl: true);
				Environment.Exit (0);
			}

			// Check file
			else if (!string.IsNullOrEmpty (Conf.File)) {
				RunFile (repl: false);
				Environment.Exit (0);
			}

			// No subcommands or files were specified
			else {
				
				// Enter the REPL shell
				RunRepl ();
			}
		}

		/// <summary>
		/// Run a syntax check only.
		/// </summary>
		void RunCheck () {

			// Get the file
			var content = string.Empty;
			try {
				content = File.ReadAllText (Conf.File);
			} catch (FileNotFoundException ex) {
				ANSI.WriteLine (ex.Message);
				Environment.Exit (1);
			}

			// Compile the module
			Iodine.Engine.Context.ShouldCache = false;
			try {
				Iodine.CompileSource (content);
				ANSI.WriteLine ("OK - No syntax errors.");
			} catch (Iodine.Compiler.SyntaxException e) {
				ANSI.WriteLine ($"ERR - Syntax errors occurred.\n");

				// Iterate over syntax errors
				foreach (var err in e.ErrorLog.Errors) {

					// Test if the error came from somewhere else
					if (!string.IsNullOrEmpty (err.Location.File)) {

						// Get the name of the file that caused the error
						var path = System.IO.Path.GetFileNameWithoutExtension (err.Location.File);

						// Print the error location and description
						ANSI.WriteLine ($"Error at {White}{path}{Default} ({err.Location.Line + 1}:{err.Location.Column}): {err.Text}");
						continue;
					}

					// Test if the error has an associated token
					if (err.HasToken) {

						// Get the length of the associated token
						var token_length = err.Token.Value.Length;

						// Print user input for later highlighting
						var line = content.Split ('\n') [err.Location.Line].Trim ();
						Console.Write (line);

						// Highlight the error
						Console.CursorLeft = err.Location.Column;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line.Substring (err.Location.Column, line [err.Location.Column] == '"' ? token_length + 2 : token_length)}");
						ANSI.WriteLine ($"{White}{string.Empty.PadLeft (token_length, '^').PadLeft (err.Location.Column)}");
					} else {

						// Print user input for later highlighting
						var line = content.Split ('\n') [err.Location.Line].Trim ();
						Console.Write (line);

						// Highlight the error
						Console.CursorLeft = err.Location.Column - 1;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line [err.Location.Column - 1]}");
						ANSI.WriteLine ($"{White}{"^".PadLeft (err.Location.Column, ' ')}");
					}

					// Print the error location and description
					ANSI.WriteLine ($"\n{White}SyntaxError{(err.Location.Line != 0 ? $" at line {err.Location.Line + 1}" : string.Empty)}: {Red}{err.Text}");
				}
			} catch (Exception e) {
				ANSI.WriteLine ($"ERR - {e.Message}");
			}
		}

		/// <summary>
		/// Run a file, and optionally a REPL shell afterwards.
		/// </summary>
		/// <param name="repl">If set to <c>true</c> repl.</param>
		void RunFile (bool repl) {
			
			// Get the file
			var content = string.Empty;
			try {
				content = File.ReadAllText (Conf.File);
			} catch (FileNotFoundException ex) {
				ANSI.WriteLine (ex.Message);
				Environment.Exit (1);
			}

			// Compile the module
			Iodine.Engine.Context.ShouldCache = false;
			var module = WrapIodineOperation (this, () => Iodine.CompileSource (content));
			if (module == null) return;

			// Invoke the module
			var result = WrapIodineOperation (this, () => Iodine.InvokeModule (module));
			if (result == null) return;

			// Test if the REPL shell should be entered
			if (repl) {
				
				// Enter REPL shell
				RunRepl ();
			}
		}

		/// <summary>
		/// Run a REPL shell only.
		/// </summary>
		void RunRepl () {
			
			// Print version information
			if (Conf.UsePowerlines) {
				ANSI.WriteLine (
					Powerline ()
					.Segment ($"IoX {Versions.IoxVersion} ", fg: DarkCyan, bg: White)
					.Segment ($" Iodine {Versions.IodineVersion} {Versions.IodineBuildDate}", fg: White, bg: DarkCyan)
					.ToAnsiString ());
			} else {
				ANSI.WriteLine ($"IOx {Versions.IoxVersion} (Iodine {Versions.IodineVersion} {Versions.IodineBuildDate})\n");
			}

			while (!shouldExit) {

				// Run a single REP iteration
				RunIteration ();
			}
		}

		/// <summary>
		/// Run a single read-evaluate-print iteration.
		/// </summary>
		void RunIteration () {

			// Read user input
			BufferedInput = ReadUserInput ();
			if (string.IsNullOrEmpty (BufferedInput)) return;

			// Compile the module
			var module = WrapIodineOperation (this, () => Iodine.CompileSource (BufferedInput));
			if (module == null) return;

			// Invoke the module
			var result = WrapIodineOperation (this, () => Iodine.InvokeModule (module));
			if (result == null) return;

			// Pretty print the result
			PrettyPrint.WriteLine (result);
		}

		string ReadUserInput () {

			// Declare variables
			var accum = new StringBuilder ();
			string lastLine = string.Empty;
			bool editingFinished = false;
			bool correctIndent = false;
			bool correctIndentExtra = false; // special unindent for matched grouping
			int foldCount = 0;
			int foldBracketDiff = 0;
			int foldBraceDiff = 0;
			int foldParenDiff = 0;
			int line = 1;
			int indent = 0;

			// Define getFoldCount function
			var getFoldCount = new Func<string, int> (str => {
				var lexer = new Iodine.Compiler.Tokenizer (
					new Iodine.Compiler.ErrorSink (),
					new Iodine.Compiler.SourceReader (str, "__anonymous__")
				);
				IEnumerable<Iodine.Compiler.Token> tokens;
				try {
					tokens = lexer.Scan ();
				} catch {
					foldBracketDiff = 0;
					foldParenDiff = 0;
					foldBraceDiff = 0;
					return 0;
				}
				foldBracketDiff = (
					tokens.Count (t => t.Class == global::Iodine.Compiler.TokenClass.OpenBracket) -
					tokens.Count (t => t.Class == global::Iodine.Compiler.TokenClass.CloseBracket)
				);
				foldBraceDiff = (
					tokens.Count (t => t.Class == global::Iodine.Compiler.TokenClass.OpenBrace) -
					tokens.Count (t => t.Class == global::Iodine.Compiler.TokenClass.CloseBrace)
				);
				foldParenDiff = (
					tokens.Count (t => t.Class == global::Iodine.Compiler.TokenClass.OpenParan) -
					tokens.Count (t => t.Class == global::Iodine.Compiler.TokenClass.CloseParan)
				);
				return foldBracketDiff + foldBraceDiff + foldParenDiff;
			});

			// Define getPrompt function
			var getPrompt = new Func<int, string> (lineNum => {
				if (Conf.UsePowerlines) {
					var powerline = Powerline ()
						.Segment ($"IoX {AssemblyVersion.ToString (2)} ", fg: DarkCyan, bg: White)
						.Segment ($"{lineNum}", fg: White, bg: DarkCyan)
						.ToAnsiString ();
					return powerline;
				}
				return $"{lineNum} λ";
			});

			// Define rewriteIndent function
			var rewriteIndent = new Action (() => {
				
				// Save cursor state
				var currentCursorTop = Console.CursorTop;
				var targetCursorTop = Math.Max (0, Console.CursorTop - 1);

				// Rewrite last line
				Console.CursorTop = targetCursorTop;
				Console.CursorLeft = 0;
				Console.Write ("".PadRight (Console.WindowWidth));
				Console.CursorTop = targetCursorTop;
				Console.CursorLeft = 0;
				Prompt.Push (getPrompt (Math.Max (0, line - 1)));
				ANSI.Write (Prompt.ToString ());
				Prompt.Pop ();
				Console.Write (string.Empty.PadLeft ((correctIndentExtra ? Math.Max (0, indent - 1) : indent) * 2));
				Console.Write (lastLine);

				// Restore cursor state
				Console.CursorTop = currentCursorTop;
				Console.CursorLeft = 0;
			});

			// Read more lines
			while (!editingFinished) {
				
				// Test if this is the first line
				if (line == 1) {

					// Duplicate prompt
					Prompt.Dup ();
				} else {
					
					// Push new prompt to visually indicate multi-line editing
					Prompt.Push (getPrompt (line));
				}

				// Test if the indentation of the previous line should be rewritten
				if (correctIndent) {

					// Rewrite line
					rewriteIndent ();

					// Do not correct the indentation again
					correctIndent = false;
					correctIndentExtra = false;
				}

				// Test if the prompt of the previous line should be rewritten
				if (line == 2) {
					
					// Save cursor state
					var currentCursorTop = Console.CursorTop;
					var targetCursorTop = Math.Max (0, Console.CursorTop - 1);

					// Rewrite last line
					Console.CursorTop = targetCursorTop;
					Console.CursorLeft = 0;
					Console.Write ("".PadRight (Console.WindowWidth));
					Console.CursorTop = targetCursorTop;
					Console.CursorLeft = 0;
					Prompt.Push (getPrompt (1));
					ANSI.Write (Prompt.ToString ());
					Prompt.Pop ();
					Console.Write (lastLine);

					// Restore cursor state
					Console.CursorTop = currentCursorTop;
					Console.CursorLeft = 0;
				}

				// Modify prompt
				Prompt.Push ($"{Prompt.ToString ().Trim ()}{string.Empty.PadLeft (indent * 2)}");

				// Read line
				lastLine = Hinter.Edit (Prompt.ToString ()) ?? string.Empty;

				// Restore prompt
				Prompt.Pop ();

				//lastLine = Hinter.ReadHintedLine (
				//	hintSource: Iodine.Engine.Context.Globals.Concat (Iodine.Engine.Context.InteractiveLocals),
				//	hintField: attr => attr.Key,
				//	hintColor: Gray
				//).Trim ();

				// lastLine = Console.ReadLine ().Trim ();

				// Update state
				var localFoldCount = getFoldCount (lastLine);
				foldCount += localFoldCount;

				// Auto-indent based on local fold count
				if (localFoldCount > 0) {
					indent += 1;
				} else if (localFoldCount < 0) {
					correctIndent = true;
					indent = Math.Max (0, indent - 1);
				}

				// Auto-indent based on total fold count
				if (foldCount == 0) {
					indent = 0;
				}

				// Auto-indent based on matched close-open grouping operators
				if (lastLine.Count (c => new [] { '{', '}', '[', ']', '(', ')' }.Contains (c)) >= 2
						&& lastLine.IndexOfAny (new [] { '}', ']', ')' }) < lastLine.IndexOfAny (new [] { '{', '[', '(' })) {
					correctIndentExtra = true;
					correctIndent = true;
				}

				editingFinished = foldCount == 0;
				line += 1;

				// Test for negative (unfixable) grouping mismatch
				if (foldCount < 0) {

					// Clear buffer
					accum.Clear ();

					// Output error
					ANSI.WriteLine ($"{Red}Mismatched bracket, brace, or parenthesis group!");
					editingFinished = true;
				} else {

					// Append line to buffer
					accum.AppendLine (lastLine);
				}

				// Restore prompt
				Prompt.Pop ();
			}

			// Rewrite indent if fold count is 0
			if (correctIndent) rewriteIndent ();

			// Return buffer
			return accum.ToString ();
		}

		/// <summary>
		/// Execute code safely using the Iodine engine.
		/// </summary>
		/// <returns>The iodine operation.</returns>
		/// <param name="instance">Instance.</param>
		/// <param name="action">Action.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		static T WrapIodineOperation<T> (Shell instance, Func<T> action) where T: IodineObject {
			try {

				// Try to just run the specified operation
				return action ();
			} catch (Iodine.Compiler.SyntaxException e) {

				// Iterate over syntax errors
				foreach (var err in e.ErrorLog.Errors) {

					// Test if the error came from somewhere else
					if (!string.IsNullOrEmpty (err.Location.File)) {

						// Get the name of the file that caused the error
						var path = System.IO.Path.GetFileNameWithoutExtension (err.Location.File);

						// Print the error location and description
						ANSI.WriteLine ($"Error at {White}{path}{Default} ({err.Location.Line + 1}:{err.Location.Column}): {err.Text}");
						continue;
					}

					// Test if the error has an associated token
					if (err.HasToken) {

						// Get the length of the associated token
						var token_length = err.Token.Value.Length;

						// Print user input for later highlighting
						var line = instance.BufferedInput.Split ('\n') [err.Location.Line].Trim ();
						Console.Write (line);

						// Highlight the error
						//Console.CursorLeft = err.Location.Column - token_length;
						Console.CursorLeft = err.Location.Column;
						//ANSI.WriteLine ($"{DarkRed.bg}{White}{line.Substring (err.Location.Column - token_length, token_length)}");
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line.Substring (err.Location.Column, line [err.Location.Column] == '"' ? token_length + 2 : token_length)}");
						ANSI.WriteLine ($"{White}{string.Empty.PadLeft (token_length, '^').PadLeft (err.Location.Column)}");
					} else {

						// Print user input for later highlighting
						var line = instance.BufferedInput.Split ('\n') [err.Location.Line].Trim ();
						Console.Write (line);

						// Highlight the error
						Console.CursorLeft = err.Location.Column - 1;
						ANSI.WriteLine ($"{DarkRed.bg}{White}{line [err.Location.Column - 1]}");
						ANSI.WriteLine ($"{White}{"^".PadLeft (err.Location.Column, ' ')}");
					}

					// Print the error location and description
					ANSI.WriteLine ($"\n{White}SyntaxError{(err.Location.Line != 0 ? $" at line {err.Location.Line + 1}" : string.Empty)}: {Red}{err.Text}");
				}
			} catch (UnhandledIodineExceptionException e) {

				// Pretty print the underlying Iodine exception
				PrettyPrint.WriteLine (e.OriginalException);

				// Test if the error occurred in an actual module
				if (e.Frame.Module.Name != "__anonymous__") {

					// Print the stack trace
					e.PrintStack ();
				}
			} catch (ModuleNotFoundException e) {

				// Print the name of the missing module
				ANSI.WriteLine ($"Unable to find module '{White}{e.Name}{ANSI.ESC}[0m'");

				// Iterate over the search paths
				foreach (var searchPath in e.SearchPath) {

					// Create a relative URI of the search path
					var workingPathUri = new Uri (Environment.CurrentDirectory);
					var searchPathUri = new Uri (System.IO.Path.GetFullPath (searchPath), UriKind.RelativeOrAbsolute);
					var relativePathUri = workingPathUri.MakeRelativeUri (searchPathUri);

					// Print the relative search path
					ANSI.WriteLine ($"- ./{relativePathUri}");
				}
			} catch (Exception e) {

				// The Iodine engine exploded and we have no idea why.
				// Show the error to the user, maybe he can do something useful with it.
				Console.WriteLine (e.Message);
			}

			// Nothing to do here
			return null;
		}
	}
}