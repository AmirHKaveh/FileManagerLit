namespace FileManager.Helpers
{
    public static class Extensions
    {
        public static string GetSubstringToLastSlash(this string input)
        {
            // Find the position of the last slash
            int lastSlashIndex = input.LastIndexOf('\\'); // For Windows
            int lastSlashIndexForward = input.LastIndexOf('/'); // For Unix/Linux

            // Choose the one that occurs last
            int lastIndex = Math.Max(lastSlashIndex, lastSlashIndexForward);

            // If not found, return the original string or an empty string
            if (lastIndex == -1)
            {
                return string.Empty; // or return input if you prefer to keep the full string
            }

            // Extract substring from the start to the last slash
            return input.Substring(0, lastIndex + 1).TrimEnd('/');
        }
    }
}
