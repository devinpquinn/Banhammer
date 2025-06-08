using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ChatSpawner : MonoBehaviour
{
    [Header("Delays")]
    public float minDelay = 0.5f;
    public float maxDelay = 2f;

    [Header("Weights")]
    [Range(0, 1)] public float warningChance = 0.1f;
    [Range(0, 1)] public float banChance = 0.03f;

    [Header("UI")]
    public Transform chatLogContainer;
    public GameObject chatEntryPrefab; // must have ChatEntryDraggable on it

    private MessageSource normalMessages;
    private MessageSource warningMessages;
    private MessageSource banMessages;
    private List<string> usernames;

    void Start()
    {
        normalMessages = new MessageSource("normal");
        warningMessages = new MessageSource("warning");
        banMessages = new MessageSource("ban");

        usernames = new List<string>(Resources.Load<TextAsset>("usernames")
            .text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            SpawnChatMessage();
        }
    }

    private void SpawnChatMessage()
    {
        string msgText;
        ChatCategory category;

        float roll = Random.value;

        if (roll < banChance)
        {
            msgText = banMessages.GetNextMessage();
            category = ChatCategory.Ban;
        }
        else if (roll < banChance + warningChance)
        {
            msgText = warningMessages.GetNextMessage();
            category = ChatCategory.Warning;
        }
        else
        {
            msgText = normalMessages.GetNextMessage();
            category = ChatCategory.Normal;
        }

        string username = usernames[Random.Range(0, usernames.Count)];

        GameObject entry = Instantiate(chatEntryPrefab);

        // Get references
        RectTransform entryRect = entry.GetComponent<RectTransform>();
        ContentSizeFitter fitter = entry.GetComponent<ContentSizeFitter>();
        VerticalLayoutGroup layoutGroup = entry.GetComponent<VerticalLayoutGroup>();

        // Disable layout while populating
        if (fitter != null) fitter.enabled = false;

        // Set parent (without world position retention)
        entry.transform.SetParent(chatLogContainer, false);

        // Set username + message
        var texts = entry.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        texts[0].text = username;
        texts[1].text = msgText;
        
        // Re-enable layout fitter
        if (fitter != null) fitter.enabled = true;

        // Force layout now (important!)
        LayoutRebuilder.ForceRebuildLayoutImmediate(entryRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chatLogContainer);
    }

    public enum ChatCategory { Normal, Warning, Ban }
}
