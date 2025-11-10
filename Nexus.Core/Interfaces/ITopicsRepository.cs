using Nexus.Core.Models;

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
    }
}
