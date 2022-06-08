using PerfXml.Resolvers;

namespace PerfXml;

public static class Xml {
	private static IXmlFormatterResolver defaultResolver = StandardResolver.Instance;

	public static IXmlFormatterResolver DefaultResolver {
		get => defaultResolver;
		set {
			if (value != null!) {
				defaultResolver = value;
			}
		}
	}

	public static ReadOnlySpan<char> Serialize<T>(T obj)
		where T : IXmlSerialization =>
		XmlWriteBuffer.SerializeStatic(obj);

	public static void Serialize<T>(T obj,
		Span<char> span,
		out int charsWritten,
		IXmlFormatterResolver? resolver = null)
		where T : IXmlSerialization {
		resolver ??= DefaultResolver;
		XmlWriteBuffer.SerializeStatic(obj, span, out charsWritten, resolver);
	}

	public static T Deserialize<T>(ReadOnlySpan<char> span, IXmlFormatterResolver? resolver = null)
		where T : IXmlSerialization, new() {
		resolver ??= DefaultResolver;
		var reader = new XmlReadBuffer();
		return reader.Read<T>(span, resolver);
	}
}
