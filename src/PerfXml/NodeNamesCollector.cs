namespace PerfXml;

public static class NodeNamesCollector {
	public static string GetFor<T>()
		where T : IXmlSerialization, new() =>
		Cache<T>.NodeName;

	// public static void RegisterFor<T>(string nodeName)
	// 	where T : IXmlSerialization, new() {
	// 	Cache<T>.NodeName = nodeName;
	// 	Console.WriteLine($"Registered nodename for {typeof(T)}");
	// }

	private static class Cache<T>
		where T : IXmlSerialization, new() {
		public static readonly string NodeName;

		static Cache() {
			NodeName = new T().GetNodeName().ToString();
		}
	}
}
