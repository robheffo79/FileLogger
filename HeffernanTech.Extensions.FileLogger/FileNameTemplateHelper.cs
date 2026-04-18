using System;
using System.IO;
using System.Text;

namespace HeffernanTech.Extensions.FileLogging
{
    internal static class FileNameTemplateHelper
    {
        public static String SanitizeFileNamePart(String value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return String.Empty;
            }

            Char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(value.Length);

            for (Int32 i = 0; i < value.Length; i++)
            {
                Char c = value[i];
                if (Array.IndexOf(invalid, c) >= 0)
                {
                    sb.Append('_');
                }
                else if (Char.IsWhiteSpace(c))
                {
                    sb.Append('_');
                }
                else if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                {
                    sb.Append('_');
                }
                else if (c == ':' && i > 1)
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static String CleanupFileName(String value)
        {
            String sanitized = SanitizeFileNamePart(value);

            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            while (sanitized.Contains(".."))
            {
                sanitized = sanitized.Replace("..", "_");
            }

            sanitized = sanitized.Replace("_.", ".");
            sanitized = sanitized.Trim('_');

            if (String.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "log.log";
            }

            return sanitized;
        }
    }
}