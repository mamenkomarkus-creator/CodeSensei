using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;

public class PresetButton : UdonSharpBehaviour
{
    public VRCUrl url;
    public string label;
    public CodeSenseiTerminal terminal;

    private TextMeshProUGUI _labelText;

    private void Start()
    {
        _labelText = GetComponentInChildren<TextMeshProUGUI>();

        if (_labelText != null)
        {
            _labelText.text = label;
        }
    }

    public void OnPresetClicked()
    {
        if (terminal != null && url != null)
        {
            terminal.AskPreset(url);
        }
    }
}
