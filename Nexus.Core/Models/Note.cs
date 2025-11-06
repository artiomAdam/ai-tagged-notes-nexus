using Nexus.Core.Utilities;

namespace Nexus.Core.Models
{
    public class Note : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Note Clone() => (Note)this.MemberwiseClone();
    }
}
