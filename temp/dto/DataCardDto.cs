namespace temp.dto
{
    public class DataCardDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public string Tags { get; set; } = "";
        public int? Visibility { get; set; }
        public float[]? Vector { get; set; }
    }
}
