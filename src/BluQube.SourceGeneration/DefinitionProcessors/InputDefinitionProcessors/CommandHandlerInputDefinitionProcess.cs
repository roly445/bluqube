using System;
using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class CommandHandlerInputDefinitionProcess : InputDefinitionProcessor<CommandHandlerInputDefinitionProcess.InputDefinition>
    {
        public override bool CanProcess(SyntaxNode syntaxNode)
        {
            var relevantClasses = syntaxNode.DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.BaseList?.Types.Any(x =>
                    x.ToString().Contains("CommandHandler")) == true);

            if (relevantClasses?.BaseList == null || relevantClasses.BaseList.Types.Count == 0)
            {
                return false;
            }

            var baseType = relevantClasses.BaseList.Types.First();

            switch (baseType)
            {
                case SimpleBaseTypeSyntax simpleBase:
                    if (simpleBase.Type is GenericNameSyntax genericName)
                    {
                        return genericName.TypeArgumentList.Arguments.Count is 1 or 2;
                    }

                    break;

                case PrimaryConstructorBaseTypeSyntax primaryBase:
                    if (primaryBase.Type is GenericNameSyntax genericName2)
                    {
                        return genericName2.TypeArgumentList.Arguments.Count is 1 or 2;
                    }

                    break;
            }

            return false;
        }

        protected override InputDefinition ProcessInternal(SyntaxNode syntaxNode)
        {
            var relevantClasses = syntaxNode.DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.BaseList?.Types.Any(x =>
                    x.ToString().Contains("CommandHandler")) == true);

            var baseType = relevantClasses!.BaseList!.Types.First();

            switch (baseType)
            {
                case SimpleBaseTypeSyntax simpleBase:
                    if (simpleBase.Type is GenericNameSyntax genericName)
                    {
                        return new InputDefinition(genericName.TypeArgumentList.Arguments[0]);
                    }

                    break;

                case PrimaryConstructorBaseTypeSyntax primaryBase:
                    if (primaryBase.Type is GenericNameSyntax genericName2)
                    {
                        return new InputDefinition(genericName2.TypeArgumentList.Arguments[0]);
                    }

                    break;
            }

            throw new InvalidOperationException("Unable to process CommandHandler input definition.");
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeSyntax commandDeclaration)
            {
                this.CommandDeclaration = commandDeclaration;
            }

            public TypeSyntax CommandDeclaration { get; }
        }
    }
}