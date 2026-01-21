using System.ComponentModel.DataAnnotations;

namespace BowlingGame.API.DTOs.Requests;

public class AddRollRequest
{
    [Required]
    [Range(0, 10)]
    public int Pins { get; set; }
}
