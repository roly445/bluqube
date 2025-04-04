using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class ResponderInputDefinitionProcessor : InputDefinitionProcessor<ResponderInputDefinitionProcessor.InputDefinition>
    {
        public override bool CanProcess(SyntaxNode syntaxNode)
        {
            TypeDeclarationSyntax typeDeclarationSyntax;
            switch (syntaxNode)
            {
                case RecordDeclarationSyntax recordDeclarationSyntax:
                    typeDeclarationSyntax = recordDeclarationSyntax;
                    break;
                case ClassDeclarationSyntax classDeclarationSyntax:
                    typeDeclarationSyntax = classDeclarationSyntax;
                    break;
                default:
                    return false;
            }

            return typeDeclarationSyntax.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => attribute.Name.ToString() == "BluQubeResponder");
        }

        protected override InputDefinition? ProcessInternal(SyntaxNode syntaxNode)
        {
            TypeDeclarationSyntax typeDeclarationSyntax;
            switch (syntaxNode)
            {
                case RecordDeclarationSyntax recordDeclarationSyntax:
                    typeDeclarationSyntax = recordDeclarationSyntax;
                    break;
                case ClassDeclarationSyntax classDeclarationSyntax:
                    typeDeclarationSyntax = classDeclarationSyntax;
                    break;
                default:
                    return null;
            }

            if (typeDeclarationSyntax.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => attribute.Name.ToString() == "BluQubeResponder"))
            {
                return new InputDefinition(typeDeclarationSyntax);
            }

            return null;
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeDeclarationSyntax typeWithAttribute)
            {
                this.TypeWithAttribute = typeWithAttribute;
            }

            public TypeDeclarationSyntax TypeWithAttribute { get; }
        }
    }
}