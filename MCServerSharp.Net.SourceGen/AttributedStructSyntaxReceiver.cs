using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MCServerSharp.Net.SourceGen
{
    public class AttributedStructSyntaxReceiver : ISyntaxReceiver
    {
        public List<StructDeclarationSyntax> Candidates { get; } = new();

        /// <summary>
        /// Called for every syntax node in the compilation,
        /// inspect the nodes and save any information useful for generation.
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is StructDeclarationSyntax structDeclarationSyntax &&
                structDeclarationSyntax.AttributeLists.Count > 0)
            {
                Candidates.Add(structDeclarationSyntax);
            }
        }
    }
}
