using System.ComponentModel.DataAnnotations;

namespace BowlingGame.API.DTOs.Requests;

public class CreateGameRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string PlayerName { get; set; } = string.Empty;
}
