namespace PerfXml;

public static class Extensions {
	public static bool TryWriteTo<T>(this IXmlFormatterResolver resolver,
		Span<char> span,
		T value,
		out int charsWritten) =>
		resolver.GetRequiredFormatter<T>().TryWriteTo(span, value, out charsWritten, resolver);

	public static T Parse<T>(this IXmlFormatterResolver resolver, ReadOnlySpan<char> span) =>
		resolver.GetRequiredFormatter<T>().Parse(span, resolver);
}
