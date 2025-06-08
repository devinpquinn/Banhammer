using UnityEngine;
using System.Collections;

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

    void Start()
    {
        normalMessages = new MessageSource("normal");
        warningMessages = new MessageSource("warning");
        banMessages = new MessageSource("ban");

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

        GameObject entry = Instantiate(chatEntryPrefab, chatLogContainer);
        entry.transform.Find("MessageText").GetComponent<TMPro.TextMeshProUGUI>().text = msgText;
        entry.GetComponent<ChatEntryDraggable>().category = category;
    }

    public enum ChatCategory { Normal, Warning, Ban }
}
