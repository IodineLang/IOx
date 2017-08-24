namespace iox {
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// ANSI Base color.
	/// </summary>
	public enum ANSIBaseColor {
		Dark = 0,   // Black/DarkGray
		Red,        // DarkRed/Red
		Green,      // DarkGreen/Green
		Yellow,     // DarkYellow/Yellow
		Blue,       // DarkBlue/Blue
		Magenta,    // DarkMagenta/Magenta
		Cyan,       // DarkCyan/Cyan
		Light,      // Gray/White
	}

	/// <summary>
	/// ANSI Color.
	/// </summary>
	public class ANSIColor {

		// The colors
		public static readonly ANSIColor Default = new ANSIColor (Console.ForegroundColor, Console.BackgroundColor);
		public static readonly ANSIColor Black = new ANSIColor (ANSIBaseColor.Dark, bright: false);
		public static readonly ANSIColor DarkRed = new ANSIColor (ANSIBaseColor.Red, bright: false);
		public static readonly ANSIColor DarkGreen = new ANSIColor (ANSIBaseColor.Green, bright: false);
		public static readonly ANSIColor DarkYellow = new ANSIColor (ANSIBaseColor.Yellow, bright: false);
		public static readonly ANSIColor DarkBlue = new ANSIColor (ANSIBaseColor.Blue, bright: false);
		public static readonly ANSIColor DarkMagenta = new ANSIColor (ANSIBaseColor.Magenta, bright: false);
		public static readonly ANSIColor DarkCyan = new ANSIColor (ANSIBaseColor.Cyan, bright: false);
		public static readonly ANSIColor Gray = new ANSIColor (ANSIBaseColor.Light, bright: false);
		public static readonly ANSIColor DarkGray = new ANSIColor (ANSIBaseColor.Dark, bright: true);
		public static readonly ANSIColor Red = new ANSIColor (ANSIBaseColor.Red, bright: true);
		public static readonly ANSIColor Green = new ANSIColor (ANSIBaseColor.Green, bright: true);
		public static readonly ANSIColor Yellow = new ANSIColor (ANSIBaseColor.Yellow, bright: true);
		public static readonly ANSIColor Blue = new ANSIColor (ANSIBaseColor.Blue, bright: true);
		public static readonly ANSIColor Magenta = new ANSIColor (ANSIBaseColor.Magenta, bright: true);
		public static readonly ANSIColor Cyan = new ANSIColor (ANSIBaseColor.Cyan, bright: true);
		public static readonly ANSIColor White = new ANSIColor (ANSIBaseColor.Light, bright: true);

		/// <summary>
		/// ANSI code-to-color mapping.
		/// </summary>
		internal static readonly Dictionary<int, ConsoleColor> ColorMap
		= new Dictionary<int, ConsoleColor> {
			[00] = ConsoleColor.Black,
			[01] = ConsoleColor.DarkRed,
			[02] = ConsoleColor.DarkGreen,
			[03] = ConsoleColor.DarkYellow,
			[04] = ConsoleColor.DarkBlue,
			[05] = ConsoleColor.DarkMagenta,
			[06] = ConsoleColor.DarkCyan,
			[07] = ConsoleColor.Gray,
			[08] = ConsoleColor.DarkGray,
			[09] = ConsoleColor.Red,
			[10] = ConsoleColor.Green,
			[11] = ConsoleColor.Yellow,
			[12] = ConsoleColor.Blue,
			[13] = ConsoleColor.Magenta,
			[14] = ConsoleColor.Cyan,
			[15] = ConsoleColor.White,
		};

		/// <summary>
		/// The bright flag.
		/// </summary>
		readonly bool Bright;

		/// <summary>
		/// Whether the color has been constructed from a console color.
		/// </summary>
		readonly bool FromConsoleColor;

		readonly ConsoleColor FromConsoleFG;
		readonly ConsoleColor FromConsoleBG;

		/// <summary>
		/// The ANSI base color.
		/// </summary>
		readonly ANSIBaseColor BaseColor;

		/// <summary>
		/// Get the foreground color.
		/// </summary>
		/// <value>The foreground color.</value>
		public string fg {
			get { return Foreground (); }
		}

		/// <summary>
		/// Get the background color.
		/// </summary>
		/// <value>The background color.</value>
		public string bg {
			get { return Background (); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:iox.ANSIColor"/> class.
		/// </summary>
		/// <param name="baseColor">Base color.</param>
		/// <param name="bright">If set to <c>true</c>, make the color bright.</param>
		public ANSIColor (ANSIBaseColor baseColor, bool bright = false) {
			Bright = bright;
			BaseColor = baseColor;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:iox.ANSIColor"/> class.
		/// </summary>
		/// <param name="fg">Foreground color.</param>
		/// <param name="bg">Background color.</param>
		public ANSIColor (ConsoleColor fg, ConsoleColor bg) {
			FromConsoleColor = true;
			FromConsoleFG = fg;
			FromConsoleBG = bg;
		}

		/// <summary>
		/// Convert the ANSI color to a console color.
		/// </summary>
		/// <returns>The console color.</returns>
		public ConsoleColor ToConsoleColor () => ColorMap [GetLutEntry ()];

		/// <summary>
		/// Convert the ANSI color to an ANSI foreground color escape string.
		/// </summary>
		/// <returns>The ANSI foreground.</returns>
		public string Foreground () => ToAnsiString (foreground: true);

		/// <summary>
		/// Convert the ANSI color to an ANSI background color escape string.
		/// </summary>
		/// <returns>The ANSI background.</returns>
		public string Background () => ToAnsiString (foreground: false);

		/// <summary>
		/// Convert the ANSI color to an ANSI-escape string.
		/// </summary>
		/// <returns>The ANSI-escaped string.</returns>
		/// <param name="foreground">If set to <c>true</c> foreground.</param>
		string ToAnsiString (bool foreground) {
			bool bright = Bright;
			string colorString = $"{ANSI.ESC}[{(foreground ? 3 : 4)}{(int) BaseColor}m";
			if (FromConsoleColor) {
				var baseColor = (int) (foreground ? FromConsoleFG : FromConsoleBG);
				bright = baseColor >= 8;
				colorString = $"{ANSI.ESC}[{(foreground ? 3 : 4)}{(bright ? baseColor - 8 : baseColor)}m";
			}
			if (!Bright) return colorString;
			return $"{ANSI.ESC}[1m{colorString}{ANSI.ESC}[22m";
		}

		/// <summary>
		/// Gets the foreground base color.
		/// </summary>
		/// <returns>The foreground base color.</returns>
		public ANSIBaseColor GetForegroundBase () => FromConsoleColor ? (ANSIBaseColor) (int) FromConsoleFG : BaseColor;

		/// <summary>
		/// Gets the background base color.
		/// </summary>
		/// <returns>The background base color.</returns>
		public ANSIBaseColor GetBackgroundBase () => FromConsoleColor ? (ANSIBaseColor) (int) FromConsoleBG : BaseColor;

		/// <summary>
		/// Get the index into the BaseColor enumeration as string.
		/// </summary>
		/// <returns>The color index string.</returns>
		public string ToColorIndexString () => $"{(int) BaseColor}";

		/// <summary>
		/// Get the index into the color look-up table.
		/// </summary>
		/// <returns>The lut entry.</returns>
		int GetLutEntry () => ((int) BaseColor) + (Bright ? 8 : 0);

		public override string ToString () => Foreground ();
	}
}
