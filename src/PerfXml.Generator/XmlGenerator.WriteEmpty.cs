namespace PerfXml.Generator;

internal partial class XmlGenerator {
	private void WriteEmptyParseBody(IndentedTextWriter writer, ClassGenInfo cls) {
		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} bool ParseFullBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end)"
			   )) {
			writer.WriteLine("return default;");
		}
	}

	private void WriteEmptyParseSubBody(IndentedTextWriter writer, ClassGenInfo cls) {
		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner)"
			   )) {
			writer.WriteLine("return default;");
		}
	}

	private void WriteEmptyParseSubBodyByNames(IndentedTextWriter writer, ClassGenInfo cls) {
		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner)"
			   )) {
			writer.WriteLine("return default;");
		}
	}
}
