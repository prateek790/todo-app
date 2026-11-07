using System.ComponentModel.DataAnnotations;

namespace TodoApp.Api.Models;

public sealed class User : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(80, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Name cannot be blank.")]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public required string Email { get; set; }

    [Required]
    [RegularExpression(@"^(?=(?:.*\d){7,})[0-9+()\-\s]{7,20}$")]
    public required string Mobile { get; set; }

    [Required]
    [StringLength(40)]
    public required string Gender { get; set; }

    [Required]
    public DateTime? Dob { get; set; }

    public bool IsActive { get; set; } = true;

    public int[]? Range { get; set; } = [0, 10];

    [Required]
    [StringLength(20)]
    public string UserType { get; set; } = "User";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Dob is null || Dob.Value > DateTime.UtcNow)
        {
            yield return new ValidationResult("Date of birth must be in the past.", [nameof(Dob)]);
        }

        if (Range is { Length: not 2 }
            || Range is { Length: 2 } && Range.Any(value => value is < 0 or > 10)
            || Range is { Length: 2 } && Range[0] > Range[1])
        {
            yield return new ValidationResult(
                "Range must contain two values from 0 to 10 in ascending order.",
                [nameof(Range)]);
        }
    }
}
