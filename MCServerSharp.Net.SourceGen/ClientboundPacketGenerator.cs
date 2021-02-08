using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MCServerSharp.Net.SourceGen
{
    [Generator]
    public class ClientboundPacketGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif 

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new AttributedStructSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retreive the populated receiver 
            if (context.SyntaxReceiver is not AttributedStructSyntaxReceiver receiver)
                return;

            foreach (StructDeclarationSyntax candidateStruct in receiver.Candidates)
            {
                string structTypeName = TypeDeclarationSyntaxExtensions.GetFullName(candidateStruct);
                INamedTypeSymbol? structSymbol = context.Compilation.GetTypeByMetadataName(structTypeName);

                if (structSymbol == null)
                {
                    // report diagnostic
                    continue;
                }

                ImmutableArray<AttributeData> attribs = structSymbol.GetAttributes();

                AttributeData attrib = attribs[0];

                IMethodSymbol? constructor = attrib.AttributeConstructor;

                ImmutableArray<TypedConstant> constructArgs = attrib.ConstructorArguments;

                ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs = attrib.NamedArguments;

                ImmutableArray<ISymbol> members = structSymbol.GetMembers();

            }
        }
    }

    public static class TypeDeclarationSyntaxExtensions
    {
        const char NESTED_CLASS_DELIMITER = '+';
        const char NAMESPACE_CLASS_DELIMITER = '.';
        const char TYPEPARAMETER_CLASS_DELIMITER = '`';

        public static string GetFullName(this TypeDeclarationSyntax source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var namespaces = new LinkedList<NamespaceDeclarationSyntax>();
            var types = new LinkedList<TypeDeclarationSyntax>();

            for (var parent = source.Parent; parent is object; parent = parent.Parent)
            {
                if (parent is NamespaceDeclarationSyntax @namespace)
                {
                    namespaces.AddFirst(@namespace);
                }
                else if (parent is TypeDeclarationSyntax type)
                {
                    types.AddFirst(type);
                }
            }

            var result = new StringBuilder();
            for (var item = namespaces.First; item is object; item = item.Next)
            {
                result.Append(item.Value.Name).Append(NAMESPACE_CLASS_DELIMITER);
            }
            for (var item = types.First; item is object; item = item.Next)
            {
                TypeDeclarationSyntax type = item.Value;
                AppendName(result, type);
                result.Append(NESTED_CLASS_DELIMITER);
            }
            AppendName(result, source);

            return result.ToString();
        }

        static void AppendName(StringBuilder builder, TypeDeclarationSyntax type)
        {
            builder.Append(type.Identifier.Text);
            int typeArguments = type.TypeParameterList?.ChildNodes()
                .Count(node => node is TypeParameterSyntax) ?? 0;

            if (typeArguments != 0)
                builder.Append(TYPEPARAMETER_CLASS_DELIMITER).Append(typeArguments);
        }
    }
}
