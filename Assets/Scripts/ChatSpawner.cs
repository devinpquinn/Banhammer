/*  ChatSpawner.cs
 *  Drives one “stream” (round) of chat, spawns entries, and keeps score by
 *  counting the player’s moderation mistakes.  Attach this to an empty Game-Object.
 *
 *  Requires:
 *    • ChatEntry prefab that has:
 *        – ChatEntryUI   (exposes usernameText / messageText references)
 *        – ChatEntryDraggable (stores ChatCategory, handles drag-and-drop)
 *    • Bin images with a BinReceiver script (see notes at bottom)
 *    • Five text files in Assets/Resources/
 *        – normal.txt   – warning.txt   – ban.txt
 *        – hello.txt    – goodbye.txt
 *        – usernames.txt
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ChatSpawner : MonoBehaviour
{
    /* ────────────────── public inspector fields ────────────────── */    [Header("Containers & Prefabs")]
    [Tooltip("Parent for the spawned chat entries (typically the Content object inside the Scroll View).")]
    public Transform chatLogContainer;
    public GameObject chatEntryPrefab;
    public ScrollRect scrollRect;          // optional auto-scroll to bottom

    [Header("Mistake Summary UI")]
    [Tooltip("Parent container with vertical layout group for mistake summary entries")]
    public GameObject mistakeSummaryPanel;
    public Transform mistakeSummaryContainer;
    public GameObject mistakeSummaryPrefab;
    [Tooltip("Text component to display the final score")]
    public TextMeshProUGUI finalScoreText;

    [Header("Phase Durations (seconds)")]
    public float greetingSeconds = 5f;    // hello burst before stream starts
    public float streamSeconds = 60f;   // main gameplay window
    public float goodbyeSeconds = 5f;    // goodbye burst after stream ends

    [Header("Spawn Delay Ranges (sec)")]
    public Vector2 greetingDelay = new(0.25f, 0.45f);
    public Vector2 streamDelay = new(0.60f, 1.50f);
    public Vector2 goodbyeDelay = new(0.25f, 0.45f);

    [Header("Main-Phase Probabilities")]
    [Range(0f, 1f)] public float warningChance = 0.10f;
    [Range(0f, 1f)] public float banChance = 0.03f;

    /* ─────────────── mistake weights (tweak in Inspector) ─────────────── */
    [System.Serializable]
    public class MistakeWeight
    {
        public MistakeType type;
        public int weight = 1;
    }
    [Header("Mistake Weights")]
    public List<MistakeWeight> mistakeWeights = new();

    /* ────────────────── enums ────────────────── */
    public enum ChatCategory { Normal, Warning, Ban }
    public enum MistakeType
    {
        BannedNormal,    // normal message placed in Ban bin
        BannedWarning,   // warning-worthy message placed in Ban bin
        WarnedNormal,    // normal message placed in Warning bin
        WarnedBan,       // ban-worthy message placed in Warning bin
        MissedBan,       // ban-worthy message left in log
        MissedWarning    // warning-worthy message left in log
    }
    public enum BinType { Warning, Ban }

    /* ────────────────── private state ────────────────── */
    private MessageSource normalSrc, warningSrc, banSrc, helloSrc, goodbyeSrc;
    private List<string> usernames;
    private readonly Dictionary<MistakeType, int> mistakeCounts = new();

    /* ────────────────── Unity lifecycle ────────────────── */
    private void Start()
    {
        // text pools
        normalSrc = new MessageSource("normal");
        warningSrc = new MessageSource("warning");
        banSrc = new MessageSource("ban");
        helloSrc = new MessageSource("hello");
        goodbyeSrc = new MessageSource("goodbye");

        // usernames
        usernames = new List<string>(
            Resources.Load<TextAsset>("usernames")
                .text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
        );

        // initialise counters
        foreach (MistakeType mt in System.Enum.GetValues(typeof(MistakeType)))
            mistakeCounts[mt] = 0;

        // start master coroutine
        StartCoroutine(StreamLoop());
    }

    /* ────────────────── public API called by BinReceiver ────────────────── */
    public void ReportDrop(ChatCategory msgCat, BinType bin)
    {
        switch (bin)
        {
            case BinType.Ban:
                if (msgCat == ChatCategory.Normal) ReportMistake(MistakeType.BannedNormal);
                else if (msgCat == ChatCategory.Warning) ReportMistake(MistakeType.BannedWarning);
                break;

            case BinType.Warning:
                if (msgCat == ChatCategory.Normal) ReportMistake(MistakeType.WarnedNormal);
                else if (msgCat == ChatCategory.Ban) ReportMistake(MistakeType.WarnedBan);
                break;
        }
    }

    /* ────────────────── master game-round coroutine ────────────────── */
    private IEnumerator StreamLoop()
    {
        ClearChatLog();

        // reset mistake counts per round
        foreach (var mt in new List<MistakeType>(mistakeCounts.Keys))
        {
            mistakeCounts[mt] = 0;
        }

        // 1) greeting burst
        yield return SpawnBurst(helloSrc, greetingDelay, greetingSeconds);

        // 2) main stream phase
        yield return SpawnMainPhase();

        // 3) before goodbyes tally what’s left
        TallyRemainingMistakes();

        // 4) goodbye burst
        yield return SpawnBurst(goodbyeSrc, goodbyeDelay, goodbyeSeconds);

        // 5) print summary
        PrintMistakeSummary();
    }

    /* ────────────────── phase helpers ────────────────── */
    private IEnumerator SpawnBurst(MessageSource src, Vector2 delayRange, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            SpawnEntry(src.GetNextMessage(), ChatCategory.Normal);
            float d = Random.Range(delayRange.x, delayRange.y);
            t += d;
            yield return new WaitForSeconds(d);
        }
    }

    private IEnumerator SpawnMainPhase()
    {
        float t = 0f;
        while (t < streamSeconds)
        {
            SpawnWeightedMain();
            float d = Random.Range(streamDelay.x, streamDelay.y);
            t += d;
            yield return new WaitForSeconds(d);
        }
    }

    /* ────────────────── spawn helpers ────────────────── */
    private void SpawnWeightedMain()
    {
        float r = Random.value;
        if (r < banChance) SpawnEntry(banSrc.GetNextMessage(), ChatCategory.Ban);
        else if (r < banChance + warningChance) SpawnEntry(warningSrc.GetNextMessage(), ChatCategory.Warning);
        else SpawnEntry(normalSrc.GetNextMessage(), ChatCategory.Normal);
    }

    private void SpawnEntry(string message, ChatCategory category)
    {
        string user = usernames[Random.Range(0, usernames.Count)];

        // Instantiate & parent
        GameObject entry = Instantiate(chatEntryPrefab);

        // Get references
        RectTransform entryRect = entry.GetComponent<RectTransform>();
        ContentSizeFitter fitter = entry.GetComponent<ContentSizeFitter>();

        // Disable layout while populating
        if (fitter != null) fitter.enabled = false;

        // Set parent (without world position retention)
        entry.transform.SetParent(chatLogContainer, false);

        // Set username + message();
        TextMeshProUGUI entryText = entry.GetComponentInChildren<TextMeshProUGUI>();
        entryText.text = $"<b>{user}</b>: {message}";

        // category (for drag bins)
        entry.GetComponent<ChatEntryDraggable>().category = category;

        // Re-enable layout fitter
        if (fitter != null) fitter.enabled = true;

        // Force layout now (important!)
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chatLogContainer);
        LayoutRebuilder.ForceRebuildLayoutImmediate(entryRect);
    }

    /* ────────────────── mistake bookkeeping ────────────────── */
    private void ReportMistake(MistakeType type) => mistakeCounts[type]++;

    private void TallyRemainingMistakes()
    {
        foreach (Transform child in chatLogContainer)
        {
            var d = child.GetComponent<ChatEntryDraggable>();
            if (!d) continue;

            if (d.category == ChatCategory.Ban) mistakeCounts[MistakeType.MissedBan]++;
            else if (d.category == ChatCategory.Warning) mistakeCounts[MistakeType.MissedWarning]++;
        }
    }    private void PrintMistakeSummary()
    {
        Debug.Log("=== Mistake Summary ===");
        int total = 0;

        foreach (MistakeType mt in mistakeCounts.Keys)
        {
            int count = mistakeCounts[mt];
            int weight = mistakeWeights.Find(m => m.type == mt)?.weight ?? 1;
            int penalty = count * weight;
            total += penalty;

            Debug.Log($"{mt}: {count}  (weight {weight}, penalty {penalty})");
        }
        Debug.Log($"TOTAL PENALTY: {total}");
        
        // Display in UI
        DisplayMistakeSummaryUI(total);
    }

    private void DisplayMistakeSummaryUI(int totalPenalty)
    {
        if (mistakeSummaryContainer == null || mistakeSummaryPrefab == null)
        {
            Debug.LogWarning("Mistake summary UI components not assigned!");
            return;
        }
        
        // Clear the mistake summary panel
        if (mistakeSummaryPanel != null)
        {
            mistakeSummaryPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Mistake summary panel not assigned!");
        }
        
        // Clear previous final score text
        if (finalScoreText != null)
        {
            finalScoreText.text = ""; // Clear previous score
        }
        else
        {
            Debug.LogWarning("Final score text not assigned!");
        }

        // Clear existing summary entries
        foreach (Transform child in mistakeSummaryContainer)
        {
            Destroy(child.gameObject);
        }

        // Create summary entries for each mistake type that occurred
        foreach (MistakeType mt in mistakeCounts.Keys)
        {
            int count = mistakeCounts[mt];
            if (count > 0) // Only show mistakes that actually occurred
            {
                int weight = mistakeWeights.Find(m => m.type == mt)?.weight ?? 1;
                int penalty = count * weight;
                
                CreateMistakeSummaryEntry(mt, count, penalty);
            }
        }

        // Calculate and display final score
        int finalScore = Mathf.Max(0, 100 - totalPenalty);
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {finalScore}";
        }
    }

    private void CreateMistakeSummaryEntry(MistakeType mistakeType, int count, int penalty)
    {
        GameObject summaryEntry = Instantiate(mistakeSummaryPrefab, mistakeSummaryContainer);
        
        // Get the TextMeshProUGUI components (assuming they are direct children)
        TextMeshProUGUI[] textComponents = summaryEntry.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (textComponents.Length >= 2)
        {
            // First text component: mistake type and count
            textComponents[0].text = $"{mistakeType} x {count}";
            
            // Second text component: penalty
            textComponents[1].text = $"-{penalty}";
        }
        else
        {
            Debug.LogWarning($"MistakeSummary prefab should have at least 2 TextMeshProUGUI components, found {textComponents.Length}");
        }
    }

    /* ────────────────── utility ────────────────── */
    private void ClearChatLog()
    {
        foreach (Transform child in chatLogContainer)
            Destroy(child.gameObject);
    }
}

/* ─────────────────────────────────────────────────────────────────────────────
 *  MessageSource:  no-repeat text pool with shuffle
 * ────────────────────────────────────────────────────────────────────────────*/
public class MessageSource
{
    private readonly Queue<string> queue = new();
    private readonly List<string> pool = new();

    public MessageSource(string resource)
    {
        var ta = Resources.Load<TextAsset>(resource);
        pool.AddRange(ta.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));
        ShuffleBackIntoQueue();
    }

    public string GetNextMessage()
    {
        if (queue.Count == 0) ShuffleBackIntoQueue();
        return queue.Dequeue();
    }

    private void ShuffleBackIntoQueue()
    {
        for (int i = 0; i < pool.Count; ++i)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        foreach (var s in pool) queue.Enqueue(s);
    }
}
