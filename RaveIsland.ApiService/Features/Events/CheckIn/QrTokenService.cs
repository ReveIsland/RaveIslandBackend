using System.Security.Cryptography;
using System.Text;

namespace RaveIsland.ApiService.Features.Events.CheckIn;

public interface IQrTokenService
{
    string GenerateToken(Guid ticketId, Guid eventId);
    (Guid TicketId, Guid EventId)? ParseToken(string token);
}

public sealed class QrTokenService : IQrTokenService
{
    public string GenerateToken(Guid ticketId, Guid eventId)
    {
        var payload = $"{ticketId:N}:{eventId:N}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(bytes);
    }

    public (Guid TicketId, Guid EventId)? ParseToken(string token)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = payload.Split(':');
            if (parts.Length != 2)
            {
                return null;
            }

            return (Guid.Parse(parts[0]), Guid.Parse(parts[1]));
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
