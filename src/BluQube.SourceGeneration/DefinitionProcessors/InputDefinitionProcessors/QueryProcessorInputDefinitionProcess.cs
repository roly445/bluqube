using System;
using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class QueryProcessorInputDefinitionProcess : InputDefinitionProcessor<QueryProcessorInputDefinitionProcess.InputDefinition>
    {
        public override bool CanProcess(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is ClassDeclarationSyntax classDeclaration))
            {
                return false;
            }

            if (classDeclaration.BaseList == null || classDeclaration.BaseList.Types.Count == 0)
            {
                return false;
            }

            foreach (var baseType in classDeclaration.BaseList.Types)
            {
                if (!(baseType is SimpleBaseTypeSyntax simpleBaseTypeSyntax))
                {
                    continue;
                }

                if (!(simpleBaseTypeSyntax.Type is GenericNameSyntax genericNameSyntax) || genericNameSyntax.Identifier.Text != "IQueryProcessor" ||
                    genericNameSyntax.TypeArgumentList.Arguments.Count != 2)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        protected override InputDefinition ProcessInternal(SyntaxNode syntaxNode)
        {
            return new InputDefinition(((ClassDeclarationSyntax)syntaxNode).BaseList!
                .Types.Select(type =>
                    ((GenericNameSyntax)((SimpleBaseTypeSyntax)type).Type).TypeArgumentList.Arguments[0])
                .First());
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeSyntax queryDeclaration)
            {
                this.QueryDeclaration = queryDeclaration;
            }

            public TypeSyntax QueryDeclaration { get; }
        }
    }
}