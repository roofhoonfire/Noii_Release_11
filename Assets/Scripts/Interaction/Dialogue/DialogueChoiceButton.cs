using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private Action _onClick;

    public void Bind(string text, Action onClick)
    {
        if (label != null) label.text = text;
        _onClick = onClick;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke());
        }
    }

    private void Reset()
    {
        button = GetComponentInChildren<Button>();
        label = GetComponentInChildren<TMP_Text>();
    }
}
