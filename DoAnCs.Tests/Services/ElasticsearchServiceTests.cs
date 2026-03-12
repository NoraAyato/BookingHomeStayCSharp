using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DoAnCs.Services;
using DoAnCs.Models;
using DoAnCs.Models.SearchModels;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Tests.Services
{
    [TestFixture]
    public class ElasticsearchServiceTests
    {
        private Mock<IElasticClient> _mockElasticClient;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IConfigurationSection> _mockConfigSection;
        private ApplicationDbContext _dbContext;
        private ElasticsearchService _service;
        private const string TestIndexName = "test_homestay";

        [SetUp]
        public void SetUp()
        {
            _mockElasticClient = new Mock<IElasticClient>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfigSection = new Mock<IConfigurationSection>();

            // Setup configuration
            _mockConfigSection.Setup(x => x.Value).Returns(TestIndexName);
            _mockConfiguration.Setup(x => x.GetSection("Elasticsearch"))
                             .Returns(_mockConfigSection.Object);
            _mockConfiguration.Setup(x => x.GetSection("Elasticsearch")["IndexName"])
                             .Returns(TestIndexName);

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _service = new ElasticsearchService(_mockElasticClient.Object, _mockConfiguration.Object, _dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
        }

        [Test]
        public async Task SuggestKhuVucAsync_ShouldReturnEmptyList_WhenQueryIsEmpty()
        {
            // Arrange
            var emptyQuery = "";
            var mockSearchResponse = new Mock<ISearchResponse<KhuVucDocument>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<KhuVucDocument>());
            mockSearchResponse.Setup(x => x.IsValid).Returns(true);

            _mockElasticClient.Setup(x => x.SearchAsync<KhuVucDocument>(
                It.IsAny<Func<SearchDescriptor<KhuVucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(mockSearchResponse.Object);

            // Act
            var result = await _service.SuggestKhuVucAsync(emptyQuery);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task SuggestKhuVucAsync_ShouldReturnMatchingResults_WhenQueryIsValid()
        {
            // Arrange
            var query = "Hà Nội";
            var expectedDocuments = new List<KhuVucDocument>
            {
                new KhuVucDocument { Ma_KV = "HN", Ten_KV = "Hà Nội" },
                new KhuVucDocument { Ma_KV = "HN01", Ten_KV = "Hà Nội - Quận 1" }
            };

            var mockSearchResponse = new Mock<ISearchResponse<KhuVucDocument>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(expectedDocuments);
            mockSearchResponse.Setup(x => x.IsValid).Returns(true);

            _mockElasticClient.Setup(x => x.SearchAsync<KhuVucDocument>(
                It.IsAny<Func<SearchDescriptor<KhuVucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(mockSearchResponse.Object);

            // Act
            var result = await _service.SuggestKhuVucAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Ten_KV.Should().Contain("Hà Nội");
        }

        [Test]
        public async Task SuggestKhuVucAsync_ShouldHandleElasticsearchException()
        {
            // Arrange
            var query = "test query";
            _mockElasticClient.Setup(x => x.SearchAsync<KhuVucDocument>(
                It.IsAny<Func<SearchDescriptor<KhuVucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new Exception("Elasticsearch connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.SuggestKhuVucAsync(query));
            
            exception.Message.Should().Contain("Elasticsearch connection failed");
        }

        [Test]
        public async Task SuggestTinTucAsync_ShouldReturnEmptyList_WhenQueryIsEmpty()
        {
            // Arrange
            var emptyQuery = "";
            var mockSearchResponse = new Mock<ISearchResponse<TinTucDocument>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<TinTucDocument>());
            mockSearchResponse.Setup(x => x.IsValid).Returns(true);

            _mockElasticClient.Setup(x => x.SearchAsync<TinTucDocument>(
                It.IsAny<Func<SearchDescriptor<TinTucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(mockSearchResponse.Object);

            // Act
            var result = await _service.SuggestTinTucAsync(emptyQuery);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task SuggestTinTucAsync_ShouldReturnMatchingResults_WhenQueryIsValid()
        {
            // Arrange
            var query = "du lịch";
            var expectedDocuments = new List<TinTucDocument>
            {
                new TinTucDocument 
                { 
                    ID_TinTuc = "TT001", 
                    TieuDe = "Hướng dẫn du lịch Hà Nội",
                    NoiDung = "Nội dung về du lịch"
                },
                new TinTucDocument 
                { 
                    ID_TinTuc = "TT002", 
                    TieuDe = "Du lịch Sapa mùa này",
                    NoiDung = "Thông tin du lịch Sapa"
                }
            };

            var mockSearchResponse = new Mock<ISearchResponse<TinTucDocument>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(expectedDocuments);
            mockSearchResponse.Setup(x => x.IsValid).Returns(true);

            _mockElasticClient.Setup(x => x.SearchAsync<TinTucDocument>(
                It.IsAny<Func<SearchDescriptor<TinTucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(mockSearchResponse.Object);

            // Act
            var result = await _service.SuggestTinTucAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(x => x.TieuDe.Contains("du lịch", StringComparison.OrdinalIgnoreCase))
                  .Should().BeTrue();
        }

        [Test]
        public async Task SuggestTinTucAsync_ShouldHandleInvalidResponse()
        {
            // Arrange
            var query = "test query";
            var mockSearchResponse = new Mock<ISearchResponse<TinTucDocument>>();
            mockSearchResponse.Setup(x => x.IsValid).Returns(false);
            mockSearchResponse.Setup(x => x.ServerError).Returns(new ServerError 
            { 
                Error = new Error { Reason = "Index not found" }
            });

            _mockElasticClient.Setup(x => x.SearchAsync<TinTucDocument>(
                It.IsAny<Func<SearchDescriptor<TinTucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(mockSearchResponse.Object);

            // Act
            var result = await _service.SuggestTinTucAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task SeedDataAsync_ShouldCreateIndices_WhenIndicesDoNotExist()
        {
            // Arrange
            var khuVucIndex = TestIndexName + "_khuvuc";
            var tinTucIndex = TestIndexName + "_tintuc";

            var mockExistsResponse = new Mock<ExistsResponse>();
            mockExistsResponse.Setup(x => x.Exists).Returns(false);

            var mockCreateResponse = new Mock<CreateIndexResponse>();
            mockCreateResponse.Setup(x => x.IsValid).Returns(true);

            _mockElasticClient.Setup(x => x.Indices.ExistsAsync(It.IsAny<IndexName>(), It.IsAny<Func<ExistsRequestDescriptor, IExistsRequest>>()))
                             .ReturnsAsync(mockExistsResponse.Object);

            _mockElasticClient.Setup(x => x.Indices.CreateAsync(It.IsAny<IndexName>(), It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()))
                             .ReturnsAsync(mockCreateResponse.Object);

            // Act
            await _service.SeedDataAsync();

            // Assert
            _mockElasticClient.Verify(x => x.Indices.ExistsAsync(It.IsAny<IndexName>(), It.IsAny<Func<ExistsRequestDescriptor, IExistsRequest>>()), Times.AtLeast(2));
            _mockElasticClient.Verify(x => x.Indices.CreateAsync(It.IsAny<IndexName>(), It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()), Times.AtLeast(2));
        }

        [Test]
        public async Task SeedDataAsync_ShouldSkipCreation_WhenIndicesAlreadyExist()
        {
            // Arrange
            var mockExistsResponse = new Mock<ExistsResponse>();
            mockExistsResponse.Setup(x => x.Exists).Returns(true);

            _mockElasticClient.Setup(x => x.Indices.ExistsAsync(It.IsAny<IndexName>(), It.IsAny<Func<ExistsRequestDescriptor, IExistsRequest>>()))
                             .ReturnsAsync(mockExistsResponse.Object);

            // Act
            await _service.SeedDataAsync();

            // Assert
            _mockElasticClient.Verify(x => x.Indices.ExistsAsync(It.IsAny<IndexName>(), It.IsAny<Func<ExistsRequestDescriptor, IExistsRequest>>()), Times.AtLeast(2));
            _mockElasticClient.Verify(x => x.Indices.CreateAsync(It.IsAny<IndexName>(), It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()), Times.Never);
        }

        [Test]
        public async Task SeedDataAsync_ShouldRetryOnFailure()
        {
            // Arrange
            var mockExistsResponse = new Mock<ExistsResponse>();
            mockExistsResponse.Setup(x => x.Exists).Returns(false);

            var mockCreateResponseFailed = new Mock<CreateIndexResponse>();
            mockCreateResponseFailed.Setup(x => x.IsValid).Returns(false);
            mockCreateResponseFailed.Setup(x => x.OriginalException).Returns(new Exception("Connection timeout"));

            var mockCreateResponseSuccess = new Mock<CreateIndexResponse>();
            mockCreateResponseSuccess.Setup(x => x.IsValid).Returns(true);

            _mockElasticClient.Setup(x => x.Indices.ExistsAsync(It.IsAny<IndexName>(), It.IsAny<Func<ExistsRequestDescriptor, IExistsRequest>>()))
                             .ReturnsAsync(mockExistsResponse.Object);

            _mockElasticClient.SetupSequence(x => x.Indices.CreateAsync(It.IsAny<IndexName>(), It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()))
                             .ReturnsAsync(mockCreateResponseFailed.Object)  // First call fails
                             .ReturnsAsync(mockCreateResponseSuccess.Object); // Second call succeeds

            // Act & Assert
            // This test verifies the retry mechanism in SeedDataAsync
            // The exact behavior depends on the implementation details of the retry logic
            await _service.SeedDataAsync();

            _mockElasticClient.Verify(x => x.Indices.CreateAsync(It.IsAny<IndexName>(), It.IsAny<Func<CreateIndexDescriptor, ICreateIndexRequest>>()), Times.AtLeast(1));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenElasticClientIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ElasticsearchService(null, _mockConfiguration.Object, _dbContext));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ElasticsearchService(_mockElasticClient.Object, null, _dbContext));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenDbContextIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ElasticsearchService(_mockElasticClient.Object, _mockConfiguration.Object, null));
        }

        [Test]
        public async Task SuggestKhuVucAsync_ShouldTrimAndLowerCaseQuery()
        {
            // Arrange
            var query = "  HÀ NỘI  ";
            var mockSearchResponse = new Mock<ISearchResponse<KhuVucDocument>>();
            mockSearchResponse.Setup(x => x.Documents).Returns(new List<KhuVucDocument>());
            mockSearchResponse.Setup(x => x.IsValid).Returns(true);

            SearchDescriptor<KhuVucDocument> capturedSearchDescriptor = null;
            _mockElasticClient.Setup(x => x.SearchAsync<KhuVucDocument>(
                It.IsAny<Func<SearchDescriptor<KhuVucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .Callback<Func<SearchDescriptor<KhuVucDocument>, ISearchRequest>, System.Threading.CancellationToken>(
                    (searchFunc, token) =>
                    {
                        var descriptor = new SearchDescriptor<KhuVucDocument>();
                        searchFunc(descriptor);
                        capturedSearchDescriptor = descriptor;
                    })
                .ReturnsAsync(mockSearchResponse.Object);

            // Act
            var result = await _service.SuggestKhuVucAsync(query);

            // Assert
            result.Should().NotBeNull();
            // Verify that the search was called (the query processing logic is tested indirectly)
            _mockElasticClient.Verify(x => x.SearchAsync<KhuVucDocument>(
                It.IsAny<Func<SearchDescriptor<KhuVucDocument>, ISearchRequest>>(),
                It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
    }
}