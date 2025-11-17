using Nexus.Core.Interfaces;
using Nexus.Core.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Nexus.Core.Services
{
    public class TagPredictor : IDisposable
    {
        private readonly EmbeddingService _embeddingService;
        private readonly ITopicsRepository _topicsRepo;
        private readonly Dictionary<string, float[]> _topicEmbeddings = new();

        public TagPredictor(ITopicsRepository topicsRepo)
        {
            _topicsRepo = topicsRepo;

            _embeddingService = new EmbeddingService(
                modelPath: "AI/Embeddings/e5-base-v2-onnx/model.onnx",
                vocabPath: "AI/Embeddings/e5-base-v2-onnx/vocab.txt"
            );
        }

        public async Task InitializeAsync()
        {
            var topics = await _topicsRepo.GetAllAsync();

            foreach (var t in topics)
            {
                float[] embedding;

                if (t.Embedding == null || t.Embedding.Length == 0)
                {
                    var expanded = ExpandTopicForEmbedding(t.Name);
                    embedding = _embeddingService.GetEmbedding("passage: " + expanded);

                    await _topicsRepo.UpdateEmbeddingAsync(t.Id, embedding);
                }
                else
                {
                    embedding = t.Embedding;
                }

                _topicEmbeddings[t.Id] = embedding;
            }
        }

        public void RemoveTopicAsync(string topicId)
        {
            _topicEmbeddings.Remove(topicId);
        }

        private static string ExpandTopicForEmbedding(string topicName)
        {
            var cleaned = topicName.Trim().Replace("_", " ").Replace("-", " ");

            return $"{cleaned}: concept, ideas, things, and activities related to {cleaned.ToLowerInvariant()}";
        }

        public async Task AddOrUpdateTopicAsync(string topicId, string topicName)
        {
            var expanded = ExpandTopicForEmbedding(topicName);
            var emb = _embeddingService.GetEmbedding("passage: " + expanded);
            Normalize(emb);
            await _topicsRepo.UpdateEmbeddingAsync(topicId, emb);
            _topicEmbeddings[topicId] = emb;
        }

        public async Task<string?> PredictTopic(Note note, double threshold = 0.2)
        {
            if (_topicEmbeddings.Count == 0)
                return null;

            var noteEmb = _embeddingService.GetEmbedding("query: " + note.Content);
            Normalize(noteEmb);

            double bestScore = threshold;
            string? bestTopic = null;

            System.Diagnostics.Debug.WriteLine($"Predictions:");
            foreach (var (topicId, topicEmb) in _topicEmbeddings)
            {
                double score = CosineSimilarity(noteEmb, topicEmb);
                var topicName = await _topicsRepo.GetNameByIdAsync(topicId);
                System.Diagnostics.Debug.WriteLine($"{topicName} → {score:F3}");
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTopic = topicId;
                }
            }

            return bestTopic;
        }

        public async Task<List<(string TopicId, double Score)>> PredictTopTopics(Note note, int topN = 3)
        {
            float LowerThreshold = 0.678f;
            if (_topicEmbeddings.Count == 0)
                return new();
            string textContent = ExtractPlainText(note.Content);
            var noteEmb = await _embeddingService.GetEmbeddingAsync("query: " + textContent);
            Normalize(noteEmb);

            var scores = new List<(string, double)>();
            foreach (var (topicId, topicEmb) in _topicEmbeddings)
            {
                double score = CosineSimilarity(noteEmb, topicEmb);
                if(score > LowerThreshold)
                    scores.Add((topicId, score));
            }
            //_ = PrintEmbeddings(scores); for debugging
            return scores.OrderByDescending(s => s.Item2)
                         .Take(topN)
                         .ToList();
        }

        // method for debugging, to see the sopic names and scores...
        private async Task PrintEmbeddings(List<(string name, double score)> scores)
        {
            Debug.WriteLine("\nTopics and Scores:");
            scores = scores.OrderByDescending(s => s.Item2).ToList();
            foreach (var (topicId, score) in scores)
            {
                var name = await _topicsRepo.GetNameByIdAsync(topicId);
                Debug.WriteLine($"{name} : {score}");
            }
        }

        private static string ExtractPlainText(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return string.Empty;
            string noTags = System.Text.RegularExpressions.Regex.Replace(
                xaml,
                "<[^>]+>",
                string.Empty);
            return System.Net.WebUtility.HtmlDecode(noTags).Trim();
        }


        private static void Normalize(float[] v)
        {
            double norm = Math.Sqrt(v.Sum(x => x * x));
            if (norm == 0) return;
            for (int i = 0; i < v.Length; i++)
                v[i] = (float)(v[i] / norm);
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null)
                return 0.0;
            if (a.Length == 0 || b.Length == 0)
                return 0.0;

            int len = Math.Min(a.Length, b.Length); // prevent out-of-range
            double dot = 0, magA = 0, magB = 0;

            for (int i = 0; i < len; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
            return denom > 0 ? dot / denom : 0.0;
        }

        public void Dispose() => _embeddingService.Dispose();
    }
}