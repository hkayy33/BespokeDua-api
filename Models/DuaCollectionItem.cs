namespace BespokeDuaApi.Models
{
    public class DuaCollectionItem
    {
        public Guid ItemId { get; set; }
        public Guid CollectionId { get; set; }
        public Guid? DuaId { get; set; }
        public Guid? SunnahDuaId { get; set; }
        public int SortOrder { get; set; }

        public DuaCollection Collection { get; set; } = null!;
        public SavedDua? SavedDua { get; set; }
        public SavedSunnahDua? SavedSunnahDua { get; set; }
    }
}
