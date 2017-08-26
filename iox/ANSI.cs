namespace iox {
	using System;
	using System.Text;

	/// <summary>
	/// ANSI helper.
	/// </summary>
	public static class ANSI {

		/// <summary>
		/// ANSI escape character.
		/// </summary>
		internal const char ESC = '\x1b';

		/// <summary>
		/// Sanitize an ANSI-escaped string.
		/// </summary>
		/// <returns>The sanitize.</returns>
		/// <param name="str">String.</param>
		public static string Sanitize (string str) => $"{ESC}[0m{str}{ESC}[0m";

		/// <summary>
		/// Print an ANSI-escaped string.
		/// </summary>
		/// <param name="str">String.</param>
		public static void WriteLine (string str) {
			Write (str);
			Console.WriteLine ();
		}

		/// <summary>
		/// Print an ANSI-escaped string.
		/// </summary>
		/// <param name="str">String.</param>
		public static void Write (string str) {
			WriteInternal (Sanitize (str));
		}

		/// <summary>
		/// Test if a string contains one or more ANSI escape sequence.
		/// </summary>
		/// <returns><c>true</c>, if escapes was containsed, <c>false</c> otherwise.</returns>
		/// <param name="str">String.</param>
		public static bool ContainsEscapes (string str) => Purify (str) != str;

		/// <summary>
		/// Purify a string by removing all ANSI escapes.
		/// </summary>
		/// <returns>The purify.</returns>
		/// <param name="str">String.</param>
		public static string Purify (string str) {

			// Initialize state
			var receivedEscape = false;
			var inEscapeSequence = false;
			var escapeSequenceFinished = false;
			var accum = new StringBuilder ();

			// Iterate over all characters
			foreach (var chr in str) {

				// Test if an escape sequence is ready to be printed
				if (escapeSequenceFinished) {
					escapeSequenceFinished = false;
				}

				// Test if we're inside of an escape sequence
				if (inEscapeSequence) {
					
					// Test if we're at the end of the sequence
					if (chr != 'm') {
						continue;
					}

					// Update state
					escapeSequenceFinished = true;
					inEscapeSequence = false;
					continue;
				}

				// Test if we've received the escape character
				if (receivedEscape) {
					receivedEscape = false;
					if (chr == '[') {
						inEscapeSequence = true;
						continue;
					}
				}

				// Test for ANSI escape character
				if (chr == ESC) {
					receivedEscape = true;
					continue;
				}

				// Nothing special, just print the char
				accum.Append (chr);
			}

			return accum.ToString ();
		}

		/// <summary>
		/// Print an ANSI-escaped string.
		/// </summary>
		/// <param name="str">String.</param>
		static void WriteInternal (string str) {

			// Initialize state
			var bold = false;
			var boldCount = 0;
			var receivedEscape = false;
			var inEscapeSequence = false;
			var escapeSequenceFinished = false;
			var escapeSequence = new StringBuilder ();
			var savedForeground = Console.ForegroundColor;
			var savedBackground = Console.BackgroundColor;

			// Iterate over all characters
			foreach (var chr in str) {

				// Test if an escape sequence is ready to be printed
				if (escapeSequenceFinished) {

					// Prepare parsing
					byte code;
					var sequenceString = escapeSequence.ToString ();

					// Update state
					escapeSequence.Clear ();
					escapeSequenceFinished = false;

					// Try parsing the escape sequence byte
					if (!byte.TryParse (sequenceString, out code)) {
						continue; // bail out
					}

					// Reset all
					if (code == 0) {
						bold = false;
						boldCount = 0;
						Console.ForegroundColor = savedForeground;
						Console.BackgroundColor = savedBackground;
					}

					// Enable bold
					else if (code == 1) {
						bold = true;
						boldCount++;
					}

					// Disable bold
					else if (code == 22) {
						boldCount -= boldCount > 0 ? 1 : 0;
						bold &= boldCount != 0;
					}

					// Set foreground color
					else if (code >= 30 && code <= 37) {
						Console.ForegroundColor = ANSIColor.ColorMap [code - 30 + (bold ? 8 : 0)];
					}

					// Reset foreground color
					else if (code == 39) {
						Console.ForegroundColor = savedForeground;
					}

					// Set background color
					else if (code >= 40 && code <= 47) {
						Console.BackgroundColor = ANSIColor.ColorMap [code - 40 + (bold ? 8 : 0)];
					}

					// Reset background color
					else if (code == 49) {
						Console.BackgroundColor = savedBackground;
					}
				}

				// Test if we're inside of an escape sequence
				if (inEscapeSequence) {

					// Test if we're at the end of the sequence
					if (chr != 'm') {
						escapeSequence.Append (chr);
						continue;
					}

					// Update state
					escapeSequenceFinished = true;
					inEscapeSequence = false;
					continue;
				}

				// Test if we've received the escape character
				if (receivedEscape) {
					receivedEscape = false;
					if (chr == '[') {
						inEscapeSequence = true;
						continue;
					}
				}

				// Test for ANSI escape character
				if (chr == ESC) {
					receivedEscape = true;
					continue;
				}

				// Nothing special, just print the char
				Console.Write (chr);
			}

			// Reset colors
			Console.ForegroundColor = savedForeground;
			Console.BackgroundColor = savedBackground;
		}
	}
}
