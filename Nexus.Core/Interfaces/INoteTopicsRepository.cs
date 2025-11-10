namespace Nexus.Core.Interfaces
{
    public interface INoteTopicsRepository
    {
        Task AddTopicToNoteAsync(string noteId, string topicId);
        Task RemoveTopicFromNoteAsync(string noteId, string topicId);
        Task<IEnumerable<string>> GetTopicsForNoteAsync(string noteId);
        Task<IEnumerable<string>> GetNotesForTopicAsync(string topicId);
        Task DeleteByNoteIdAsync(string noteId);
        Task DeleteByTopicIdAsync(string topicId);

    }
}
