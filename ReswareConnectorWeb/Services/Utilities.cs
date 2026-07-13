namespace ReswareConnectorWeb.Services
{
    public static class Utilities
    {
        public static string GetEnvironmentVariableAnywhere(string variableName)
        {
            // Try Process (current process) first
            var value = Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrEmpty(value))
            {
                // Try User variables
                value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
            }

            if (string.IsNullOrEmpty(value))
            {
                // Try Machine/System variables
                value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
            }

            return value;
        }
    }
}
