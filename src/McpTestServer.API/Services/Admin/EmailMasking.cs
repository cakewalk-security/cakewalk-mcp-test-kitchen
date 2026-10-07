namespace McpTestServer.API.Services.Admin;

public static class EmailMasking
{
    public const string MaskedLocalPart = "***";

    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return MaskedLocalPart;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1)
        {
            return MaskedLocalPart;
        }

        return $"{MaskedLocalPart}{email[atIndex..]}";
    }
}
