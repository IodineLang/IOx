/*
 * The MIT License (MIT)	
 * Copyright (c) 2016 Fábio Junqueira
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

/* 
 * This is a *HEAVILY MODIFIED* version of the original source!
 * It behaves very differently and is tailored to the needs of iox.
 * 
 * If you want the original version, get it from here:
 * https://github.com/fjunqueira/hinter
 */

namespace iox {
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using static ANSIColor;

	/// <summary>
	/// Hinter.
	/// </summary>
	public static class __Hinter {
		public static string ReadHintedLine<T, TResult> (IEnumerable<T> hintSource, Func<T, TResult> hintField, ANSIColor hintColor, string inputRegex = ".*") {

			ConsoleKeyInfo input;
			var editComplete = true;
			var accum = string.Empty;
			var lastWord = string.Empty;
			var userInput = string.Empty;
			var suggestion = string.Empty;
			var initialCursorTop = Console.CursorTop;
			var initialCursorLeft = Console.CursorLeft;
			var lastInitialCursorTop = Console.CursorTop;
			var lastInitialCursorLeft = Console.CursorLeft;
			var localInitialCursorTop = Console.CursorTop;
			var localInitialCursorLeft = Console.CursorLeft;

#if DEBUG
			var __DEBUG_TRD = new System.Threading.Thread (() => {
				while (true) {
					Console.Title = ($"ACCUM: {accum} | USRIN: {userInput}");
					System.Threading.Thread.Sleep (100);
				}
			});
			__DEBUG_TRD.Start ();
#endif

			// Read next key
			while (ConsoleKey.Enter != (input = Console.ReadKey (intercept: true)).Key) {

				// Prepare state
				if (editComplete) {
					lastWord = string.Empty;
					userInput = string.Empty;
					suggestion = string.Empty;
					editComplete = false;
					lastInitialCursorTop = localInitialCursorTop;
					lastInitialCursorLeft = localInitialCursorLeft;
					localInitialCursorTop = Console.CursorTop;
					localInitialCursorLeft = Console.CursorLeft;
				}

				// Handle backspace
				if (input.Key == ConsoleKey.Backspace) {
					if (userInput.Any ()) {
						userInput = userInput.Any () ? userInput.Remove (userInput.Length - 1, 1) : string.Empty;
					} else {
						accum = accum.Remove (Math.Max (0, accum.Length - 1), accum.Length > 0 ? 1 : 0);
						editComplete = true;

						// Clear line
						Console.SetCursorPosition (initialCursorLeft, initialCursorTop);
						Console.Write (string.Empty.PadLeft (Console.WindowWidth - initialCursorLeft));
						Console.SetCursorPosition (initialCursorLeft, initialCursorTop);

						// Write finished line
						Console.Write (accum);
						continue;
					}
				}

				// Handle member access
				else if (input.Key == ConsoleKey.OemPeriod) {
					editComplete = true;
					userInput += input.KeyChar;
				}

				// Handle space
				else if (input.Key == ConsoleKey.Spacebar) {
					editComplete = true;
					userInput += input.KeyChar;
				}

				// Handle tab (accept suggestion)
				else if (input.Key == ConsoleKey.Tab) {
					editComplete = true;
					userInput = suggestion ?? userInput;
				}

				// Test if keychar is not alphanumeric
				else if (!char.IsLetterOrDigit (input.KeyChar)) {
					editComplete = true;
					userInput += input.KeyChar;
				}

				// Test if keychar matches input regex
				else if (Regex.IsMatch (input.KeyChar.ToString (), inputRegex)) {
					Console.CursorLeft++;
					userInput += input.KeyChar;
				}

				// Get the suggestion
				suggestion = (
					hintSource.Select (item => hintField (item).ToString ())
					.FirstOrDefault (
						item => (
							item.Length > userInput.Length
							&& item.Substring (0, userInput.Length) == userInput
						)
					)
				);

				// Get line
				lastWord = suggestion ?? userInput;

				// Clear line
				Console.SetCursorPosition (localInitialCursorLeft, localInitialCursorTop);
				Console.Write (string.Empty.PadLeft (Console.WindowWidth - localInitialCursorLeft));
				Console.SetCursorPosition (localInitialCursorLeft, localInitialCursorTop);

				// Write user input
				ANSI.Write ($"{(suggestion != null ? White : Default)}{userInput}");

				// Write suggestion
				if (userInput.Any ()) {
					ANSI.Write ($"{hintColor}{lastWord.Substring (userInput.Length, lastWord.Length - userInput.Length)}");
					if (editComplete) {

						// Clear line
						Console.SetCursorPosition (initialCursorLeft, initialCursorTop);
						Console.Write (string.Empty.PadLeft (Console.WindowWidth - initialCursorLeft));
						Console.SetCursorPosition (initialCursorLeft, initialCursorTop);

						// Write finished line
						accum += lastWord;
						Console.Write (accum);
					}
				}

				continue;
			}

			if (!editComplete) {
				accum += userInput;
			}

			// Clear line
			Console.SetCursorPosition (initialCursorLeft, initialCursorTop);
			Console.Write (string.Empty.PadLeft (Console.WindowWidth - initialCursorLeft));
			Console.SetCursorPosition (initialCursorLeft, initialCursorTop);

			// Write finished line
			Console.WriteLine (accum);

#if DEBUG
			__DEBUG_TRD.Abort ();
#endif

			// Return read line
			return accum;
		}
	}
}