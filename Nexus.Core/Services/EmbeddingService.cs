using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;

namespace Nexus.Core.Services
{
    public class EmbeddingService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly E5Tokenizer _tokenizer;

        public EmbeddingService(string modelPath, string vocabPath)
        {
            _session = new InferenceSession(modelPath);
            _tokenizer = new E5Tokenizer(vocabPath);
            //Debug.WriteLine("=== Model Outputs ===");
            //foreach (var o in _session.OutputMetadata)
            //    Debug.WriteLine(o.Key);
        }
        public Task<float[]> GetEmbeddingAsync(string text)
        {
            return Task.Run(() => GetEmbedding(text));
        }
        public float[] GetEmbedding(string text)
        {
            if (!text.StartsWith("query: ") && !text.StartsWith("passage: "))
                text = "query: " + text;

            var tokens = _tokenizer.Tokenize(text);
            int len = tokens.Length;

            var input = new DenseTensor<long>(new[] { 1, len });
            var mask = new DenseTensor<long>(new[] { 1, len });

            for (int i = 0; i < len; i++)
            {
                input[0, i] = tokens[i];
                mask[0, i] = tokens[i] != 0 ? 1 : 0;
            }

            var typeIds = new DenseTensor<long>(new[] { 1, len });
            for (int i = 0; i < len; i++)
                typeIds[0, i] = 0;

            var inputs = new List<NamedOnnxValue>
{
    NamedOnnxValue.CreateFromTensor("input_ids", input),
    NamedOnnxValue.CreateFromTensor("attention_mask", mask),
    NamedOnnxValue.CreateFromTensor("token_type_ids", typeIds)
};

            using var results = _session.Run(inputs);
            var output = results.First(r => r.Name == "last_hidden_state")
                                .AsEnumerable<float>()
                                .ToArray();

            int hiddenSize = output.Length / len;
            var pooled = new float[hiddenSize];

            for (int j = 0; j < hiddenSize; j++)
            {
                float sum = 0;
                int count = 0;
                for (int i = 0; i < len; i++)
                {
                    if (mask[0, i] == 1)
                    {
                        sum += output[i * hiddenSize + j];
                        count++;
                    }
                }
                pooled[j] = sum / Math.Max(1, count);
            }

            float norm = (float)Math.Sqrt(pooled.Sum(v => v * v));
            for (int i = 0; i < pooled.Length; i++)
                pooled[i] /= norm;

            return pooled;
        }

        public void Dispose() => _session.Dispose();
    }
}
