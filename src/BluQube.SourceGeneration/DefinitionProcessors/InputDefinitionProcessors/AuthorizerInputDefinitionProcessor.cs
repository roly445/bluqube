using System.Collections.Generic;
using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class AuthorizerInputDefinitionProcessor : InputDefinitionProcessor<AuthorizerInputDefinitionProcessor.InputDefinition>
    {
        public override bool CanProcess(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            {
                return false;
            }

            if (classDeclaration.BaseList == null || classDeclaration.BaseList.Types.Count == 0)
            {
                return false;
            }

            return classDeclaration.BaseList.Types.Any(baseType =>
                baseType.ToString().Contains("AbstractRequestAuthorizer"));
        }

        protected override InputDefinition ProcessInternal(SyntaxNode syntaxNode)
        {
            var classDeclaration = (ClassDeclarationSyntax)syntaxNode;

            // Extract TRequest from AbstractRequestAuthorizer<TRequest>
            var baseType = classDeclaration.BaseList!.Types
                .First(bt => bt.ToString().Contains("AbstractRequestAuthorizer"));

            TypeSyntax? requestType = null;
            if (baseType is SimpleBaseTypeSyntax simpleBase &&
                simpleBase.Type is GenericNameSyntax genericName)
            {
                requestType = genericName.TypeArgumentList.Arguments[0];
            }

            // Extract UseRequirement calls from BuildPolicy method
            var buildPolicyMethod = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "BuildPolicy");

            var requirements = new List<string>();
            if (buildPolicyMethod?.Body != null)
            {
                var requirementCalls = buildPolicyMethod.Body.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(inv => inv.Expression.ToString().Contains("UseRequirement"));

                foreach (var call in requirementCalls)
                {
                    // Extract requirement type from: UseRequirement(new MustBeAuthenticatedRequirement())
                    var objectCreation = call.ArgumentList.Arguments
                        .FirstOrDefault()?.Expression as ObjectCreationExpressionSyntax;

                    if (objectCreation?.Type != null)
                    {
                        requirements.Add(objectCreation.Type.ToString());
                    }
                }
            }

            return new InputDefinition(requestType!, requirements);
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeSyntax requestDeclaration, List<string> requirements)
            {
                this.RequestDeclaration = requestDeclaration;
                this.Requirements = requirements;
            }

            public TypeSyntax RequestDeclaration { get; }

            public List<string> Requirements { get; }
        }
    }
}

