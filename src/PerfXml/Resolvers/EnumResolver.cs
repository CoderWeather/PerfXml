using PerfXml.Formatters;

namespace PerfXml.Resolvers;

public sealed class EnumResolver : IXmlFormatterResolver {
	private EnumResolver() { }
	public static readonly EnumResolver Instance = new();

	public IXmlFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

	private static class Cache<T> {
		public static readonly IXmlFormatter<T>? Formatter;

		static Cache() {
			var type = typeof(T);
			if (type.IsValueType is false || type.IsEnum is false) {
				return;
			}

			var instance = Activator.CreateInstance(typeof(EnumFormatter<>).MakeGenericType(type));
			Formatter ??= instance as IXmlFormatter<T>;
		}
	}
}
