namespace BluQube.SourceGeneration.Utilities
{
    internal class RecordParameterInfo
    {
        public RecordParameterInfo(string name, string typeName)
        {
            this.Name = name;
            this.TypeName = typeName;
        }

        public string Name { get; }

        public string TypeName { get; }
    }
}

