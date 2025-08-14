using temp.dto;

namespace temp.Service.Interface
{
    public interface IRetrieverService
    {
        public void CreateDb(string jsonPath, string dbPath = "game.db");
        public void BuildFaissIndex();
        public List<DataCardDto> GetTopKCardsAsync(string query, int k, string lastChats);
    }
}
