using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TerminalNetwork : UdonSharpBehaviour
{
    public const int REQUEST_KIND_ANSWER = 0;
    public const int REQUEST_KIND_INBOX = 1;

    [Header("Налаштування черги запитів")]
    public float minRequestInterval = 5.5f;

    [Header("Зв'язок з терміналом")]
    public CodeSenseiTerminal terminal;

    private const int QUEUE_SIZE = 8;

    private VRCUrl[] _queueUrls = new VRCUrl[QUEUE_SIZE];
    private int[] _queueKinds = new int[QUEUE_SIZE];

    private int _head = 0;
    private int _tail = 0;
    private int _count = 0;

    private float _lastRequestTime = -1000f;

    private bool _retryScheduled = false;

    private int _currentRequestKind = REQUEST_KIND_ANSWER;

    public void EnqueueUrl(VRCUrl url, int requestKind)
    {
        if (url == null)
        {
            Debug.LogWarning("[TerminalNetwork] Спроба додати порожній VRCUrl у чергу — проігноровано.");
            return;
        }

        if (_count >= QUEUE_SIZE)
        {
            Debug.LogWarning("[TerminalNetwork] Черга запитів переповнена (макс. " + QUEUE_SIZE + "), запит проігноровано.");
            return;
        }

        _queueUrls[_tail] = url;
        _queueKinds[_tail] = requestKind;
        _tail = (_tail + 1) % QUEUE_SIZE;
        _count++;

        TryProcessQueue();
    }

    public void TryProcessQueue()
    {
        _retryScheduled = false;

        if (_count == 0)
        {
            return;
        }

        float interval = minRequestInterval < 5.5f ? 5.5f : minRequestInterval;
        float elapsed = Time.time - _lastRequestTime;

        if (elapsed < interval)
        {
            if (!_retryScheduled)
            {
                _retryScheduled = true;
                SendCustomEventDelayedSeconds("TryProcessQueue", (interval - elapsed) + 0.05f);
            }
            return;
        }

        VRCUrl urlToSend = _queueUrls[_head];
        _currentRequestKind = _queueKinds[_head];
        _queueUrls[_head] = null;
        _head = (_head + 1) % QUEUE_SIZE;
        _count--;

        _lastRequestTime = Time.time;
        VRCStringDownloader.LoadUrl(urlToSend, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        string json = result.Result;

        DataToken rootToken;
        bool parsed = VRCJson.TryDeserializeFromJson(json, out rootToken);

        if (!parsed || rootToken.TokenType != TokenType.DataDictionary)
        {
            DeliverError("Сервер повернув некоректний JSON.");
            ScheduleNextIfAny();
            return;
        }

        DataDictionary dict = rootToken.DataDictionary;

        bool ok = false;
        DataToken okToken;
        if (dict.TryGetValue("ok", out okToken) && okToken.TokenType == TokenType.Boolean)
        {
            ok = okToken.Boolean;
        }

        string status = "";
        DataToken statusToken;
        if (dict.TryGetValue("status", out statusToken) && statusToken.TokenType == TokenType.String)
        {
            status = statusToken.String;
        }

        string[] lines = new string[0];
        DataToken linesToken;
        if (dict.TryGetValue("lines", out linesToken) && linesToken.TokenType == TokenType.DataList)
        {
            DataList linesList = linesToken.DataList;
            int n = linesList.Count;
            lines = new string[n];
            for (int i = 0; i < n; i++)
            {
                DataToken lineToken = linesList[i];
                lines[i] = lineToken.TokenType == TokenType.String ? lineToken.String : "";
            }
        }

        if (terminal != null)
        {
            terminal.ReceiveNetworkResult(_currentRequestKind, ok, status, lines, json);
        }

        ScheduleNextIfAny();
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        string errorMessage = result.Error;

        if (terminal != null)
        {
            terminal.ReceiveNetworkError(_currentRequestKind, errorMessage);
        }

        ScheduleNextIfAny();
    }

    private void DeliverError(string message)
    {
        if (terminal != null)
        {
            terminal.ReceiveNetworkError(_currentRequestKind, message);
        }
    }

    private void ScheduleNextIfAny()
    {
        if (_count > 0 && !_retryScheduled)
        {
            _retryScheduled = true;
            float interval = minRequestInterval < 5.5f ? 5.5f : minRequestInterval;
            SendCustomEventDelayedSeconds("TryProcessQueue", interval);
        }
    }
}