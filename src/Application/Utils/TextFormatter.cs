using System;
using System.Text;

namespace Application.Utils;

public static class TextFormatter
{
    public static string FormatForTerminal(string input, int maxLineLength = 55)
    {
        if (string.IsNullOrWhiteSpace(input)) 
            return string.Empty;

       
        string cleanText = input.Replace("```csharp", "")
            .Replace("```python", "")
            .Replace("```javascript", "")
            .Replace("```", "")
            .Replace("**", "")
            .Replace("*", "")
            .Trim();

        // Використовуємо StringBuilder із попередньо виділеною пам'яттю
        var sb = new StringBuilder(cleanText.Length + (cleanText.Length / maxLineLength));
        
        // Використовуємо Span для швидкого читання пам'яті без створення нових рядків
        ReadOnlySpan<char> span = cleanText.AsSpan();
        int currentLineLength = 0;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            if (c == '\n')
            {
                sb.Append(c);
                currentLineLength = 0;
                continue;
            }

            // Розбивка по межі слова, якщо перевищено довжину рядка
            if (currentLineLength >= maxLineLength && c == ' ')
            {
                sb.Append('\n');
                currentLineLength = 0;
                continue;
            }

            sb.Append(c);
            currentLineLength++;
        }

        return sb.ToString();
    }
}