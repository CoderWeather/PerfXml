namespace PerfXml;

public static class Xml {
	public static ReadOnlySpan<char> Serialize<T>(T obj)
		where T : IXmlSerialization {
		return XmlWriteBuffer.SerializeStatic(obj);
	}

	public static void Serialize<T>(T obj, Span<char> span, out int writtenChars)
		where T : IXmlSerialization {
		XmlWriteBuffer.SerializeStatic(obj, span, out writtenChars);
	}

	public static T Deserialize<T>(ReadOnlySpan<char> span)
		where T : IXmlSerialization, new() {
		return XmlReadBuffer.ReadStatic<T>(span);
	}
}
