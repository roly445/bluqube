namespace BluQube.CodeGenerators.Contracts
{
    internal interface IOutputDefinitionProcessor<in TDefinition>
        where TDefinition : IOutputDefinition
    {
        string Process(TDefinition data);
    }
}