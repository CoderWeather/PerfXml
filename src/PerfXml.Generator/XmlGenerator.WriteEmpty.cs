namespace PerfXml.Generator;

internal partial class XmlGenerator {
	private static void WriteEmptyParseBody(IndentedTextWriter writer, ClassGenInfo cls) {
		writer.WriteLine(
			$"public{cls.AdditionalInheritanceMethodModifiers} bool ParseFullBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			writer.WriteLine("return default;");
		}
	}

	private static void WriteEmptyParseSubBody(IndentedTextWriter writer, ClassGenInfo cls) {
		writer.WriteLine(
			$"public{cls.AdditionalInheritanceMethodModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			writer.WriteLine("return default;");
		}
	}

	private static void WriteEmptyParseSubBodyByNames(IndentedTextWriter writer, ClassGenInfo cls) {
		writer.WriteLine(
			$"public{cls.AdditionalInheritanceMethodModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner, IXmlFormatterResolver resolver)"
		);
		using (NestedScope.Start(writer)) {
			writer.WriteLine("return default;");
		}
	}
}
