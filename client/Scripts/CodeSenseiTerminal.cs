using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.Components;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class CodeSenseiTerminal : UdonSharpBehaviour
{
    public const int SYNC_STATUS_IDLE = 0;
    public const int SYNC_STATUS_ANSWER = 1;
    public const int SYNC_STATUS_ERROR = 2;

    private const int SYNCED_ANSWER_MAX_LENGTH = 1000;

    private const int STATE_IDLE = 0;
    private const int STATE_WAITING = 1;
    private const int STATE_PRINTING = 2;
    private const int STATE_ERROR = 3;
    private const int STATE_TICKET_WAITING = 4;

    public TerminalNetwork network;
    public TerminalDisplay display;
    public VRCUrlInputField freeQuestionInput;
    public VRCUrl inboxUrl;

    public float ticketPollInterval = 6f;
    public float ticketTimeoutSeconds = 180f;

    private const string TICKET_ALPHABET = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int TICKET_LENGTH = 5;

    private int _state = STATE_IDLE;
    private string _currentTicketCode = "";
    private float _ticketStartTime = -1f;
    private bool _firstAttemptResolved = false;

    [UdonSynced]
    private string _syncedAnswer = "";

    [UdonSynced]
    private int _syncedStatus = 0;

    public void AskPreset(VRCUrl url)
    {
        if (url == null)
        {
            return;
        }

        if (_state == STATE_WAITING || _state == STATE_TICKET_WAITING)
        {
            if (display != null)
            {
                display.ShowStatus("Зайнято попереднім запитом, зачекай.");
            }
            return;
        }

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        _state = STATE_WAITING;

        if (display != null)
        {
            display.ShowStatus("Обробляю запит…");
        }

        if (network != null)
        {
            network.EnqueueUrl(url, TerminalNetwork.REQUEST_KIND_ANSWER);
        }
    }

    public void SubmitFreeQuestion()
    {
        if (freeQuestionInput == null)
        {
            return;
        }

        VRCUrl url = freeQuestionInput.GetUrl();

        if (url == null)
        {
            if (display != null)
            {
                display.ShowStatus("Порожній запит.");
            }
            return;
        }

        AskPreset(url);
    }

    public void StartCodeReview()
    {
        if (_state == STATE_WAITING || _state == STATE_TICKET_WAITING)
        {
            if (display != null)
            {
                display.ShowStatus("Зайнято попереднім запитом, зачекай.");
            }
            return;
        }

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        _currentTicketCode = GenerateTicketCode();
        _ticketStartTime = Time.time;
        _state = STATE_TICKET_WAITING;

        if (display != null)
        {
            display.ShowStatus("Твій код: " + _currentTicketCode + ". Введи його на сайті бекенда.");
        }

        PollInbox();
    }

    public void CancelRequest()
    {
        _state = STATE_IDLE;
        _currentTicketCode = "";
        _ticketStartTime = -1f;

        if (display != null)
        {
            display.ShowStatus("Очікую");
        }
    }

    public void PollInbox()
    {
        if (_state != STATE_TICKET_WAITING)
        {
            return;
        }

        if (Time.time - _ticketStartTime >= ticketTimeoutSeconds)
        {
            _state = STATE_ERROR;

            if (display != null)
            {
                display.ShowStatus("Таймаут очікування код-рев'ю (3 хв). Спробуй ще раз.");
            }

            return;
        }

        if (network != null && inboxUrl != null)
        {
            network.EnqueueUrl(inboxUrl, TerminalNetwork.REQUEST_KIND_INBOX);
        }

        SendCustomEventDelayedSeconds("PollInbox", ticketPollInterval);
    }

    public void ReceiveNetworkResult(int requestKind, bool ok, string status, string[] lines, string rawJson)
    {
        bool isFirstAttempt = !_firstAttemptResolved;
        _firstAttemptResolved = true;

        if (requestKind == TerminalNetwork.REQUEST_KIND_INBOX)
        {
            HandleInboxResult(rawJson);
            return;
        }

        if (!ok)
        {
            _state = STATE_ERROR;

            string message = (lines != null && lines.Length > 0) ? lines[0] : "сервер повернув помилку.";

            if (display != null)
            {
                display.ShowStatus("Помилка: " + message);
            }

            SyncAnswer(SYNC_STATUS_ERROR, message);
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            _state = STATE_ERROR;

            if (display != null)
            {
                display.ShowStatus("Порожня відповідь від сервера.");
            }

            SyncAnswer(SYNC_STATUS_ERROR, "Порожня відповідь від сервера.");
            return;
        }

        _state = STATE_PRINTING;

        if (display != null)
        {
            display.ShowStatus("Готово");
            display.ShowLines(lines);
        }

        _state = STATE_IDLE;

        SyncAnswer(SYNC_STATUS_ANSWER, JoinLines(lines));
    }

    public void ReceiveNetworkError(int requestKind, string errorMessage)
    {
        bool isFirstAttempt = !_firstAttemptResolved;
        _firstAttemptResolved = true;

        if (requestKind == TerminalNetwork.REQUEST_KIND_INBOX && _state == STATE_TICKET_WAITING)
        {
            return;
        }

        _state = STATE_ERROR;

        if (display == null)
        {
            return;
        }

        if (isFirstAttempt)
        {
            string hint = "Немає з'єднання. Увімкни 'Allow Untrusted URLs' у налаштуваннях VRChat (Settings → Security) і спробуй ще раз.";
            display.ShowStatus(hint);
            SyncAnswer(SYNC_STATUS_ERROR, hint);
            return;
        }

        display.ShowStatus("Немає з'єднання: " + errorMessage);
        SyncAnswer(SYNC_STATUS_ERROR, "Немає з'єднання: " + errorMessage);
    }

    private void HandleInboxResult(string rawJson)
    {
        if (_state != STATE_TICKET_WAITING)
        {
            return;
        }

        if (Time.time - _ticketStartTime >= ticketTimeoutSeconds)
        {
            _state = STATE_ERROR;

            if (display != null)
            {
                display.ShowStatus("Таймаут очікування код-рев'ю (3 хв). Спробуй ще раз.");
            }

            return;
        }

        DataToken rootToken;
        bool parsed = VRCJson.TryDeserializeFromJson(rawJson, out rootToken);

        if (!parsed || rootToken.TokenType != TokenType.DataDictionary)
        {
            return;
        }

        DataDictionary dict = rootToken.DataDictionary;
        DataToken itemsToken;

        if (!dict.TryGetValue("items", out itemsToken) || itemsToken.TokenType != TokenType.DataList)
        {
            return;
        }

        DataList items = itemsToken.DataList;
        int n = items.Count;

        for (int i = 0; i < n; i++)
        {
            DataToken itemToken = items[i];

            if (itemToken.TokenType != TokenType.DataDictionary)
            {
                continue;
            }

            DataDictionary item = itemToken.DataDictionary;
            DataToken codeToken;
            string code = "";

            if (item.TryGetValue("code", out codeToken) && codeToken.TokenType == TokenType.String)
            {
                code = codeToken.String;
            }

            if (code.ToUpper() != _currentTicketCode)
            {
                continue;
            }

            DataToken statusToken;
            string status = "";

            if (item.TryGetValue("status", out statusToken) && statusToken.TokenType == TokenType.String)
            {
                status = statusToken.String;
            }

            if (status != "completed" && status != "error")
            {
                continue;
            }

            string[] itemLines = new string[0];
            DataToken linesToken;

            if (item.TryGetValue("lines", out linesToken) && linesToken.TokenType == TokenType.DataList)
            {
                DataList linesList = linesToken.DataList;
                int ln = linesList.Count;
                itemLines = new string[ln];

                for (int j = 0; j < ln; j++)
                {
                    DataToken lt = linesList[j];
                    itemLines[j] = lt.TokenType == TokenType.String ? lt.String : "";
                }
            }

            _state = STATE_PRINTING;

            if (display != null)
            {
                if (status == "error")
                {
                    display.ShowStatus("Код-рев'ю з помилкою");
                }
                else
                {
                    display.ShowStatus("Код-рев'ю отримано");
                }

                display.ShowLines(itemLines);
            }

            if (status == "error")
            {
                _state = STATE_ERROR;
                SyncAnswer(SYNC_STATUS_ERROR, JoinLines(itemLines));
            }
            else
            {
                _state = STATE_IDLE;
                SyncAnswer(SYNC_STATUS_ANSWER, JoinLines(itemLines));
            }

            return;
        }
    }

    private string GenerateTicketCode()
    {
        char[] codeChars = new char[TICKET_LENGTH];
        int alphabetLength = TICKET_ALPHABET.Length;

        for (int i = 0; i < TICKET_LENGTH; i++)
        {
            int idx = Random.Range(0, alphabetLength);
            codeChars[i] = TICKET_ALPHABET[idx];
        }

        return new string(codeChars);
    }

    private void SyncAnswer(int syncStatus, string answerText)
    {
        string text = answerText != null ? answerText : "";

        if (text.Length > SYNCED_ANSWER_MAX_LENGTH)
        {
            text = text.Substring(0, SYNCED_ANSWER_MAX_LENGTH);
        }

        _syncedStatus = syncStatus;
        _syncedAnswer = text;

        RequestSerialization();
    }

    private string JoinLines(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return "";
        }

        string result = lines[0];

        for (int i = 1; i < lines.Length; i++)
        {
            result = result + "\n" + lines[i];
        }

        return result;
    }

    public override void OnDeserialization()
    {
        if (display == null)
        {
            return;
        }

        if (_syncedStatus == SYNC_STATUS_ANSWER)
        {
            string[] lines = _syncedAnswer.Split('\n');
            _state = STATE_PRINTING;
            display.ShowStatus("Готово");
            display.ShowLines(lines);
            _state = STATE_IDLE;
            return;
        }

        if (_syncedStatus == SYNC_STATUS_ERROR)
        {
            _state = STATE_ERROR;
            display.ShowStatus(_syncedAnswer);
        }
    }
}