namespace PerfXml;

[AttributeUsage(AttributeTargets.Class)]
public sealed class XmlClsAttribute : Attribute {
	public readonly string? Name;

	public XmlClsAttribute(string? name = default) {
		Name = name;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class XmlFieldAttribute : Attribute {
	public readonly string Name;

	public XmlFieldAttribute(string name) {
		Name = name;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class XmlBodyAttribute : Attribute {
	public readonly string? Name;
	public readonly bool TakeNameFromType;

	public XmlBodyAttribute(string? name = null, bool takeNameFromType = false) {
		Name = name;
		TakeNameFromType = takeNameFromType;
	}

	public XmlBodyAttribute(bool takeNameFromType) {
		TakeNameFromType = takeNameFromType;
	}
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class XmlSplitStrAttribute : Attribute {
	public readonly char SplitOn;

	public XmlSplitStrAttribute(char splitOn = ',') {
		SplitOn = splitOn;
	}
}
