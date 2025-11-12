using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;

namespace Nexus.Core.Services
{
    public class EmbeddingService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly MiniLMTokenizer _tokenizer;

        public EmbeddingService(string modelPath, string vocabPath)
        {
            _session = new InferenceSession(modelPath);
            _tokenizer = new MiniLMTokenizer(vocabPath);
            //Debug.WriteLine("=== Model Outputs ===");
            //foreach (var o in _session.OutputMetadata)
            //    Debug.WriteLine(o.Key);
        }

        public float[] GetEmbedding(string text)
        {
            // e5-base-v2-onnx
            var tokens = _tokenizer.Tokenize(text);
            int len = tokens.Length;

            var input = new DenseTensor<long>(new[] { 1, len });
            var mask = new DenseTensor<long>(new[] { 1, len });
            var type = new DenseTensor<long>(new[] { 1, len });

            for (int i = 0; i < len; i++)
            {
                input[0, i] = tokens[i];
                mask[0, i] = 1;
                type[0, i] = 0;
            }

            var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("input_ids", input),
        NamedOnnxValue.CreateFromTensor("attention_mask", mask),
        NamedOnnxValue.CreateFromTensor("token_type_ids", type)
    };

            using var results = _session.Run(inputs);

            // get the only output
            var output = results.First(r => r.Name == "last_hidden_state")
                                .AsEnumerable<float>()
                                .ToArray();

            // The shape is [1, seq_len, hidden_size]
            int hiddenSize = output.Length / len;
            var pooled = new float[hiddenSize];

            // mean pooling across tokens
            for (int i = 0; i < len; i++)
                for (int j = 0; j < hiddenSize; j++)
                    pooled[j] += output[i * hiddenSize + j];

            for (int j = 0; j < hiddenSize; j++)
                pooled[j] /= len;

            return pooled;
        }

        public void Dispose() => _session.Dispose();
    }
}
