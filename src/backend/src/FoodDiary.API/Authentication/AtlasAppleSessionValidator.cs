using System.Globalization;
using System.Text.Json;
using FoodDiary.Configuration;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Authentication;

public sealed class AtlasAppleSessionValidator(HttpClient client, IOptions<AtlasAppleAuthOptions> settings)
{
    public async Task<bool> IsCurrentAsync(long ownerId, long authEpoch, CancellationToken cancellationToken)
    {
        var options = settings.Value;
        if (!options.Enabled || ownerId <= 0 || authEpoch < 0)
        {
            return false;
        }

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            [AtlasAppleAuthentication.OwnerClaim] = ownerId.ToString(CultureInfo.InvariantCulture),
            [AtlasAppleAuthentication.AuthEpochClaim] = authEpoch.ToString(CultureInfo.InvariantCulture),
        });

        try
        {
            using var response = await client.PostAsync(AtlasAppleAuthentication.Endpoint(options, "session"), form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("active", out var active) &&
                active.ValueKind == JsonValueKind.True;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
