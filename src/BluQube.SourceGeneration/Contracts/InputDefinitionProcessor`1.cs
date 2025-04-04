using Microsoft.CodeAnalysis;

namespace BluQube.CodeGenerators.Contracts
{
    internal abstract class InputDefinitionProcessor<TDefinition>
        where TDefinition : IInputDefinition
    {
        public TDefinition? Process(SyntaxNode syntaxNode)
        {
            return this.CanProcess(syntaxNode) ?
                this.ProcessInternal(syntaxNode) : default(TDefinition);
        }

        public abstract bool CanProcess(SyntaxNode syntaxNode);

        protected abstract TDefinition? ProcessInternal(SyntaxNode syntaxNode);
    }
}