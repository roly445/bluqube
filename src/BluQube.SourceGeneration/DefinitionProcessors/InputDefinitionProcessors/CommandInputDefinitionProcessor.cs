using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class CommandInputDefinitionProcessor : InputDefinitionProcessor<CommandInputDefinitionProcessor.InputDefinition>
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

            foreach (var baseType in typeDeclarationSyntax.BaseList?.Types ?? Enumerable.Empty<BaseTypeSyntax>())
            {
                if (baseType is SimpleBaseTypeSyntax simpleBaseTypeSyntax &&
                    simpleBaseTypeSyntax.Type is IdentifierNameSyntax genericNameSyntax1 &&
                    !typeDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword)) &&
                    genericNameSyntax1.Identifier.Text == "ICommand")
                {
                    return true;
                }
            }

            return false;
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

            foreach (var baseType in typeDeclarationSyntax.BaseList?.Types ?? Enumerable.Empty<BaseTypeSyntax>())
            {
                if (baseType is SimpleBaseTypeSyntax simpleBaseTypeSyntax &&
                    simpleBaseTypeSyntax.Type is IdentifierNameSyntax genericNameSyntax1 &&
                    !typeDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword)) &&
                    genericNameSyntax1.Identifier.Text == "ICommand")
                {
                    var bluQubeQueryAttributeSyntax = typeDeclarationSyntax
                        .AttributeLists.SelectMany(x => x.Attributes)
                        .Single(x => x.Name.GetFirstToken().ToString() == "BluQubeCommand");

                    return new InputDefinition(
                        typeDeclarationSyntax,
                        bluQubeQueryAttributeSyntax);
                }
            }

            return null;
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeDeclarationSyntax commandDeclaration, AttributeSyntax bluQubeCommandAttributeSyntax)
            {
                this.CommandDeclaration = commandDeclaration;
                this.BluQubeCommandAttributeSyntax = bluQubeCommandAttributeSyntax;
            }

            public TypeDeclarationSyntax CommandDeclaration { get; }

            public AttributeSyntax BluQubeCommandAttributeSyntax { get; }
        }
    }
}