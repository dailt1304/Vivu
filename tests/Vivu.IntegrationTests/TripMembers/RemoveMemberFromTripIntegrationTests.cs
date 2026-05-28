using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.UseCases.TripMember.Command.RemoveMemberFromTrip;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.Interfaces.Auth;

namespace Vivu.IntegrationTests.TripMembers
{
    [Collection("Integration Tests")]
    public class RemoveMemberFromTripIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        public RemoveMemberFromTripIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<Domain.Entities.TripMember>().ExecuteDeleteAsync();
            await _dbContext.Set<Trip>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
        }

        #region Helper Methods

        private async Task<User> CreateTestUserAsync(string email, string fullName)
        {
            var user = User.Create(
                email: email.ToLower(),
                passwordHash: _passwordHasher.HashPassword("hashed_password"),
                fullName: fullName
            );
            user.IsEmailVerified = true;
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            var roleId = Guid.NewGuid();
            var role = new Role
            {
                Id = roleId,
                RoleName = "User",
                RoleDescription = "Standard User"
            };

            user.UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    Role = role
                }
            };

            _dbContext.Set<Role>().Add(role);
            await _dbContext.Set<User>().AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<Trip> CreateTestTripAsync(Guid ownerId, string title, string status = "planning")
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: title
            );

            // Update status if different from default
            if (status != "planning")
            {
                trip.Status = status;
            }

            await _dbContext.Set<Trip>().AddAsync(trip);
            await _dbContext.SaveChangesAsync();
            return trip;
        }

        private async Task<Domain.Entities.TripMember> AddMemberToTripAsync(Guid tripId, Guid userId, Guid ownerId, string role = "member")
        {
            var tripMember = Domain.Entities.TripMember.Create(
                tripId: tripId,
                userId: userId,
                ownerId: ownerId,
                role: role
            );

            await _dbContext.Set<Domain.Entities.TripMember>().AddAsync(tripMember);
            await _dbContext.SaveChangesAsync();
            return tripMember;
        }

        private async Task<string> GetAuthTokenAsync(Guid userId)
        {
            return $"Bearer {userId}";
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task RemoveMember_WithValidRequest_ShouldReturnOkAndRemoveMember()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");
            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeTrue();
            apiResponse.Data.Should().Contain("Test Member");
            apiResponse.Data.Should().Contain("Test Trip");

            var memberExists = await _dbContext.Set<Domain.Entities.TripMember>()
                .AnyAsync(tm => tm.TripId == trip.Id && tm.UserId == member.Id);
            memberExists.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveMember_WithMultipleMembers_ShouldOnlyRemoveSpecifiedMember()
        {
            // Arrange
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member1 = await CreateTestUserAsync("member1@test.com", "Member One");
            var member2 = await CreateTestUserAsync("member2@test.com", "Member Two");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");

            await AddMemberToTripAsync(trip.Id, member1.Id, owner.Id);
            await AddMemberToTripAsync(trip.Id, member2.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member1.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var member1Exists = await _dbContext.Set<Domain.Entities.TripMember>()
                .AnyAsync(tm => tm.TripId == trip.Id && tm.UserId == member1.Id);
            member1Exists.Should().BeFalse();

            var member2Exists = await _dbContext.Set<Domain.Entities.TripMember>()
                .AnyAsync(tm => tm.TripId == trip.Id && tm.UserId == member2.Id);
            member2Exists.Should().BeTrue();
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task RemoveMember_WhenUserIsNotOwner_ShouldReturnForbidden()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var nonOwner = await CreateTestUserAsync("nonowner@test.com", "Non Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");

            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            var token = await GetAccessTokenAsync(nonOwner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);

            var memberExists = await _dbContext.Set<Domain.Entities.TripMember>()
                .AnyAsync(tm => tm.TripId == trip.Id && tm.UserId == member.Id);
            memberExists.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveMember_WhenOwnerTriesToRemoveThemselves_ShouldReturnBadRequest()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");

            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = owner.Id 
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse.Should().NotBeNull();
            apiResponse.Message.Should().Contain("Cannot remove yourself from the trip. Leave the trip instead.");
        }

        [Fact]
        public async Task RemoveMember_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");
            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            // No authentication header set

            // Act
            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validation Tests


        [Fact]
        public async Task RemoveMember_WithEmptyUserId_ShouldReturnBadRequest()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.Empty
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task RemoveMember_WhenTripNotFound_ShouldReturnNotFound()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");

            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(), 
                UserId = member.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RemoveMember_WhenMemberNotFound_ShouldReturnNotFound()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = Guid.NewGuid() 
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RemoveMember_WhenMemberNotInTrip_ShouldReturnNotFound()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var nonMember = await CreateTestUserAsync("nonmember@test.com", "Non Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = nonMember.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            apiResponse.Should().NotBeNull();
            apiResponse!.Success.Should().BeFalse();
            apiResponse.Message.Should().Contain("Trip member was not found");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task RemoveMember_FromDeletedTrip_ShouldHandleGracefully()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip");
            trip.IsDeleted = true;
            await _dbContext.SaveChangesAsync();

            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);


            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound
            );
        }

        [Fact]
        public async Task RemoveMember_FromCompletedTrip_ShouldHandleGracefully()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");
            var trip = await CreateTestTripAsync(owner.Id, "Test Trip", "completed");
            await AddMemberToTripAsync(trip.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);


            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest
            );
        }



        #endregion

        #region Database State Verification Tests

        [Fact]
        public async Task RemoveMember_ShouldNotAffectOtherTrips()
        {
            var owner = await CreateTestUserAsync("owner@test.com", "Trip Owner");
            var member = await CreateTestUserAsync("member@test.com", "Test Member");

            var trip1 = await CreateTestTripAsync(owner.Id, "Trip 1");
            var trip2 = await CreateTestTripAsync(owner.Id, "Trip 2");

            await AddMemberToTripAsync(trip1.Id, member.Id, owner.Id);
            await AddMemberToTripAsync(trip2.Id, member.Id, owner.Id);

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip1.Id,
                UserId = member.Id
            };

            var token = await GetAccessTokenAsync(owner.Email);
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/TripMembers/remove-member", command);

            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var trip1MemberExists = await _dbContext.Set<Domain.Entities.TripMember>()
                .AnyAsync(tm => tm.TripId == trip1.Id && tm.UserId == member.Id);
            trip1MemberExists.Should().BeFalse();

            var trip2MemberExists = await _dbContext.Set<Domain.Entities.TripMember>()
                .AnyAsync(tm => tm.TripId == trip2.Id && tm.UserId == member.Id);
            trip2MemberExists.Should().BeTrue();
        }
        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; }
            public string Message { get; set; }
        }
        private async Task<string> GetAccessTokenAsync(string email)
        {
            var loginRequest = new LoginUserCommand
            {
                Email = email,
                Password = "hashed_password"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
            {
                throw new Exception($"Login failed. Status: {response.StatusCode}, Body: {content}");
            }

            return apiResponse.Data.AccessToken;
        }

        #endregion
    }
}
