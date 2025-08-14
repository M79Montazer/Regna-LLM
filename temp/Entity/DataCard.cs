namespace temp.Entity
{
    public class DataCard
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string Tags { get; set; }  // e.g. comma-separated or JSON
        public int? Visibility { get; set; }

        public Embedding Embedding { get; set; }
    }
    public class Embedding
    {
        public int CardId { get; set; }  // FK = DataCard.Id
        public byte[] Vector { get; set; }

        public DataCard Card { get; set; }
    }
}
