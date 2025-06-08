using System.Collections.Generic;
using UnityEngine;

public class MessageSource
{
    private readonly string resourceName;
    private readonly Queue<string> messageQueue = new();
    private List<string> originalMessages;

    public MessageSource(string resourceName)
    {
        this.resourceName = resourceName;
        LoadMessages();
        ShuffleQueue();
    }

    private void LoadMessages()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourceName);
        originalMessages = new List<string>(textAsset.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));
    }

    private void ShuffleQueue()
    {
        List<string> shuffled = new(originalMessages);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int rand = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[rand]) = (shuffled[rand], shuffled[i]);
        }

        foreach (var msg in shuffled)
            messageQueue.Enqueue(msg);
    }

    public string GetNextMessage()
    {
        if (messageQueue.Count == 0)
            ShuffleQueue();

        return messageQueue.Dequeue();
    }
}
