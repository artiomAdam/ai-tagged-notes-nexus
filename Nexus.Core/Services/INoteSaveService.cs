using Nexus.Core.Models;
namespace Nexus.Core.Services
{
    public interface INoteSaveService
    {
        Task SaveAsync(Note note, string content);
    }
}
