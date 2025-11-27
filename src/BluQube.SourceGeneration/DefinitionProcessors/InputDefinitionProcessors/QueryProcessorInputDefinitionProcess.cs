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
            var baseList = ((ClassDeclarationSyntax)syntaxNode).BaseList!;
            var queryProcessorType = baseList.Types
                .Select(type => ((GenericNameSyntax)((SimpleBaseTypeSyntax)type).Type))
                .First(g => g.Identifier.Text == "IQueryProcessor");

            return new InputDefinition(
                queryProcessorType.TypeArgumentList.Arguments[0],
                queryProcessorType.TypeArgumentList.Arguments[1]);
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeSyntax queryDeclaration, TypeSyntax queryResultDeclaration)
            {
                this.QueryDeclaration = queryDeclaration;
                this.QueryResultDeclaration = queryResultDeclaration;
            }

            public TypeSyntax QueryDeclaration { get; }

            public TypeSyntax QueryResultDeclaration { get; }
        }
    }
}