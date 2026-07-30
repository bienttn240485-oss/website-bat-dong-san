namespace RealEstateManagement.Application.Properties;

public static class PropertyReferenceCode
{
    public static string FromInternalCode(string code)
    {
        var parts = code.Trim().ToUpperInvariant().Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        if (parts.Length == 1)
        {
            return parts[0];
        }

        var prefix = parts[0] switch
        {
            "TR" => "TR",
            "OR" => "OR",
            "GH" => "GH",
            "BV" => "BV",
            "BS7" or "BS8" or "BS9" => parts[0],
            "LBV" => "LBV",
            "MCP" => "MCP",
            "MAN" => "MAN",
            "MG" => "MG",
            "OP" => "OP",
            _ => parts[0]
        };

        var unit = parts[^1];
        return $"{prefix}-{unit}";
    }
}
