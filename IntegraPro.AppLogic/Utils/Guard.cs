namespace IntegraPro.AppLogic.Utils;

public static class Guard
{
    public static void AgainstNull(object? input, string parameterName)
    {
        if (input == null)
            throw new ArgumentNullException(parameterName, $"{parameterName} no puede ser nulo.");
    }

    public static void AgainstEmptyString(string input, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException($"{parameterName} no puede estar vacio.", parameterName);
    }

    public static string GenerateReadableId(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
