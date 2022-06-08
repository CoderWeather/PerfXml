using PerfXml.Internal;

namespace PerfXml;

public interface IXmlFormatter {
	public TypeOf Type() => throw new InvalidOperationException();
}

public interface IXmlFormatter<T> : IXmlFormatter {
	TypeOf IXmlFormatter.Type() => TypeOf.Get<T>();

	bool TryWriteTo(Span<char> span, T value, out int charsWritten, IXmlFormatterResolver resolver);

	T Parse(ReadOnlySpan<char> span, IXmlFormatterResolver resolver);
}

public interface IXmlFormatterResolver {
	IXmlFormatter<T>? GetFormatter<T>();

	internal IXmlFormatter<T> GetRequiredFormatter<T>() =>
		GetFormatter<T>() ?? throw new($"No registered formatter for {typeof(T)}");
}
