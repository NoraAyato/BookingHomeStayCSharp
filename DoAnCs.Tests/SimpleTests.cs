using NUnit.Framework;

namespace Demo.Tests
{
    [TestFixture]
    public class SimpleTests
    {
        private int _setupValue;

        [SetUp]
        public void SetUp()
        {
            _setupValue = 100;
        }

        [Test]
        public void Test1()
        {
            // Arrange
            int attrb1 = 1;
            int attrb2 = 2;

            // Act & Assert
            Assert.That(attrb1, Is.EqualTo(attrb2 - 1));
            Assert.That(_setupValue, Is.EqualTo(100));
        }

        [Test]
        public void Test_Addition()
        {
            // Arrange
            int a = 5;
            int b = 3;
            int expected = 8;

            // Act
            int result = a + b;

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Test_String_Contains()
        {
            // Arrange
            string text = "Hello World";

            // Act & Assert
            Assert.That(text, Does.Contain("World"));
        }

        [TestCase(1, 2, 3)]
        [TestCase(5, 5, 10)]
        [TestCase(0, 0, 0)]
        public void Test_Addition_WithParameters(int a, int b, int expected)
        {
            // Act
            int result = a + b;

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup after each test
            _setupValue = 0;
        }
    }
}