using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BinReceiver : MonoBehaviour
{
    public ChatSpawner.BinType binType;

    public void Receive(ChatEntryDraggable entry)
    {
        FindObjectOfType<ChatSpawner>().ReportDrop(entry.category, binType);
    }
}
