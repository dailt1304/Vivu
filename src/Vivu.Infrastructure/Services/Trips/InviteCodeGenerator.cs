using System.Security.Cryptography;
using System.Threading;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Interfaces;

namespace Vivu.Infrastructure.Services.Trips
{
    public class InviteCodeGenerator : IInviteCodeGenerator
    {
        private readonly ITripRepository _tripRepository;
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 8;
        public InviteCodeGenerator(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public async Task<string> Generate(CancellationToken cancellationToken)
        {
            var random = new Random();

            string inviteCode;
            bool isUnique;

            do
            {
                inviteCode = new string(Enumerable.Repeat(Characters, CodeLength)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());

                isUnique = await _tripRepository.CheckAvailableInviteCode(inviteCode, cancellationToken);

            } while (!isUnique);

            return inviteCode;
        }
    }
}
