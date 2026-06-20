using System;
using System.Collections.Generic;
using System.Text;

namespace CU.RemoteConsole.Web;

internal static class SerializationHelper
{
    public static string Escape(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    public static string NullableDate(DateTimeOffset? value)
    {
        return value.HasValue ? "\"" + value.Value.ToString("O") + "\"" : "null";
    }

    public static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    private const int MaxLineLength = 50000;
    private const int MaxTotalChars = 500000;

    public static string LimitOutput(string value)
    {
        if (value.Length <= MaxLineLength)
        {
            return value;
        }

        return value.Substring(0, MaxLineLength) + "...";
    }

    public static bool IsOutputTruncated(IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].Length > MaxLineLength)
            {
                return true;
            }
        }

        return false;
    }

    public static string SerializeStringArray(IReadOnlyList<string> values)
    {
        var builder = new StringBuilder();
        var totalChars = 0;
        builder.Append('[');
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            var limited = LimitOutput(values[i]);
            var remaining = MaxTotalChars - totalChars;
            if (remaining <= 0)
            {
                builder.Append('"').Append("...").Append('"');
                break;
            }
            if (limited.Length > remaining)
            {
                limited = limited.Substring(0, remaining) + "...";
            }

            builder.Append('"').Append(Escape(limited)).Append('"');
            totalChars += limited.Length;
        }

        builder.Append(']');
        return builder.ToString();
    }
}
