namespace iox {
	public static class Versions {
		public static readonly string IoxVersion;
		public static readonly string IoxFullVersion;
		public static readonly string IodineVersion;
		public static readonly string IodineFullVersion;
		public static readonly string IodineBuildDate;

		static Versions () {
			var ioxAssemblyVersion = ReflectionHelper.GetVersion ();
			var iodineAssemblyVersion = ReflectionHelper.GetIodineVersion ();
			IoxVersion = ioxAssemblyVersion.ToString (2);
			IoxFullVersion = ioxAssemblyVersion.ToString ();
			IodineVersion = iodineAssemblyVersion.ToString (3);
			IodineFullVersion = iodineAssemblyVersion.ToString ();
			IodineBuildDate = ReflectionHelper.GetIodineBuildDate ()?.ToString ("MMM dd yyyy") ?? string.Empty;
		}
	}
}
