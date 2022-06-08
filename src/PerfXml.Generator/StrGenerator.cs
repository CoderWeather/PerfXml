/*
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PerfXml.Generator;

[Generator]
internal sealed class StrGenerator : ISourceGenerator {
	public void Initialize(GeneratorInitializationContext context) {
/*#if DEBUG
		if (Debugger.IsAttached is false) {
			Debugger.Launch();
		}
#endif#1#
		context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
	}

	private class ClassGenInfo {
		public readonly INamedTypeSymbol Symbol;
		public readonly List<FieldGenInfo> Fields = new();

		public ClassGenInfo(INamedTypeSymbol symbol) {
			Symbol = symbol;
		}
	}

	private class FieldGenInfo {
		public readonly IFieldSymbol Field;
		public readonly int? Group;
		public readonly string? DefaultValue;

		public FieldGenInfo(IFieldSymbol fieldSymbol,
			int? group,
			VariableDeclaratorSyntax variableDeclaratorSyntax) {
			Field = fieldSymbol;
			Group = group;
			DefaultValue = variableDeclaratorSyntax.Initializer?.Value.ToString();
		}
	}

	public void Execute(GeneratorExecutionContext context) {
		try {
			ExecuteInternal(context);
		}
		catch (Exception e) {
			var descriptor = new DiagnosticDescriptor(nameof(StrGenerator),
				"Error",
				e.ToString(),
				"Error",
				DiagnosticSeverity.Error,
				true);
			var diagnostic = Diagnostic.Create(descriptor, Location.None);
			context.ReportDiagnostic(diagnostic);
		}
	}

	public static void ExecuteInternal(GeneratorExecutionContext context) {
		if (context.SyntaxReceiver is not SyntaxReceiver receiver)
			return;

		var compilation = context.Compilation;

		var attributeSymbol = compilation.GetTypeByMetadataName("PerfXml.Str.StrFieldAttribute");
		var groupAttributeSymbol = compilation.GetTypeByMetadataName("PerfXml.Str.StrOptionalAttribute");

		var classes = new Dictionary<INamedTypeSymbol, ClassGenInfo>(SymbolEqualityComparer.Default);

		foreach (var field in receiver.CandidateFields) {
			var model = compilation.GetSemanticModel(field.SyntaxTree);
			foreach (var variable in field.Declaration.Variables) {
				if (model.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
					continue;

				var fieldAttr = fieldSymbol.GetAttributes()
				   .SingleOrDefault(ad =>
						ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);
				var groupAttr = fieldSymbol.GetAttributes()
				   .SingleOrDefault(ad =>
						ad.AttributeClass?.Equals(groupAttributeSymbol, SymbolEqualityComparer.Default) ?? false);

				if (fieldAttr == null)
					continue;
				if (!classes.TryGetValue(fieldSymbol.ContainingType, out var classInfo)) {
					classInfo = new(fieldSymbol.ContainingType);
					classes[fieldSymbol.ContainingType] = classInfo;
				}

				int? group = null;
				if (groupAttr is not null)
					group = (int?)groupAttr.ConstructorArguments[0].Value;
				classInfo.Fields.Add(new(fieldSymbol, group, variable));
			}
		}

		foreach (var info in classes) {
			var classSource = ProcessClass(info.Value.Symbol, info.Value, classes);
			if (classSource is null)
				continue;

			var className = $"{info.Value.Symbol.ContainingNamespace}.{info.Value.Symbol.Name}";
			context.AddSource($"{nameof(StrGenerator)}_{className}.cs",
				SourceText.From(classSource, Encoding.UTF8));
		}
	}

	private static string? ProcessClass(INamedTypeSymbol classSymbol,
		ClassGenInfo classGenInfo,
		IReadOnlyDictionary<INamedTypeSymbol, ClassGenInfo> classes) {
		var writer = new IndentedTextWriter(new StringWriter(), "    ");
		writer.WriteLine("using PerfXml;");
		writer.WriteLine("using PerfXml.Str;");
		writer.WriteLine();

		var scope = new NestedClassScope(classSymbol);
		scope.Start(writer);
		writer.WriteLine(NestedClassScope.GetClsString(classSymbol));
		writer.WriteLine("{");
		writer.Indent++;

		WriteDeserializeMethod(writer, classGenInfo, classes);
		WriteSerializeMethod(writer, classGenInfo, classes);

		writer.Indent--;
		writer.WriteLine("}");
		scope.End(writer);

		var writerStr = writer.InnerWriter.ToString();
		return writerStr;
	}

	private static void WriteDeserializeMethod(IndentedTextWriter writer,
		ClassGenInfo classGenInfo,
		IReadOnlyDictionary<INamedTypeSymbol, ClassGenInfo> classes) {
		writer.WriteLine("public void Deserialize(ref StrReader reader)");
		writer.WriteLine("{");
		writer.Indent++;

		var groupsStarted = new HashSet<int>();
		int? currentGroup = null;
		foreach (var f in classGenInfo.Fields) {
			if (currentGroup != f.Group) {
				if (currentGroup is not null) {
					writer.Indent--;
					writer.WriteLine("}");
				}

				if (f.Group is not null) {
					const string conditionName = "read";

					if (groupsStarted.Add(f.Group.Value))
						writer.WriteLine($"var {conditionName}{f.Group.Value} = reader.HasRemaining();");

					writer.WriteLine($"if ({conditionName}{f.Group.Value})");
					writer.WriteLine("{");
					writer.Indent++;
				}

				currentGroup = f.Group;
			}

			var typeToRead = (INamedTypeSymbol)f.Field.Type;
			ExtractNullable(ref typeToRead); // don't need to do anything special to assign to nullable if it is
			if (classes.ContainsKey(typeToRead)) {
				// todo: doesn't support other compilations
				writer.WriteLine($"{f.Field.Name} = new {typeToRead.Name}();");
				writer.WriteLine($"{f.Field.Name}.Deserialize(ref reader);");
			}
			else {
				var reader = GetReaderForType(typeToRead.Name);
				writer.WriteLine($"{f.Field.Name} = {reader};");
			}
		}

		if (currentGroup is not null) {
			writer.Indent--;
			writer.WriteLine("}");
		}

		writer.Indent--;
		writer.WriteLine("}");
	}

	private static void WriteSerializeMethod(IndentedTextWriter writer,
		ClassGenInfo classGenInfo,
		IReadOnlyDictionary<INamedTypeSymbol, ClassGenInfo> classes) {
		writer.WriteLine("public void Serialize(ref StrWriter writer)");
		writer.WriteLine("{");
		writer.Indent++;
		{
			var allGroups = new HashSet<int>();
			foreach (var f in classGenInfo.Fields)
				if (f.Group is not null)
					allGroups.Add(f.Group.Value);

			const string conditionName = "doGroup";

			var setupGroups = new HashSet<int>();
			var groupConditions = new List<string>();
			foreach (var field in classGenInfo.Fields)
				if (field.Group is not null && setupGroups.Add(field.Group.Value)) {
					var boolOrs = new List<string>();
					foreach (var existingGroup in allGroups) {
						if (existingGroup <= field.Group)
							continue;
						boolOrs.Add($"{conditionName}{existingGroup}");
					}

					boolOrs.Add($"{field.Field.Name} != {field.DefaultValue}");
					groupConditions.Add($"bool {conditionName}{field.Group} = {string.Join(" || ", boolOrs)};");
				}

			groupConditions.Reverse();
			foreach (var condition in groupConditions)
				writer.WriteLine(condition);

			int? currentGroup = null;
			foreach (var f in classGenInfo.Fields) {
				if (currentGroup != f.Group) {
					if (currentGroup is not null) {
						writer.Indent--;
						writer.WriteLine("}");
					}

					if (f.Group is not null) {
						writer.WriteLine($"if ({conditionName}{f.Group.Value})");
						writer.WriteLine("{");
						writer.Indent++;
					}

					currentGroup = f.Group;
				}

				var typeToWrite = (INamedTypeSymbol)f.Field.Type;
				var toWrite = f.Field.Name;
				if (ExtractNullable(ref typeToWrite))
					toWrite += ".Value";
				if (classes.ContainsKey(typeToWrite)) {
					// todo: doesn't support other compilations
					writer.WriteLine($"{toWrite}.Serialize(ref writer);");
				}
				else {
					var writerFunc = GetWriterForType(typeToWrite.Name, toWrite);
					writer.WriteLine($"{writerFunc};");
				}
			}

			if (currentGroup is not null) {
				writer.Indent--;
				writer.WriteLine("}");
			}
		}
		writer.Indent--;
		writer.WriteLine("}");
	}

	private static bool ExtractNullable(ref INamedTypeSymbol type) {
		if (type.Name != "Nullable")
			return false;
		type = (INamedTypeSymbol)type.TypeArguments[0];
		return true;
	}

	public static string GetWriterForType(string type, string toWrite) {
		var result = type switch {
			"Int32"   => $"writer.PutInt({toWrite})",
			"Double"  => $"writer.PutDouble({toWrite})",
			"String"  => $"writer.PutString({toWrite})",
			"ReadOnlySpan<char>" => $"writer.PutString({toWrite})",
			_         => throw new($"GetWriterForType: {type}")
		};
		return result;
	}

	public static string GetReaderForType(string type) {
		var result = type switch {
			"Int32"   => "reader.GetInt()",
			"Double"  => "reader.GetDouble()",
			"String"  => "reader.GetString().ToString()",
			"ReadOnlySpan<char>" => "reader.GetReadOnlySpan<char>ing()",
			_         => throw new($"GetReaderForType: {type}")
		};
		return result;
	}

	private class SyntaxReceiver : ISyntaxReceiver {
		public List<FieldDeclarationSyntax> CandidateFields { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
			if (syntaxNode is FieldDeclarationSyntax { AttributeLists.Count: > 0 } fieldDeclarationSyntax)
				CandidateFields.Add(fieldDeclarationSyntax);
		}
	}
}
*/



