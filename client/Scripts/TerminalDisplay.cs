using UdonSharp;
using UnityEngine;
using TMPro;

public class TerminalDisplay : UdonSharpBehaviour
{
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI pageIndicatorText;

    public float typingIntervalSeconds = 0.3f;
    public int linesPerPage = 6;

    private string[] _currentLines = new string[0];
    private int _currentPage = 0;
    private int _totalPages = 1;
    private int _typingLineIndexInPage = 0;
    private int _typingGeneration = 0;
    private int _activeTypingGeneration = -1;

    public void ShowLines(string[] lines)
    {
        _currentLines = lines != null ? lines : new string[0];

        int lineCount = _currentLines.Length;
        _totalPages = lineCount == 0 ? 1 : Mathf.CeilToInt((float)lineCount / linesPerPage);
        _currentPage = 0;

        _typingGeneration++;
        _activeTypingGeneration = _typingGeneration;
        _typingLineIndexInPage = 0;

        if (mainText != null)
        {
            mainText.text = "";
        }

        UpdatePageIndicator();
        TypeNextLine();
    }

    public void TypeNextLine()
    {
        if (_activeTypingGeneration != _typingGeneration)
        {
            return;
        }

        int startIndex = _currentPage * linesPerPage;
        int endIndexExclusive = Mathf.Min(startIndex + linesPerPage, _currentLines.Length);
        int globalIndex = startIndex + _typingLineIndexInPage;

        if (globalIndex >= endIndexExclusive)
        {
            return;
        }

        if (mainText != null)
        {
            mainText.text = mainText.text + _currentLines[globalIndex] + "\n";
        }

        _typingLineIndexInPage++;
        SendCustomEventDelayedSeconds("TypeNextLine", typingIntervalSeconds);
    }

    public void NextPage()
    {
        if (_currentPage + 1 >= _totalPages)
        {
            return;
        }

        _currentPage++;
        ShowPageInstant();
    }

    public void PreviousPage()
    {
        if (_currentPage <= 0)
        {
            return;
        }

        _currentPage--;
        ShowPageInstant();
    }

    private void ShowPageInstant()
    {
        _typingGeneration++;
        _activeTypingGeneration = -1;

        int startIndex = _currentPage * linesPerPage;
        int endIndexExclusive = Mathf.Min(startIndex + linesPerPage, _currentLines.Length);
        string buffer = "";

        for (int i = startIndex; i < endIndexExclusive; i++)
        {
            buffer = buffer + _currentLines[i] + "\n";
        }

        if (mainText != null)
        {
            mainText.text = buffer;
        }

        UpdatePageIndicator();
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicatorText == null)
        {
            return;
        }

        pageIndicatorText.text = (_currentPage + 1) + " / " + _totalPages;
    }

    public void ShowStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }
}
