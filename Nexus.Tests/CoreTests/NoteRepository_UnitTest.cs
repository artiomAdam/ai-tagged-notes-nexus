using Nexus.Core.Models;
using Nexus.Core.Storage;

namespace Nexus.Tests.CoreTests
{
    public class NoteRepository_UnitTest
    {

        [Test]
        public async Task InsertAndRetrieveNote_WorksCorrectly()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

            using var context = new DbContext(tempDb);
            context.Initialize();

            var repo = new NoteRepository(context);
            var note = new Note { Title = "Test", Content = "Hello" };
            await repo.InsertAsync(note);

            var loaded = await repo.GetByIdAsync(note.Id);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Content, Is.EqualTo("Hello"));
        }

        [Test]
        public async Task InsertAndUpdateNote_WorksCorrectly()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

            using var context = new DbContext(tempDb);
            context.Initialize();
            var repo = new NoteRepository(context);
            var note1 = new Note { Title = "Test", Content = "Hello" };
            await repo.InsertAsync(note1);
            note1.Content = "Bye";
            await repo.UpdateAsync(note1);

            var loaded = await repo.GetByIdAsync(note1.Id);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Content, Is.EqualTo("Bye"));
        }

        [Test]
        public async Task InsertAndDeleteNote_WorksCorrectly()
        {
            var tempDb = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

            using var context = new DbContext(tempDb);
            context.Initialize();
            var repo = new NoteRepository(context);
            var note = new Note { Title = "Test", Content = "Hello" };
            await repo.InsertAsync(note);
            var loaded = await repo.GetByIdAsync(note.Id);
            Assert.That(loaded, Is.Not.Null);
            await repo.DeleteAsync(note.Id);
            loaded = await repo.GetByIdAsync(note.Id);
            Assert.That(loaded, Is.Null);
        }
    }
}
