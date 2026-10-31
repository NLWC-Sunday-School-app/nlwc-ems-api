using System.ComponentModel.DataAnnotations;

namespace nlwc.ems.Dtos;

public class SignUpRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10, ErrorMessage = "Password must be at least 10 characters."), MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string GivenName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FamilyName { get; set; } = string.Empty;

    public Gender? Gender { get; set; }
}

public class LogInRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    public Surface Surface { get; set; } = Surface.Web;
}

public class VerifyEmailRequest
{
    [Required, MinLength(10)]
    public string Token { get; set; } = string.Empty;
}

public class MfaCodeRequest
{
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits.")]
    public string Code { get; set; } = string.Empty;
}

public class LoginResultDto
{
    /// <summary>Echoed in <c>X-CSRF-Token</c> on every mutation (it is also set as a readable cookie).</summary>
    public string CsrfToken { get; set; } = string.Empty;

    public bool MfaVerified { get; set; }
    public bool EmailVerified { get; set; }
}

public class MeDto
{
    public Guid PersonId { get; set; }
    public string Surface { get; set; } = string.Empty;
    public bool MfaVerified { get; set; }
    public bool EmailVerified { get; set; }
}

public class MfaEnrollmentDto
{
    public string OtpauthUri { get; set; } = string.Empty;
}

/// <summary>Body of a 201 for a newly created record the caller may need to refer to.</summary>
public class CreatedDto
{
    public Guid Id { get; set; }
}
