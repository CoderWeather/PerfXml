namespace PerfXml.Generator;

internal partial class XmlGenerator {
	private void WriteParseBody(IndentedTextWriter writer, ClassGenInfo cls) {
		var needsInlineBody = false;
		var needsSubBody = false;

		foreach (var body in cls.XmlBodies) {
			if (body.Type.IsPrimitive() && body.XmlName is null)
				needsInlineBody = true;
			else
				needsSubBody = true;
		}

		if (needsInlineBody && needsSubBody) {
			throw new($"{cls.Symbol.Name} needs inline body and sub body");
		}

		if (needsInlineBody) {
			using (NestedScope.Start(writer,
					   $"public{cls.AdditionalMethodsModifiers} bool ParseFullBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> bodySpan, ref int end)"
				   )) {
				foreach (var body in cls.XmlBodies) {
					if (body.Type.Name == "String") {
						writer.WriteLine(
							$"this.{body.Symbol.Name} = buffer.DeserializeCDATA(bodySpan, out end).ToString();");
					}
					else {
						throw new(
							$"Xml:WriteParseBodyMethods: how to inline body {body.Type.IsNativeIntegerType}");
					}
				}

				writer.WriteLine("return true;");
			}

			WriteEmptyParseSubBody(writer, cls);
			WriteEmptyParseSubBodyByNames(writer, cls);
		}
		else {
			if (cls.InheritedFromSerializable is false) {
				WriteEmptyParseBody(writer, cls);
			}

			if (needsSubBody) {
				WriteParseSubBody(writer, cls);
				WriteParseSubBodyByNames(writer, cls);
			}
			else if (cls.InheritedFromSerializable is false) {
				WriteEmptyParseSubBody(writer, cls);
				WriteEmptyParseSubBodyByNames(writer, cls);
			}
		}
	}

