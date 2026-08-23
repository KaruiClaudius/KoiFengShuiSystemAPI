namespace KoiFengShuiSystem.Modules.Identity.Application;

/// <summary>
/// Registration conflict for an already-registered email (council D1).
/// Extends ApplicationException so legacy handlers keep catching it while the
/// controller maps this type to the EMAIL_TAKEN code.
/// </summary>
public class EmailTakenException(string email) : ApplicationException($"Email '{email}' is already taken");
