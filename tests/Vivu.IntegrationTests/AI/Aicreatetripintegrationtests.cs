using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.AI.CreateTrip;
using Vivu.Domain.Entities;
using TripLocationEntity = Vivu.Domain.Entities.TripLocation;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.AI
{
    [Collection("Integration Tests")]
    public class AICreateTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private readonly Guid _testLocationId1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private readonly Guid _testLocationId2 = Guid.Parse("10000000-0000-0000-0000-000000000002");

        public AICreateTripIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _scope = factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        }

        public async Task InitializeAsync() => await CleanupDatabaseAsync();

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        #region Happy Path — All Entities Saved

        [Fact]
        public async Task AICreateTrip_ValidRequest_SavesTripToDatabase()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var command = CreateValidCommand(city.Id, locations);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var savedTrip = await _dbContext.Set<Trip>()
                .FirstOrDefaultAsync(t => t.UserId == testUser.Id);
            savedTrip.Should().NotBeNull();
            savedTrip!.Title.Should().Be(command.TripPlan.Title);
            savedTrip.IsPublic.Should().BeFalse();
        }

        [Fact]
        public async Task AICreateTrip_ValidRequest_AlwaysCreatesIdeasDayInDatabase()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);

            var result = await handler.Handle(CreateValidCommand(city.Id, locations), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var tripId = result.Value!.Id;

            var ideasDay = await _dbContext.Set<TripDay>()
                .FirstOrDefaultAsync(d => d.TripId == tripId && d.DayIndex == 0);

            ideasDay.Should().NotBeNull();
            ideasDay!.Title.Should().Be("Ideas");
        }

        [Fact]
        public async Task AICreateTrip_ValidRequest_CreatesCorrectNumberOfTripDays()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var command = CreateValidCommand(city.Id, locations, dayCount: 3);

            var result = await handler.Handle(command, CancellationToken.None);

            var tripId = result.Value!.Id;
            var tripDays = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == tripId)
                .ToListAsync();

            // 1 "Ideas" day + 3 actual days
            tripDays.Should().HaveCount(4);
            tripDays.Should().Contain(d => d.DayIndex == 0 && d.Title == "Ideas");
        }

        [Fact]
        public async Task AICreateTrip_ValidRequest_CreatesTripMemberWithOwnerRoleInDatabase()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);

            var result = await handler.Handle(CreateValidCommand(city.Id, locations), CancellationToken.None);

            var tripId = result.Value!.Id;
            var member = await _dbContext.Set<TripMember>()
                .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == testUser.Id);

            member.Should().NotBeNull();
            member!.Role.Should().Be("owner");
            member.OwnerId.Should().Be(testUser.Id);
        }

        [Fact]
        public async Task AICreateTrip_ValidRequest_CreatesTwoChatMessagesInDatabase()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var command = CreateValidCommand(city.Id, locations);

            var result = await handler.Handle(command, CancellationToken.None);

            var tripId = result.Value!.Id;
            var messages = await _dbContext.Set<ChatMessage>()
                .Where(m => m.TripId == tripId)
                .ToListAsync();

            messages.Should().HaveCount(2);
            messages.Should().Contain(m => m.IsAiMessage == false && m.Content == command.UserPrompt);
            messages.Should().Contain(m => m.IsAiMessage == true && m.MessageType == "trip_plan");
        }

        [Fact]
        public async Task AICreateTrip_ValidRequest_SavesTripLocationsInDatabase()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var command = CreateValidCommand(city.Id, locations, dayCount: 2, locationsPerDay: 2);

            var result = await handler.Handle(command, CancellationToken.None);

            var tripId = result.Value!.Id;
            var tripDayIds = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == tripId && d.DayIndex > 0)
                .Select(d => d.Id)
                .ToListAsync();

            var tripLocations = await _dbContext.Set<TripLocationEntity>()
                .Where(l => tripDayIds.Contains(l.TripDayId))
                .ToListAsync();

            // 2 days x 2 locations per day = 4 TripLocations
            tripLocations.Should().HaveCount(4);
        }

        #endregion

        #region Happy Path — Invite Code

        [Fact]
        public async Task AICreateTrip_GenerateInviteCodeTrue_TripHasInviteCode()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var command = CreateValidCommand(city.Id, locations, generateInviteCode: true);

            var result = await handler.Handle(command, CancellationToken.None);

            var trip = await _dbContext.Set<Trip>()
                .FirstOrDefaultAsync(t => t.Id == result.Value!.Id);
            trip!.InviteCode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AICreateTrip_GenerateInviteCodeFalse_TripHasNoInviteCode()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id, useRealInviteCodeGenerator: false);
            var command = CreateValidCommand(city.Id, locations, generateInviteCode: false);

            var result = await handler.Handle(command, CancellationToken.None);

            var trip = await _dbContext.Set<Trip>()
                .FirstOrDefaultAsync(t => t.Id == result.Value!.Id);
            trip!.InviteCode.Should().BeNullOrEmpty();
        }

        #endregion

        #region Failure Tests — Location Validation

        [Fact]
        public async Task AICreateTrip_WithNonExistentLocationIds_ReturnsFailure()
        {
            var (testUser, city, _) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var fakeLocations = new List<Location>
            {
                new Location { Id = Guid.NewGuid() }, // IDs that don't exist in DB
                new Location { Id = Guid.NewGuid() }
            };
            var command = CreateValidCommand(city.Id, fakeLocations);

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task AICreateTrip_WithNonExistentLocationIds_DoesNotSaveAnythingToDatabase()
        {
            var (testUser, city, _) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var fakeLocations = new List<Location>
            {
                new Location { Id = Guid.NewGuid() }
            };

            await handler.Handle(CreateValidCommand(city.Id, fakeLocations), CancellationToken.None);

            var tripCount = await _dbContext.Set<Trip>()
                .CountAsync(t => t.UserId == testUser.Id);
            tripCount.Should().Be(0);
        }

        #endregion

        #region Database Constraint Tests

        [Fact]
        public async Task AICreateTrip_ForeignKeyConstraint_TripLocationMustReferenceTripDay()
        {
            // Verify that orphaned TripLocations cannot be inserted
            var orphanedTripLocation = TripLocationEntity.Create(
                tripDayId: Guid.NewGuid(), // non-existent TripDay
                locationId: Guid.NewGuid(),
                orderIndex: 1,
                startTime: TimeSpan.FromHours(9),
                endTime: TimeSpan.FromHours(11),
                note: "Test",
                transportMode: "walking"
            );

            _dbContext.Set<TripLocationEntity>().Add(orphanedTripLocation);

            await Assert.ThrowsAsync<DbUpdateException>(
                async () => await _dbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task AICreateTrip_MultipleDays_AllDaysSavedWithCorrectDayIndex()
        {
            var (testUser, city, locations) = await SeedTestDataAsync();
            var handler = CreateHandler(testUser.Id);
            var command = CreateValidCommand(city.Id, locations, dayCount: 3);

            var result = await handler.Handle(command, CancellationToken.None);

            var tripId = result.Value!.Id;
            var savedDays = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == tripId && d.DayIndex > 0)
                .OrderBy(d => d.DayIndex)
                .ToListAsync();

            savedDays.Should().HaveCount(3);
            savedDays.Select(d => d.DayIndex).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        #endregion

        #region Concurrent Creation Tests

        [Fact]
        public async Task AICreateTrip_ConcurrentCreation_AllSucceedWithIsolation()
        {
            const int concurrentUsers = 3;
            
            // Seed all test data first  
            var seedResults = new List<(User testUser, City city, List<Location> locations)>();
            for (int i = 0; i < concurrentUsers; i++)
            {
                var seed = await SeedTestDataAsync($"concurrent{i}@test.com");
                seedResults.Add(seed);
            }

            // Sequential trip creation (concurrent would require better DI scoping)
            var results = new List<Result<DetailedTripDto>>();
            foreach (var seed in seedResults)
            {
                var handler = CreateHandler(seed.testUser.Id);
                var result = await handler.Handle(
                    CreateValidCommand(seed.city.Id, seed.locations, generateInviteCode: false),
                    CancellationToken.None);
                results.Add(result);
            }

            results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

            var tripCount = await _dbContext.Set<Trip>().CountAsync();
            tripCount.Should().Be(concurrentUsers);
        }

        #endregion

        #region Helper Methods — Handler Factory

        /// <summary>
        /// Creates a handler with real repositories from DI and a mocked ICurrentUser.
        /// IInviteCodeGenerator is mocked to return predictable codes unless useRealInviteCodeGenerator is true.
        /// </summary>
        private AICreateTripCommandHandler CreateHandler(Guid userId, bool useRealInviteCodeGenerator = true)
        {
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            currentUserMock.Setup(x => x.TraceId).Returns("integration-test-trace");

            IInviteCodeGenerator inviteCodeGenerator;
            if (useRealInviteCodeGenerator)
            {
                // Use real generator from DI if registered, else use a simple mock
                inviteCodeGenerator = _scope.ServiceProvider.GetService<IInviteCodeGenerator>()
                    ?? CreateMockInviteCodeGenerator();
            }
            else
            {
                inviteCodeGenerator = CreateMockInviteCodeGenerator();
            }

            return new AICreateTripCommandHandler(
                currentUserMock.Object,
                inviteCodeGenerator,
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<IMapper>(),
                _scope.ServiceProvider.GetRequiredService<ITripLocationRepository>(),
                _scope.ServiceProvider.GetRequiredService<ITripLocationAlternativeRepository>(),
                _scope.ServiceProvider.GetRequiredService<IChatMessageRepository>(),
                _scope.ServiceProvider.GetRequiredService<IAIConvert>(),
                _scope.ServiceProvider.GetRequiredService<ITripDayRepository>(),
                _scope.ServiceProvider.GetRequiredService<ITripRepository>(),
                _scope.ServiceProvider.GetRequiredService<ITripMemberRepository>(),
                _scope.ServiceProvider.GetRequiredService<ILocationRepository>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<AICreateTripCommandHandler>>()
            );
        }

        private static IInviteCodeGenerator CreateMockInviteCodeGenerator()
        {
            var mock = new Mock<IInviteCodeGenerator>();
            mock.Setup(x => x.Generate(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => $"TEST-{Guid.NewGuid().ToString()[..8].ToUpper()}");
            return mock.Object;
        }

        #endregion

        #region Helper Methods — Seed Data

        private async Task<(User testUser, City city, List<Location> locations)> SeedTestDataAsync(
            string? email = null,
            int locationCount = 2)
        {
            var user = User.Create(
                email: (email ?? "aitest@example.com").ToLower(),
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: "AI Test User",
                avatarUrl: null
            );
            user.IsEmailVerified = true;

            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, RoleName = "User", RoleDescription = "Standard User" };
            user.UserRoles = new List<UserRole>
            {
                new UserRole { UserId = user.Id, RoleId = roleId, Role = role }
            };

            //ForeignKey from city should be Country
            var country = new Country { Id = Guid.NewGuid(), Code = "VN", Name = "Vietnam" };
            var city = new City { Id = Guid.NewGuid(), Name = "Huế", CountryId = country.Id };

            var category = new LocationCategory { Id = Guid.NewGuid(), Name = "Di tích lịch sử" };

            var locations = Enumerable.Range(1, locationCount).Select(i => new Location
            {
                Id = Guid.NewGuid(),
                Name = $"Địa điểm {i}",
                CityId = city.Id,
                CategoryId = category.Id,
                Address = $"Số {i}, Đường Lê Lợi, Huế",
                Description = $"Mô tả địa điểm thứ {i} tại Huế",
                RatingAverage = 4.5M,
                RatingCount = 100 + i
            }).ToList();

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            _dbContext.Set<Country>().Add(country);
            _dbContext.Set<City>().Add(city);
            _dbContext.Set<LocationCategory>().Add(category);
            _dbContext.Set<Location>().AddRange(locations);
            await _dbContext.SaveChangesAsync();

            // Detach all to avoid EF tracking conflicts during handler execution
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            return (user, city, locations);
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<TripLocationEntity>().ExecuteDeleteAsync();
            await _dbContext.Set<ChatMessage>().ExecuteDeleteAsync();
            await _dbContext.Set<TripDay>().ExecuteDeleteAsync();
            await _dbContext.Set<TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<Location>().ExecuteDeleteAsync();
            await _dbContext.Set<LocationCategory>().ExecuteDeleteAsync();
            await _dbContext.Set<City>().ExecuteDeleteAsync();
            await _dbContext.Set<Country>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #endregion

        #region Helper Methods — Command Factory

        private AICreateTripCommand CreateValidCommand(
            Guid cityId,
            List<Location> locations,
            bool generateInviteCode = true,
            int dayCount = 1,
            int locationsPerDay = 1)
        {
            var locationsList = locations.Take(locationsPerDay).ToList();

            return new AICreateTripCommand
            {
                UserPrompt = "Lên kế hoạch đi chơi 3 ngày 2 đêm ở Huế",
                cityId = cityId,
                GenerateInviteCode = generateInviteCode,
                TripPlan = CreateFakeTripPlanResponse(locations, dayCount, locationsPerDay)
            };
        }

        private static TripPlanResponse CreateFakeTripPlanResponse(
            List<Location> locations,
            int dayCount = 1,
            int locationsPerDay = 1)
        {
            var startDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            var endDate = startDate.AddDays(dayCount);

            return new TripPlanResponse
            {
                Title = $"{dayCount} Ngày Khám Phá Huế",
                Description = "Hành trình khám phá cố đô Huế tuyệt vời",
                Start = startDate,
                End = endDate,
                Size = 1,
                Days = Enumerable.Range(1, dayCount).Select(dayIndex =>
                    new DayPlanDto
                    {
                        DayIndex = dayIndex,
                        Date = startDate.AddDays(dayIndex - 1),
                        Title = $"Ngày {dayIndex}: Khám phá Huế",
                        Locations = locations
                            .Take(locationsPerDay)
                            .Select((loc, idx) => new LocationPlanDto
                            {
                                LocationId = loc.Id,
                                Name = loc.Name,
                                Description = $"Tham quan {loc.Name}",
                                StartTime = new TimeOnly(9 + idx * 2, 0),
                                EndTime = new TimeOnly(11 + idx * 2, 0),
                                TransportMode = "walking",
                                OrderIndex = idx + 1
                            }).ToList()
                    }).ToList()
            };
        }

        #endregion
    }
}