	private void WriteParseSubBody(IndentedTextWriter writer, ClassGenInfo cls) {
		var xmlBodies = cls.XmlBodies
		   .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
		   .Where(x => x is not FieldGenInfo field || field.Symbol.IsReadOnly is false)
		   .Where(x => x.Type is not ITypeParameterSymbol)
		   .ToArray();

		if (xmlBodies.Any() is false && cls.InheritedFromSerializable) {
			return;
		}

		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ulong hash, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner)"
			   )) {
			if (xmlBodies.Any() is false) {
				writer.WriteLine("return default;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable) {
				writer.WriteLine(
					"if (base.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner)) return true;"
				);
			}

			writer.WriteLine("switch (hash)");
			writer.WriteLine("{");
			writer.Indent++;

			foreach (var body in xmlBodies) {
				var isList = body.Type.IsList();

				ClassGenInfo? classToParse = null;
				if (body.Type.IsPrimitive() is false) {
					var type = isList
						? ((INamedTypeSymbol)body.Type).TypeArguments[0]
						: body.Type;

					classToParse = classes[type];
				}

				var nameToCheck =
					body.XmlName
				 ?? classToParse?.ClassName
				 ?? throw new InvalidDataException("no body name??");

				writer.WriteLine($"case {HashName(nameToCheck)}: {{");
				writer.Indent++;

				if (classToParse is not null) {
					if (isList) {
						writer.WriteLine(
							$"this.{body.Symbol.Name} ??= new();");
						writer.WriteLine(
							$"this.{body.Symbol.Name}.Add(buffer.Read<{classToParse.Symbol}>(bodySpan, out end));");
					}
					else {
						writer.WriteLine(
							$"if (this.{body.Symbol.Name} is not null) throw new InvalidDataException(\"duplicate non-list body {nameToCheck}\");");
						writer.WriteLine(
							$"this.{body.Symbol.Name} = buffer.Read<{classToParse.Symbol}>(bodySpan, out end);");
					}
				}
				else if (body.Type.Name == "String") {
					writer.WriteLine(
						$"this.{body.Symbol.Name} = buffer.DeserializeCDATA(innerBodySpan, out endInner).ToString();"
					);
				}
				else if (body.Type.IsPrimitive()) {
					writer.WriteLine(
						"var value = buffer.ReadNodeValue(innerBodySpan, out endInner);"
					);
					writer.WriteLine(
						$"this.{body.Symbol.Name} = {GetParseAction(body)};"
					);
				}
				else {
					throw new($"can't read body of type {body.Type.Name}");
				}

				writer.WriteLine("return true;");
				writer.Indent--;
				writer.WriteLine("}");
			}

			writer.Indent--;
			writer.WriteLine("}");

			writer.WriteLine("return false;");
		}
	}

	private void WriteParseSubBodyByNames(IndentedTextWriter writer, ClassGenInfo cls) {
		var xmlBodies = cls.XmlBodies
		   .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
		   .Where(x => x is not FieldGenInfo field || field.Symbol.IsReadOnly is false)
		   .Where(x => x.Type is ITypeParameterSymbol)
		   .ToArray();

		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} bool ParseSubBody(ref XmlReadBuffer buffer, ReadOnlySpan<char> nodeName, ReadOnlySpan<char> bodySpan, ReadOnlySpan<char> innerBodySpan, ref int end, ref int endInner)"
			   )) {
			if (xmlBodies.Any() is false) {
				writer.WriteLine("return default;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable) {
				writer.WriteLine(
					"if (base.ParseSubBody(ref buffer, hash, bodySpan, innerBodySpan, ref end, ref endInner)) return true;"
				);
			}

			var isFirst = true;
			foreach (var body in xmlBodies) {
				if (isFirst is false) {
					writer.Write("else ");
				}
				else {
					isFirst = false;
				}

				using (NestedScope.Start(writer,
						   $"if (nodeName.Equals(NodeNamesCollector.GetFor<{body.Type}>(), StringComparison.Ordinal))"
					   )) {
					writer.WriteLine(
						$"if (this.{body.Symbol.Name} is not null) throw new InvalidDataException(\"duplicate non-list body this.{body.Symbol.Name}\");"
					);
					writer.WriteLine(
						$"this.{body.Symbol.Name} = buffer.Read<{body.Type}>(bodySpan, out end);"
					);
					writer.WriteLine("return true;");
				}
			}

			writer.WriteLine("return false;");
		}
	}

	private void WriteParseAttribute(IndentedTextWriter writer, ClassGenInfo cls) {
		var xmlAttrs = cls.XmlAttributes
		   .Where(x => x.XmlName is not null)
		   .Where(x => x is not PropertyGenInfo prop || prop.Symbol.IsReadOnly is false)
		   .OrderBy(x => x.XmlName!.Length)
		   .ToArray();

		if (xmlAttrs.Any() is false && cls.InheritedFromSerializable) {
			return;
		}

		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} bool ParseAttribute(ref XmlReadBuffer buffer, ulong hash, SpanStr value)"
			   )) {
			if (xmlAttrs.Any() is false) {
				writer.WriteLine("return default;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable)
				writer.WriteLine("if (base.ParseAttribute(ref buffer, hash, value)) return true;");

			using (NestedScope.Start(writer, "switch (hash)")) {
				foreach (var attr in xmlAttrs) {
					using (NestedScope.Start(writer, $"case {HashName(attr.XmlName!)}:")) {
						if (attr.SplitChar is not null) {
							var typeToRead = ((INamedTypeSymbol)attr.Type).TypeArguments[0].Name;

							writer.WriteLine($"var lst = new List<{typeToRead}>();");
							writer.WriteLine($"var reader = new StrReader(value, '{attr.SplitChar}');");
							var readerMethod = GetReaderForType(typeToRead);

							using (NestedScope.Start(writer, "while (reader.HasRemaining())")) {
								writer.WriteLine($"lst.Add({readerMethod});");
							}

							writer.WriteLine($"this.{attr.Symbol.Name} = lst;");
						}
						else {
							var readCommand = GetParseAction(attr);
							writer.WriteLine($"this.{attr.Symbol.Name} = {readCommand};");
						}

						writer.WriteLine("return true;");
					}
				}
			}

			writer.WriteLine("return false;");
		}
	}

	private void WriteSerializeBody(IndentedTextWriter writer, ClassGenInfo cls) {
		if (cls.XmlBodies.Any() is false && cls.InheritedFromSerializable) {
			return;
		}

		using (NestedScope.Start(writer,
				   $"public{cls.AdditionalMethodsModifiers} void SerializeBody(ref XmlWriteBuffer buffer)"
			   )) {
			if (cls.XmlBodies.Any() is false) {
				writer.WriteLine("return;");
				NestedScope.CloseLast();
				return;
			}

			if (cls.InheritedFromSerializable)
				writer.WriteLine("base.SerializeBody(ref buffer);");

			foreach (var body in cls.XmlBodies) {
				var isCanBeNull = body.Type.IsReferenceType;

				NestedScope? isNotNullScope = null;
				if (isCanBeNull) {
					writer.WriteLine($"if (this.{body.Symbol.Name} is not null)");
					isNotNullScope = NestedScope.Start(writer);
				}

				if (body.Type.IsList()) {
					if (body.Type.IsPrimitive() || classes.ContainsKey(body.Type) is false) {
						throw new("for xml body of type list<T>, T must be IXmlSerialization");
					}

					writer.WriteLine($"foreach (var obj in this.{body.Symbol.Name})");
					using (NestedScope.Start(writer)) {
						writer.WriteLine("obj.Serialize(ref buffer);");
					}
				}
				// another IXmlSerialization
				else if (body.Type.IsPrimitive() is false) {
					writer.WriteLine($"this.{body.Symbol.Name}.Serialize(ref buffer);");
				}
				else {
					if (body.XmlName is not null) {
						writer.WriteLine("{");
						writer.Indent++;
						writer.WriteLine($"var node = buffer.StartNodeHead(\"{body.XmlName}\");");
					}

					if (body.Type.IsString()) {
						writer.WriteLine($"buffer.PutCData(this.{body.Symbol.Name});");
					}
					else if (body.Type.IsPrimitive()) {
						writer.WriteLine($"buffer.PutValue(this.{body.Symbol.Name});");
					}
					else {
						throw new($"how to put sub body {body.Type.Name}");
					}

					if (body.XmlName is not null) {
						writer.WriteLine("buffer.EndNode(ref node);");
						writer.Indent--;
						writer.WriteLine("}");
					}
				}

				isNotNullScope?.Close();
			}
		}
	}

	private void WriteSerializeAttributes(IndentedTextWriter writer, ClassGenInfo cls) {
		if (cls.XmlAttributes.Any() is false && cls.InheritedFromSerializable) {
			return;
		}

		writer.WriteLine($"public{cls.AdditionalMethodsModifiers} void SerializeAttributes(ref XmlWriteBuffer buffer)");
		using (NestedScope.Start(writer)) {
			if (cls.InheritedFromSerializable)
				writer.WriteLine("base.SerializeAttributes(ref buffer);");

			if (cls.XmlAttributes.Any() is false) {
				writer.WriteLine("return;");
				NestedScope.CloseLast();
				return;
			}

			foreach (var field in cls.XmlAttributes) {
				if (field.SplitChar is not null) {
					var typeToRead = ((INamedTypeSymbol)field.Type).TypeArguments[0].Name;

					using (NestedScope.Start(writer)) {
						writer.WriteLine($"using var writer = new StrWriter('{field.SplitChar}');");
						writer.WriteLine($"foreach (var val in {field.Symbol.Name})");
						writer.WriteLine("{");
						writer.Indent++;
						writer.WriteLine($"{GetWriterForType(typeToRead, "val")};");
						writer.Indent--;
						writer.WriteLine("}");
						writer.WriteLine($"buffer.PutAttribute(\"{field.XmlName}\", writer.m_builtSpan);");
						writer.WriteLine("writer.Dispose();");
					}
				}
				else {
					var writerAction = GetPutAttributeAction(field);
					writer.WriteLine(writerAction);
				}
			}
		}
	}

	private void WriteSerialize(IndentedTextWriter writer, ClassGenInfo cls) {
		if (cls.InheritedFromSerializable) {
			return;
		}

		writer.WriteLine("public void Serialize(ref XmlWriteBuffer buffer)");
		using (NestedScope.Start(writer)) {
			writer.WriteLine("var node = buffer.StartNodeHead(GetNodeName());");
			writer.WriteLine("SerializeAttributes(ref buffer);");
			writer.WriteLine("SerializeBody(ref buffer);");
			writer.WriteLine("buffer.EndNode(ref node);");
		}
	}
}
