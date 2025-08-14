using Microsoft.EntityFrameworkCore;
using SentenceTransformers.MiniLM;
using System.Reflection.Metadata;
using System.Text.Json;
using temp.dto;
using temp.Entity;
using temp.Service.Interface;
using FaissMask;

namespace temp.Service
{
    public class RetrieverService : IRetrieverService, IDisposable
    {
        private readonly SentenceEncoder _encoder;
        private const int Dim = 384;
        private readonly IndexFlatL2 _index;
        private readonly IndexIDMap? _indexIdMap;
        private bool _disposed = false;
        public RetrieverService(SentenceEncoder encoder)
        {
            _encoder = encoder;
            _index = new IndexFlatL2(Dim);
            _indexIdMap = new IndexIDMap(_index);
            CreateDb("Content/cards.json");
            BuildFaissIndex();
        }
        public void CreateDb(string jsonPath, string dbPath = "game.db")
        {
            if (File.Exists(dbPath))
            {
                Console.WriteLine("Database already exists. Skipping initialization.");
                return;
            }
            Console.WriteLine("No DB found. Creating new game.db...");
            using var db = new RegnaDb();
            db.Database.EnsureCreated();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = File.ReadAllText(jsonPath);
            var cards = JsonSerializer.Deserialize<List<DataCard>>(json, options);
            if (cards is null)
            {
                throw new Exception();
            }


            foreach (var raw in cards)
            {
                var card = new DataCard
                {
                    Title = raw.Title,
                    Text = raw.Text,
                    Tags = string.Join(",", raw.Tags),
                    Visibility = raw.Visibility
                };

                db.DataCards.Add(card);
                db.SaveChanges(); // Get auto-generated Id

                // Generate embedding
                var vec = _encoder.Encode([raw.Text])[0];
                var blob = new byte[vec.Length * sizeof(float)];
                Buffer.BlockCopy(vec, 0, blob, 0, blob.Length);

                db.Embeddings.Add(new Embedding
                {
                    CardId = card.Id,
                    Vector = blob
                });

                db.SaveChanges();
            }

            Console.WriteLine("Database initialized successfully.");
        }

        public void BuildFaissIndex()
        {
            if (_indexIdMap is null)
            {
                throw new Exception();
            }
            using var db = new RegnaDb();
            var dataCardDtos = db.Embeddings.Include(a => a.Card).ToList().Select(a =>
            {
                var floatCount = a.Vector.Length / sizeof(float);
                var floatVec = new float[floatCount];
                Buffer.BlockCopy(a.Vector, 0, floatVec, 0, a.Vector.Length);
                return new DataCardDto
                {
                    Id = a.Card.Id,
                    Text = a.Card.Text,
                    Tags = a.Card.Tags,
                    Title = a.Card.Title,
                    Visibility = a.Card.Visibility,
                    Vector = floatVec
                };

            }).ToList();
            var cardIds = dataCardDtos.Select(a => Convert.ToInt64(a.Id)).ToArray();
            var vectors = dataCardDtos.Select(a => a.Vector ?? []).ToArray();

            
            _indexIdMap.Add(vectors, cardIds);
        }

        /// <summary>
        /// Returns the top-K DataCards most semantically relevant to `query`,
        /// filtered by the player's traits (visibility, region).
        /// </summary>
        public List<DataCardDto> GetTopKCardsAsync(string query, int k, string lastChats)
        {
            lastChats = lastChats.Length <= 200 ? lastChats : lastChats[^200..];

            var queryVec = _encoder.Encode([lastChats + "\r\n" + query])[0] ?? [];

            if (_indexIdMap is null)
            {
                throw new Exception();
            }
            var nearest = _indexIdMap.Search(queryVec, k);

            // 3) Extract the IDs in rank order
            var rankedCards = nearest
                .OrderBy(t => t.Distance)
                .Select(t =>new
                {
                    Label= Convert.ToInt32(t.Label),
                    Distance= t.Distance - 1f
                })
                .Where(a=>a.Distance < 0.9)
                .ToList();
            rankedCards = rankedCards.Where(a => a.Distance < 0.7).ToList();
            // 4) Load matching cards from SQLite via EF Core
            using var db = new RegnaDb();
            var cards = db.DataCards
                //.Where(c =>
                //    rankedIds.Contains(c.Id) &&
                //    c.Visibility <= traits.ArcaneLevel &&
                //    c.Tags.Split(',', StringSplitOptions.TrimEntries)
                //        .Contains(traits.Region, StringComparer.OrdinalIgnoreCase)
                //)
                .Select(a=>new DataCardDto
                {
                    Text = a.Text,
                    Id = a.Id,
                    Tags = a.Tags,
                    Title = a.Title,
                    Vector = null,
                    Visibility = a.Visibility
                })
                .ToList();

            // 5) Re‑order to match the ANN ranking
            var cardsById = cards.ToDictionary(c => c.Id);
            var ordered = rankedCards.Select(a=>a.Label)
                .Where(id => cardsById.ContainsKey(id))
                .Select(id => cardsById[id])
                .ToList();

            return ordered;
        }

        public void Dispose()
        {
            _index.Dispose();
            _indexIdMap?.Dispose();
            _disposed = true;
        }
    }

}
