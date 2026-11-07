using System.ComponentModel.DataAnnotations;

namespace TodoApp.Api.Contracts;

public sealed record RegisterRequest : IValidatableObject
{
    [Required, StringLength(80, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Name cannot be blank.")]
    public required string Name { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required]
    [RegularExpression(@"^(?=(?:.*\d){7,})[0-9+()\-\s]{7,20}$")]
    public required string Mobile { get; init; }

    [Required, StringLength(40)]
    public required string Gender { get; init; }

    [Required]
    public DateTime? Dob { get; init; }

    [Required, StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Dob is null || Dob.Value > DateTime.UtcNow)
        {
            yield return new ValidationResult("Date of birth must be in the past.", [nameof(Dob)]);
        }
    }
}
