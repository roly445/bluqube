using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class QueryInputDefinitionProcessor : InputDefinitionProcessor<QueryInputDefinitionProcessor.InputDefinition>
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
                    simpleBaseTypeSyntax.Type is GenericNameSyntax genericNameSyntax1 &&
                    !typeDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword)) &&
                    genericNameSyntax1.Identifier.Text == "IQuery" &&
                    genericNameSyntax1.TypeArgumentList.Arguments.Count == 1)
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
                    simpleBaseTypeSyntax.Type is GenericNameSyntax genericNameSyntax1 &&
                    !typeDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword)) &&
                    genericNameSyntax1.Identifier.Text == "IQuery" &&
                    genericNameSyntax1.TypeArgumentList.Arguments.Count == 1)
                {
                    var bluQubeQueryAttributeSyntax = typeDeclarationSyntax
                        .AttributeLists.SelectMany(x => x.Attributes)
                        .Single(x => x.Name.GetFirstToken().ToString() == "BluQubeQuery");

                    return new InputDefinition(
                        typeDeclarationSyntax, genericNameSyntax1.TypeArgumentList.Arguments[0],
                        bluQubeQueryAttributeSyntax);
                }
            }

            return null;
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeDeclarationSyntax queryDeclaration, TypeSyntax queryResultDeclaration, AttributeSyntax bluQubeQueryAttributeSyntax)
            {
                this.QueryDeclaration = queryDeclaration;
                this.QueryResultDeclaration = queryResultDeclaration;
                this.BluQubeQueryAttributeSyntax = bluQubeQueryAttributeSyntax;
            }

            public TypeDeclarationSyntax QueryDeclaration { get; }

            public TypeSyntax QueryResultDeclaration { get; }

            public AttributeSyntax BluQubeQueryAttributeSyntax { get; }
        }
    }
}