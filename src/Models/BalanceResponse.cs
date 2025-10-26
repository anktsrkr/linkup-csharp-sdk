using System.Text.Json.Serialization;

namespace LinkupSdk.Models;

/// <summary>
/// Response model for balance API calls
/// </summary>
public class BalanceResponse
{
    /// <summary>
    /// Balance available in the account
    /// </summary>
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}