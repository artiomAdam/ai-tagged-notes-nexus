using Nexus.Core.Models;
using Nexus.Core.Storage;

namespace Nexus.Core.Interfaces
{
    public interface ITopicsRepository
    {
        Task<IEnumerable<Topic>> GetAllAsync();
        Task<Topic?> GetByIdAsync(string id);
        Task<Topic?> GetByNameAsync(string name);
        Task InsertAsync(Topic topic);
        Task UpdateAsync(Topic topic);
        Task DeleteAsync(string id);

        Task<string?> GetNameByIdAsync(string id);

        Task<string?> GetIdByNameAsync(string name);
        Task UpdateEmbeddingAsync(string id, float[] embedding);
    }
}
