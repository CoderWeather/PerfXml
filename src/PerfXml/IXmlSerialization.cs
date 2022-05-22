namespace PerfXml;

/// <summary>
/// Abstract base class for types that can be read from and written to XML using <see cref="XmlReadBuffer"/> and <see cref="XmlWriteBuffer"/>
/// </summary>
public interface IXmlSerialization {
	/// <summary>Gets the name of the node to be written</summary>
	/// <returns>Name of the node to be written</returns>
	public ReadOnlySpan<char> GetNodeName();

	public bool ParseFullBody(ref XmlReadBuffer buffer,
		ReadOnlySpan<char> bodySpan,
		ref int end,
		IXmlFormatterResolver resolver);

	public bool ParseSubBody(
		ref XmlReadBuffer buffer,
		ulong hash,
		ReadOnlySpan<char> bodySpan,
		ReadOnlySpan<char> innerBodySpan,
		ref int end,
		ref int endInner,
		IXmlFormatterResolver resolver
	);

	public bool ParseSubBody(
		ref XmlReadBuffer buffer,
		ReadOnlySpan<char> nodeName,
		ReadOnlySpan<char> bodySpan,
		ReadOnlySpan<char> innerBodySpan,
		ref int end,
		ref int endInner,
		IXmlFormatterResolver resolver
	);

	public bool ParseAttribute(ref XmlReadBuffer buffer,
		ulong hash,
		ReadOnlySpan<char> value,
		IXmlFormatterResolver resolver);

	public void SerializeBody(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver);

	public void SerializeAttributes(ref XmlWriteBuffer buffer, IXmlFormatterResolver resolver);

	public void Serialize(ref XmlWriteBuffer buffer,
		IXmlFormatterResolver resolver,
		ReadOnlySpan<char> nodeName = default);

	/// <summary>Calculate fast hash of attribute/node name</summary>
	/// <param name="name">Name to hash</param>
	/// <returns>Hashed value</returns>
	public static ulong HashName(ReadOnlySpan<char> name) {
		var hashedValue = 0x2AAAAAAAAAAAAB67ul;
		for (var i = 0; i < name.Length; i++) {
			hashedValue += name[i];
			hashedValue *= 0x2AAAAAAAAAAAAB6Ful;
		}

		return hashedValue;
	}
}
