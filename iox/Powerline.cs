namespace iox {
	using System.Text;
	using static PowerlineHelper;

	/// <summary>
	/// Powerline helper.
	/// </summary>
	static class PowerlineHelper {

		// Powerline control characters
		public const string PLColor = "\xe0b0";
		public const string PLNoColor = "\xe0b2";
		public const string PLSeparate = "\xe0b1";
		public const string PLRSeparate = "\xe0b3";

		// Powerline symbols
		public const string PLBranch = "\xe0a0";
		public const string PLGear = "\x26ef";
	}

	/// <summary>
	/// Powerline builder.
	/// </summary>
	public class PowerlineBuilder {

		/// <summary>
		/// Get a new powerline builder instance.
		/// </summary>
		/// <returns>The powerline builder.</returns>
		public static PowerlineBuilder Powerline () => new PowerlineBuilder ();

		/// <summary>
		/// The string builder.
		/// </summary>
		readonly StringBuilder Builder;

		ANSIColor lastfg;
		ANSIColor lastbg;
		ANSIColor lastconsole;

		bool first;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:iox.PowerlineBuilder"/> class.
		/// </summary>
		public PowerlineBuilder () {
			Builder = new StringBuilder ();
			lastconsole = ANSIColor.Default;
			lastfg = new ANSIColor (lastconsole.GetForegroundBase ());
			lastbg = new ANSIColor (lastconsole.GetBackgroundBase ());
			first = true;
		}

		/// <summary>
		/// Add a segment to the powerline.
		/// </summary>
		/// <returns>The segment.</returns>
		/// <param name="text">Text.</param>
		/// <param name="fg">Foreground color.</param>
		/// <param name="bg">Background color.</param>
		public PowerlineBuilder Segment (string text, ANSIColor fg = null, ANSIColor bg = null) {
			// $"\x1b[39m\x1b[46m iox \x1b[43m\x1b[36m{PLColor}\x1b[30m test.id \x1b[40m\x1b[33m{PLColor}\x1b[0m"
			fg = fg ?? lastfg;
			bg = bg ?? lastbg;
			if (first) {
				lastfg = fg ?? lastfg;
				lastbg = bg ?? lastbg;
			}
			if (!first) Builder.Append ($"{bg.bg}{lastbg.fg}{PLColor}");
			Builder.Append ($"{bg.bg}{fg.fg} {text}");
			lastfg = fg;
			lastbg = bg;
			if (first) first = false;
			return this;
		}

		/// <summary>
		/// Convert the powerline to an ANSI string.
		/// </summary>
		/// <returns>The ANSI string.</returns>
		public string ToAnsiString () => ANSI.Sanitize ($"{Builder}{lastbg.fg}{ANSI.ESC}[49m{PLColor}");
	}
}