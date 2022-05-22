using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PerfXml.Generator;

internal class SyntaxReceiver : ISyntaxReceiver {
	public List<TypeDeclarationSyntax> Classes { get; } = new();
	public List<FieldDeclarationSyntax> Fields { get; } = new();
	public List<PropertyDeclarationSyntax> Properties { get; } = new();

	public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
		switch (syntaxNode) {
			case ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax:
				Classes.Add(classDeclarationSyntax);
				break;
			case RecordDeclarationSyntax { AttributeLists.Count: > 0 } recordDeclarationSyntax:
				Classes.Add(recordDeclarationSyntax);
				break;
			case FieldDeclarationSyntax { AttributeLists.Count: > 0 } fieldDeclarationSyntax:
				Fields.Add(fieldDeclarationSyntax);
				break;
			case PropertyDeclarationSyntax { AttributeLists.Count: > 0 } propertyDeclarationSyntax:
				Properties.Add(propertyDeclarationSyntax);
				break;
		}
	}
}

internal sealed class ClassGenInfo {
	public readonly INamedTypeSymbol Symbol;
	public readonly List<BaseMemberGenInfo> XmlAttributes = new();
	public readonly List<BaseMemberGenInfo> XmlBodies = new();
	public bool InheritedClassName = false;
	public bool AlreadyHasEmptyConstructor = false;
	public bool InheritedFromSerializable = false;
	public bool HaveGenericElements = false;

	public string? ClassName;

	public ClassGenInfo(INamedTypeSymbol symbol) {
		Symbol = symbol;
	}

	public string? AdditionalMethodsModifiers =>
		InheritedFromSerializable
			? " override"
			: Symbol.IsSealed is false || Symbol.IsAbstract
				? " virtual"
				: null;
}

internal abstract class BaseMemberGenInfo {
	public ISymbol Symbol { get; }
	public ITypeSymbol OriginalType { get; }
	public ITypeSymbol Type { get; }
	public string? XmlName;
	public char? SplitChar;

	protected BaseMemberGenInfo(ISymbol symbol, ITypeSymbol type) {
		Symbol = symbol;
		Type = type;
		OriginalType = Type.IsDefinition ? Type : Type.OriginalDefinition;
	}
}

internal sealed class FieldGenInfo : BaseMemberGenInfo {
	public new IFieldSymbol Symbol { get; }

	public FieldGenInfo(IFieldSymbol fieldSymbol) :
		base(fieldSymbol, fieldSymbol.Type) {
		Symbol = fieldSymbol;
	}
}

internal sealed class PropertyGenInfo : BaseMemberGenInfo {
	public new IPropertySymbol Symbol { get; }

	public PropertyGenInfo(IPropertySymbol propertySymbol) :
		base(propertySymbol, propertySymbol.Type) {
		Symbol = propertySymbol;
	}
}
