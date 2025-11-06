namespace Nexus.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        bool a = false;
        Assert.That(a, Is.True);
    }

    [Test]
    public void Test2()
    {
        bool a = false;
        Assert.That(a, Is.False);
    }
}
