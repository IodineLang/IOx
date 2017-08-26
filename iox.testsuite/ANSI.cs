using NUnit.Framework;
namespace iox.testsuite {
	
	[TestFixture]
	public class ANSI {

		[Test]
		public void Purify__Clean () {
			string [] tests = {
				"Hello, World!",
				"Woop woop",
				"1234567890\\mtest!",
				"\\x1234\\x2345[32mtest!",
			};
			foreach (var test in tests) {
				Assert.AreEqual (test, iox.ANSI.Purify (test));
			}
		}

		[Test]
		public void Purify__Dirty () {
			string [] dirty = {
				"Hello, \x1b[32mWorld!",
				"\x1234\x1b[0m\x2345",
			};
			string [] clean = {
				"Hello, World!",
				"\x1234\x2345",
			};
			for (var i = 0; i < dirty.Length; i++) {
				Assert.AreEqual (clean [i], iox.ANSI.Purify (dirty [i]));
			}
		}
	}
}
