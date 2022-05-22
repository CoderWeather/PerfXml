namespace PerfXml.Resolvers;

public sealed class StandardResolver : IXmlFormatterResolver {
	private StandardResolver() { }

	public static readonly StandardResolver Instance = new();

	public IXmlFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

	private static class Cache<T> {
		public static readonly IXmlFormatter<T>? Formatter;

		static Cache() {
			Formatter ??= SystemResolver.Instance.GetFormatter<T>();
			Formatter ??= EnumResolver.Instance.GetFormatter<T>();
			Formatter ??= NullableStructFormatterResolver.Instance.GetFormatter<T>();
		}
	}
}
