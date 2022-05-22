namespace PerfXml;

public static class NodeNamesCollector {
	public static string GetFor<T>()
		where T : IXmlSerialization {
		return Cache<T>.NodeName
		 ?? throw new($"{typeof(T)} is not registered NodeName in {nameof(NodeNamesCollector)}");
	}

	public static void RegisterFor<T>(string nodeName)
		where T : IXmlSerialization {
		Cache<T>.NodeName = nodeName;
	}

	private static class Cache<T>
		where T : IXmlSerialization {
		// ReSharper disable once StaticMemberInGenericType
		public static string NodeName = default!;
	}
}
