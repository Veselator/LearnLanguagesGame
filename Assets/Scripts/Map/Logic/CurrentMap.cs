namespace LearnLanguages.Map.Logic
{
    public interface ICurrentMap
    {
        Data.Map Map { get; }
    }

    public sealed class CurrentMap : ICurrentMap
    {
        public Data.Map Map { get; private set; }

        public void Set(Data.Map map) => Map = map;
    }
}
