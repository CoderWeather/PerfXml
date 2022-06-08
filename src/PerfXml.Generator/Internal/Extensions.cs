namespace PerfXml.Generator.Internal;

internal static class Extensions {
	public static void WriteLines(this IndentedTextWriter writer, params string[] strings) {
		foreach (var s in strings) {
			writer.WriteLine(s);
		}
	}
}
