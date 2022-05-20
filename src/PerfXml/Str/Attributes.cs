namespace PerfXml.Str;

[AttributeUsage(AttributeTargets.Field)]
public class StrFieldAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field)]
public class StrOptionalAttribute : Attribute {
	private readonly int group;

	public StrOptionalAttribute(int group = 0) {
		this.group = group;
	}
}
