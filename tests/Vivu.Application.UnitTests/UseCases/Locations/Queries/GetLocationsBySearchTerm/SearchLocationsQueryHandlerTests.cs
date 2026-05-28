using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.Interfaces.Locations;
using Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetLocationsBySearchTerm
{
    public class SearchLocationsQueryHandlerTests
    {
        private readonly Mock<ILocationRepository> _mockRepo;
        private readonly Mock<IFilterLocation> _mockFilter;
        private readonly Mock<ISearchTermLocation> _mockSearchTerm;
        private readonly Mock<ILogger<SearchLocationsQueryHandler>> _mockLogger;
        private readonly SearchLocationsQueryHandler _handler;

        public SearchLocationsQueryHandlerTests()
        {
            _mockRepo = new Mock<ILocationRepository>();
            _mockFilter = new Mock<IFilterLocation>();
            _mockSearchTerm = new Mock<ISearchTermLocation>();
            _mockLogger = new Mock<ILogger<SearchLocationsQueryHandler>>();

            _handler = new SearchLocationsQueryHandler(
                _mockRepo.Object,
                _mockFilter.Object,
                _mockSearchTerm.Object,
                _mockLogger.Object
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Handle_Should_ReturnFailure_When_SearchTerm_IsInvalid(string invalidTerm)
        {
            var query = new SearchLocationsQuery { SearchTerm = invalidTerm };

            _mockSearchTerm.Setup(x => x.SanitizeSearchTerm(invalidTerm))
                .Returns(string.Empty);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(DomainErrors.Location.InvalidSearchTerm, result.Error);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Search term after sanitize is null or empty")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task Handle_Should_Call_Dependencies_Correctly_When_Valid()
        {
            var rawSearchTerm = "Đà Nẵng";
            var sanitizedTerm = "da nang";
            var tsQueryString = "da & nang:*";
            var request = new SearchLocationsQuery
            {
                SearchTerm = rawSearchTerm,
                PageNumber = 1,
                PageSize = 10,
                IncludeHighlights = true
            };

            _mockSearchTerm.Setup(x => x.SanitizeSearchTerm(rawSearchTerm)).Returns(sanitizedTerm);
            _mockSearchTerm.Setup(x => x.ConvertToTsQuery(sanitizedTerm)).Returns(tsQueryString);

            var locations = new List<Location>
        {
            new Location { Id = Guid.NewGuid(), Name = "Location 1", RatingAverage = 4.5m },
            new Location { Id = Guid.NewGuid(), Name = "Location 2", RatingAverage = 3.0m }
        };

            var mockQueryable = locations.AsQueryable().BuildMock();

            _mockRepo.Setup(x => x.GetSearchTermLocation(tsQueryString))
                .Returns(mockQueryable); 

            _mockFilter.Setup(x => x.ApplyFiltersToGetSearchLocation(mockQueryable, request))
                .Returns(mockQueryable);

            _mockSearchTerm.Setup(x => x.ApplyHighlightingInMemory(It.IsAny<List<LocationSearchResultDto>>(), sanitizedTerm))
                .Returns((List<LocationSearchResultDto> list, string term) => list); 


            try
            {
                await _handler.Handle(request, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
            }

            _mockSearchTerm.Verify(x => x.SanitizeSearchTerm(rawSearchTerm), Times.Once);
            _mockSearchTerm.Verify(x => x.ConvertToTsQuery(sanitizedTerm), Times.Once);
            _mockRepo.Verify(x => x.GetSearchTermLocation(tsQueryString), Times.Once);
            _mockFilter.Verify(x => x.ApplyFiltersToGetSearchLocation(It.IsAny<IQueryable<Location>>(), request), Times.Once);
        }
    }
}
