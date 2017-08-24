namespace iox {
	using System;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Reflection helper.
	/// </summary>
	public static class ReflectionHelper {

		/// <summary>
		/// Get the Iodine build date and time.
		/// </summary>
		/// <returns>The iodine build date and time.</returns>
		public static DateTime? GetIodineBuildDate () {
			return GetIodineAssembly ().GetLinkerDateTime ();
		}

		/// <summary>
		/// Get the entry assembly version.
		/// </summary>
		/// <returns>The iox version.</returns>
		public static Version GetVersion () {
			return Assembly.GetEntryAssembly ().GetName ().Version;
		}

		/// <summary>
		/// Get the Iodine version.
		/// </summary>
		/// <returns>The iodine version.</returns>
		public static Version GetIodineVersion () {
			return GetIodineAssembly ().GetName ().Version;
		}

		/// <summary>
		/// Get the Iodine assembly.
		/// </summary>
		/// <returns>The iodine assembly.</returns>
		static Assembly GetIodineAssembly () {
			return typeof (Iodine.Compiler.IodineCompiler).Assembly;
		}

		/// <summary>
		/// Get the linker date and time of an assembly.
		/// </summary>
		/// <returns>The linker date time.</returns>
		/// <param name="assembly">The assembly.</param>
		/// <param name="tzi">The time zone info.</param>
		static DateTime GetLinkerDateTime (this Assembly assembly, TimeZoneInfo tzi = null) {
			
			// PE constants
			const int PE_OFF_HEADER = 60;
			const int PE_OFF_LINKER_TS = 8;

			// Declare variables
			int offset;
			int linkerSecondsSince1970;

			// Try reading the linker time from memory
			try {
				
				// Find base memory address
				var entryModule = assembly.ManifestModule;
				var hMod = Marshal.GetHINSTANCE (entryModule);
				if (hMod == IntPtr.Zero - 1) throw new Exception ("Failed to get HINSTANCE.");

				// Read the linker timestamp
				offset = Marshal.ReadInt32 (hMod, PE_OFF_HEADER);
				linkerSecondsSince1970 = Marshal.ReadInt32 (hMod, offset + PE_OFF_LINKER_TS);
			}

			// Fall back to reading the linker time from disk
			catch {
				
				// Read PE header
				var buffer = new byte [2048];
				using (var stream = new FileStream (assembly.Location, FileMode.Open, FileAccess.Read)) {
					stream.Read (buffer, 0, 2048);
				}

				// Read the linker timestamp
				offset = BitConverter.ToInt32 (buffer, PE_OFF_HEADER);
				linkerSecondsSince1970 = BitConverter.ToInt32 (buffer, offset + PE_OFF_LINKER_TS);
			}

			// Convert the timestamp to a DateTime
			var epoch = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var linkTimeUtc = epoch.AddSeconds (linkerSecondsSince1970);
			var dt = TimeZoneInfo.ConvertTimeFromUtc (linkTimeUtc, tzi ?? TimeZoneInfo.Local);
			return dt;
		}
	}
}
