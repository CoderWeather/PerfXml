using System.Globalization;
using PerfXml.Resolvers;

namespace PerfXml;

public static class Xml {
	static Xml() {
		DateTimeFormat = DateTimeFormatInfo.InvariantInfo;
		DateTimeFormat.ShortTimePattern = "HH:mm";
		DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
		DateTimeFormat.FullDateTimePattern = "yyyy-MM-dd HH:mm:ss";
	}

	internal static readonly DateTimeFormatInfo DateTimeFormat;

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
		where T : IXmlSerialization {
		return XmlWriteBuffer.SerializeStatic(obj);
	}

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
