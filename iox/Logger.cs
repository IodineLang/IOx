namespace iox {
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;
	using _Logger = Kaliko.Logger;

	public static class Logger {
		public static void Info (string str) {
			Log (str, _Logger.Severity.Info);
		}
		public static void Warn (string str) {
			Log (str, _Logger.Severity.Warning);
		}
		public static void Critical (string str) {
			Log (str, _Logger.Severity.Critical);
		}
		static void Log (string str, _Logger.Severity severity) {
			try {
				_Logger.Write (str, severity);
			} catch (DirectoryNotFoundException ex) {
				var path = new Regex (@"[^']+").Matches (ex.Message).Cast<Match> ().ElementAtOrDefault (1)?.Value;
				if (path != null) {
					var directory = Path.GetDirectoryName (path);
					Directory.CreateDirectory (directory);
					_Logger.Write (str, severity);
				}
			}
		}
	}
}