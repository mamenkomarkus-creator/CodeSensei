using System.Text;

namespace Application.Formatting;

public static class TextFormatter
{
    public const int DefaultLineWidth = 55;

    public static string FormatForTerminal(string? input, int maxLineLength = DefaultLineWidth)
    {
        if (string.IsNullOrWhiteSpace(input) || maxLineLength <= 0)
            return string.Empty;

        ReadOnlySpan<char> source = input.AsSpan();
        Span<char> stripped = source.Length <= 1024
            ? stackalloc char[source.Length]
            : new char[source.Length];

        int length = StripMarkdown(source, stripped);
        ReadOnlySpan<char> clean = stripped[..length].Trim();
        if (clean.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder(clean.Length + clean.Length / maxLineLength + 4);
        WrapLines(clean, maxLineLength, sb);
        return sb.ToString();
    }

    private static int StripMarkdown(ReadOnlySpan<char> source, Span<char> dest)
    {
        int w = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (StartsWith(source, i, "```"))
            {
                i += 2;
                int look = i + 1;
                while (look < source.Length && char.IsLetterOrDigit(source[look]))
                    look++;
                i = look - 1;
                continue;
            }

            if (StartsWith(source, i, "**"))
            {
                i++;
                continue;
            }

            dest[w++] = source[i];
        }

        return w;
    }

    private static void WrapLines(ReadOnlySpan<char> text, int width, StringBuilder sb)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] is '\n' or '\r')
            {
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index++;

                sb.Append('\n');
                index++;
                continue;
            }

            int lineEnd = index;
            int lastBreak = -1;
            int column = 0;

            while (lineEnd < text.Length && text[lineEnd] is not ('\n' or '\r') && column < width)
            {
                if (char.IsWhiteSpace(text[lineEnd]))
                    lastBreak = lineEnd;

                lineEnd++;
                column++;
            }

            if (lineEnd < text.Length && text[lineEnd] is not ('\n' or '\r') && lastBreak > index)
                lineEnd = lastBreak;

            ReadOnlySpan<char> line = text[index..lineEnd].TrimEnd();
            sb.Append(line);

            index = lineEnd;
            while (index < text.Length && text[index] == ' ')
                index++;

            if (index < text.Length && text[index] is not ('\n' or '\r'))
                sb.Append('\n');
        }
    }

    private static bool StartsWith(ReadOnlySpan<char> source, int index, string token)
    {
        if (index + token.Length > source.Length)
            return false;

        return source.Slice(index, token.Length).Equals(token.AsSpan(), StringComparison.Ordinal);
    }
}
