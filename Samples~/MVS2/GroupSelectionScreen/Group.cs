namespace Extreal.Integration.Multiplay.NGO.MVS2.GroupSelectionScreen
{
    public class Group
    {
        public string Id { get; private set; }
        public string Name { get; private set; }

        public Group(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
