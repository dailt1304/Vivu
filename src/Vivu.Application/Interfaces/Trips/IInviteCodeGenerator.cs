namespace Vivu.Application.Interfaces.Trips
{
    public interface IInviteCodeGenerator
    {
        Task<string> Generate(CancellationToken cancellationToken);
    }
}
