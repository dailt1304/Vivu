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
using Vivu.Application.UseCases.AI.ApplyTripModification;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Vivu.Infrastructure.Data;
using TripLocationEntity = Vivu.Domain.Entities.TripLocation;
using Xunit;

namespace Vivu.IntegrationTests.AI
{
    [Collection("Integration Tests")]
    public class ApplyTripModificationIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public ApplyTripModificationIntegrationTests(IntegrationTestWebAppFactory factory)
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

        #region add_location

        [Fact]
        public async Task ApplyModification_AddLocation_SavesNewLocationToDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var newLocation = locations[1]; // second available location not yet in trip
            var handler = CreateHandler(user.Id);
            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "add_location",
                DayIndex = 1,
                Location = CreateTripLocationChange(newLocation.Id, orderIndex: 2)
            });

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var tripDayId = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == trip.Id && d.DayIndex == 1)
                .Select(d => d.Id)
                .FirstAsync();

            var tripLocations = await _dbContext.Set<TripLocationEntity>()
                .Where(l => l.TripDayId == tripDayId)
                .ToListAsync();

            tripLocations.Should().Contain(l => l.LocationId == newLocation.Id);
        }

        #endregion

        #region remove_location

        [Fact]
        public async Task ApplyModification_RemoveLocation_RemovesLocationFromDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var locationToRemove = locations[0]; // the first location (already in trip day)
            var handler = CreateHandler(user.Id);
            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = locationToRemove.Id
            });

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var tripDayId = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == trip.Id && d.DayIndex == 1)
                .Select(d => d.Id)
                .FirstAsync();

            var remainingLocations = await _dbContext.Set<TripLocationEntity>()
                .Where(l => l.TripDayId == tripDayId)
                .ToListAsync();

            remainingLocations.Should().NotContain(l => l.LocationId == locationToRemove.Id);
        }

        #endregion

        #region update_time

        [Fact]
        public async Task ApplyModification_UpdateTime_UpdatesTimesInDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var handler = CreateHandler(user.Id);
            var newStartTime = new TimeOnly(10, 30);
            var newEndTime = new TimeOnly(13, 0);
            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "update_time",
                DayIndex = 1,
                LocationId = locations[0].Id,
                StartTime = newStartTime,
                EndTime = newEndTime
            });

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var tripDayId = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == trip.Id && d.DayIndex == 1)
                .Select(d => d.Id)
                .FirstAsync();

            var updatedLocation = await _dbContext.Set<TripLocationEntity>()
                .FirstAsync(l => l.TripDayId == tripDayId && l.LocationId == locations[0].Id);

            updatedLocation.StartTime.Should().Be(newStartTime.ToTimeSpan());
            updatedLocation.EndTime.Should().Be(newEndTime.ToTimeSpan());
        }

        #endregion

        #region update_location

        [Fact]
        public async Task ApplyModification_UpdateLocation_ReplacesOldWithNewInDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var oldLocationId = locations[0].Id;
            var newLocationId = locations[1].Id;
            var handler = CreateHandler(user.Id);
            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "update_location",
                DayIndex = 1,
                OldLocationId = oldLocationId,
                Location = CreateTripLocationChange(newLocationId, orderIndex: 1)
            });

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var tripDayId = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == trip.Id && d.DayIndex == 1)
                .Select(d => d.Id)
                .FirstAsync();

            var tripLocations = await _dbContext.Set<TripLocationEntity>()
                .Where(l => l.TripDayId == tripDayId)
                .ToListAsync();

            tripLocations.Should().NotContain(l => l.LocationId == oldLocationId);
            tripLocations.Should().Contain(l => l.LocationId == newLocationId);
        }

        #endregion

        #region add_day

        [Fact]
        public async Task ApplyModification_AddDay_SavesNewDayToDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var handler = CreateHandler(user.Id);
            var newDayDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "add_day",
                DayIndex = 2,
                Title = "Ngày 2: Ngày mới",
                Date = newDayDate,
                Locations = new List<TripLocationChange> { CreateTripLocationChange(locations[0].Id, orderIndex: 1) }
            });

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var newDay = await _dbContext.Set<TripDay>()
                .FirstOrDefaultAsync(d => d.TripId == trip.Id && d.Title == "Ngày 2: Ngày mới");

            newDay.Should().NotBeNull();
        }

        #endregion

        #region remove_day

        [Fact]
        public async Task ApplyModification_RemoveDay_RemovesDayFromDatabase()
        {
            var (user, trip, _) = await SeedTripWithLocationsAsync();
            var handler = CreateHandler(user.Id);
            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "remove_day",
                DayIndex = 1
            });

            var result = await handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var remainingDays = await _dbContext.Set<TripDay>()
                .Where(d => d.TripId == trip.Id && d.DayIndex > 0)
                .ToListAsync();

            remainingDays.Should().NotContain(d => d.DayIndex == 1 && d.Title == "Ngày 1");
        }

        #endregion

        #region Side Effects

        [Fact]
        public async Task ApplyModification_ValidChange_CreatesChatMessageInDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var handler = CreateHandler(user.Id);
            const string expectedSummary = "Đã xóa địa điểm Đại Nội khỏi ngày 1";
            var command = new ApplyTripModificationCommand
            {
                TripId = trip.Id,
                UserRequest = "Xóa địa điểm",
                Modification = new TripModificationResponse
                {
                    Summary = expectedSummary,
                    Changes = new List<TripChange>
                    {
                        new TripChange
                        {
                            Type = "remove_location",
                            DayIndex = 1,
                            LocationId = locations[0].Id
                        }
                    }
                }
            };

            await handler.Handle(command, CancellationToken.None);

            var chatMessage = await _dbContext.Set<ChatMessage>()
                .FirstOrDefaultAsync(m => m.TripId == trip.Id && m.IsAiMessage && m.MessageType == "trip_modification");

            chatMessage.Should().NotBeNull();
            chatMessage!.Content.Should().Be(expectedSummary);
        }

        [Fact]
        public async Task ApplyModification_AddDayWithLocations_UpdatesTripDatesInDatabase()
        {
            var (user, trip, locations) = await SeedTripWithLocationsAsync();
            var handler = CreateHandler(user.Id);
            var newDayDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20)); // far future date

            var command = CreateCommand(trip.Id, new TripChange
            {
                Type = "add_day",
                DayIndex = 2,
                Title = "Ngày 2",
                Date = newDayDate,
                Locations = new List<TripLocationChange> { CreateTripLocationChange(locations[0].Id, orderIndex: 1) }
            });

            await handler.Handle(command, CancellationToken.None);

            _dbContext.Entry(trip).State = EntityState.Detached;
            var updatedTrip = await _dbContext.Set<Trip>()
                .AsNoTracking()
                .FirstAsync(t => t.Id == trip.Id);

            updatedTrip.EndDate.Should().NotBeNull();
        }

        #endregion

        #region Helper Methods — Handler Factory

        private ApplyTripModificationCommandHandler CreateHandler(Guid userId)
        {
            var currentUserMock = new Mock<ICurrentUser>();
            currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            currentUserMock.Setup(x => x.TraceId).Returns("integration-test-trace");

            return new ApplyTripModificationCommandHandler(
                _scope.ServiceProvider.GetRequiredService<ITripDayRepository>(),
                _scope.ServiceProvider.GetRequiredService<IAIConvert>(),
                _scope.ServiceProvider.GetRequiredService<ITripLocationRepository>(),
                currentUserMock.Object,
                _scope.ServiceProvider.GetRequiredService<ILocationRepository>(),
                _scope.ServiceProvider.GetRequiredService<IChatMessageRepository>(),
                _scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
                _scope.ServiceProvider.GetRequiredService<ITripRepository>(),
                _scope.ServiceProvider.GetRequiredService<IMapper>(),
                _scope.ServiceProvider.GetRequiredService<ILogger<ApplyTripModificationCommandHandler>>(),
                _scope.ServiceProvider.GetRequiredService<ITripLocationAlternativeRepository>(),
                _scope.ServiceProvider.GetRequiredService<ITripItineraryOptimizer>()
            );
        }

        #endregion

        #region Helper Methods — Seed Data

        private async Task<(User user, Trip trip, List<Location> locations)> SeedTripWithLocationsAsync()
        {
            // 1. Seed User
            var user = User.Create(
                email: "modifytest@example.com",
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: "Modify Test User",
                avatarUrl: null
            );
            user.IsEmailVerified = true;
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, RoleName = "User", RoleDescription = "Standard" };
            user.UserRoles = new List<UserRole> { new UserRole { UserId = user.Id, RoleId = roleId, Role = role } };

            // 2. Seed City & Category & Locations
            var country = new Country
            {
                Id = Guid.NewGuid(),
                Name = "Việt Nam",
                Code = "VN"          // adjust nếu property name khác
            };

            var city = new City
            {
                Id = Guid.NewGuid(),
                Name = "Huế",
                CountryId = country.Id  // ✅ gán FK
            };
            var category = new LocationCategory { Id = Guid.NewGuid(), Name = "Di tích" };
            var locations = Enumerable.Range(1, 3).Select(i => new Location
            {
                Id = Guid.NewGuid(),
                Name = $"Địa điểm {i}",
                CityId = city.Id,
                CategoryId = category.Id,
                Address = $"Địa chỉ {i}",
                Description = $"Mô tả {i}",
                RatingAverage = 4.5M,
                RatingCount = 100
            }).ToList();

            // 3. Seed Trip
            var trip = Trip.Create(
                userId: user.Id,
                title: "Test Trip to Huế",
                description: "Test",
                coverUrl: null,
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(10),
                tripSize: 1,
                isPublic: false,
                cityId: city.Id,
                inviteCode: null
            );

            // 4. Seed TripDay with first location
            var tripDay = TripDay.Create(
                TripId: trip.Id,
                Tittle: "Ngày 1",
                DayDate: DateTime.UtcNow.AddDays(7),
                DayIndex: 1
            );
            var tripLocation = TripLocationEntity.Create(
                tripDayId: tripDay.Id,
                locationId: locations[0].Id,
                orderIndex: 1,
                startTime: TimeSpan.FromHours(9),
                endTime: TimeSpan.FromHours(11),
                note: "Test",
                transportMode: "walking"
            );

            // 5. Seed TripMember
            var tripMember = TripMember.Create(
                tripId: trip.Id,
                userId: user.Id,
                ownerId: user.Id,
                role: "owner"
            );

            // 6. Save all
            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            _dbContext.Set<Country>().Add(country);
            _dbContext.Set<City>().Add(city);
            _dbContext.Set<LocationCategory>().Add(category);
            _dbContext.Set<Location>().AddRange(locations);
            _dbContext.Set<Trip>().Add(trip);
            _dbContext.Set<TripDay>().Add(tripDay);
            _dbContext.Set<TripLocationEntity>().Add(tripLocation);
            _dbContext.Set<TripMember>().Add(tripMember);
            await _dbContext.SaveChangesAsync();

            // Detach to avoid tracking conflicts
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            return (user, trip, locations);
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

        private static ApplyTripModificationCommand CreateCommand(Guid tripId, TripChange change) =>
            new ApplyTripModificationCommand
            {
                TripId = tripId,
                UserRequest = "Test modification",
                Modification = new TripModificationResponse
                {
                    Summary = "Test change applied",
                    Changes = new List<TripChange> { change }
                }
            };

        private static TripLocationChange CreateTripLocationChange(Guid locationId, int orderIndex = 1) =>
            new TripLocationChange
            {
                LocationId = locationId,
                Name = "Test Location",
                Description = "Test",
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(16, 0),
                TransportMode = "walking",
                OrderIndex = orderIndex
            };

        #endregion
    }
}
