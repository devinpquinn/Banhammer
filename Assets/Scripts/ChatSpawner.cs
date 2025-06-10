using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatSpawner : MonoBehaviour
{
    [Header("Containers & Prefabs")]
    public Transform chatLogContainer;
    public GameObject chatEntryPrefab;
    public ScrollRect scrollRect;               // optional auto-scroll

    [Header("Phase Durations (seconds)")]
    public float preStreamGreetingSeconds  = 5f;
    public float streamSeconds             = 60f;
    public float postStreamGoodbyeSeconds  = 5f;

    [Header("Spawn Delay Ranges")]
    public Vector2 greetingDelay   = new(0.25f, 0.45f);  // faster
    public Vector2 normalDelay     = new(0.60f, 1.50f);  // main phase
    public Vector2 goodbyeDelay    = new(0.25f, 0.45f);  // faster

    [Header("Main-Phase Probabilities")]
    [Range(0,1)] public float warningChance = 0.10f;
    [Range(0,1)] public float banChance     = 0.03f;

    /* ─────────────────────  private ───────────────────── */
    private MessageSource normalSrc, warningSrc, banSrc;
    private MessageSource helloSrc, goodbyeSrc;
    private List<string> usernames;

    private void Start()
    {
        normalSrc   = new MessageSource("normal");
        warningSrc  = new MessageSource("warning");
        banSrc      = new MessageSource("ban");
        helloSrc    = new MessageSource("hello");
        goodbyeSrc  = new MessageSource("goodbye");

        usernames = new List<string>(
            Resources.Load<TextAsset>("usernames")
                     .text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));

        StartCoroutine(StreamLoop());
    }

    /* ─────────────  master coroutine driving one round ───────────── */
    private IEnumerator StreamLoop()
    {
        while (true)           // run forever; remove loop if you only want one round
        {
            yield return StartCoroutine(SpawnBurst(
                helloSrc, greetingDelay, preStreamGreetingSeconds));

            yield return StartCoroutine(SpawnMainPhase());

            yield return StartCoroutine(SpawnBurst(
                goodbyeSrc, goodbyeDelay, postStreamGoodbyeSeconds));

            // pause before next round (optional)
            yield return new WaitForSeconds(3f);
        }
    }

    /* ─────────────── 1) greetings & goodbyes ─────────────── */
    private IEnumerator SpawnBurst(MessageSource source, Vector2 delayRange, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            SpawnEntry(source.GetNextMessage(), ChatCategory.Normal);
            float d = Random.Range(delayRange.x, delayRange.y);
            t += d;
            yield return new WaitForSeconds(d);
        }
    }

    /* ─────────────── 2) main phase ─────────────── */
    private IEnumerator SpawnMainPhase()
    {
        float t = 0f;
        while (t < streamSeconds)
        {
            SpawnWeightedMain();                // choose normal / warning / ban
            float d = Random.Range(normalDelay.x, normalDelay.y);
            t += d;
            yield return new WaitForSeconds(d);
        }
    }

    /* ─────────────── helpers ─────────────── */
    private void SpawnWeightedMain()
    {
        float r = Random.value;
        if (r < banChance)
            SpawnEntry(banSrc.GetNextMessage(), ChatCategory.Ban);
        else if (r < banChance + warningChance)
            SpawnEntry(warningSrc.GetNextMessage(), ChatCategory.Warning);
        else
            SpawnEntry(normalSrc.GetNextMessage(), ChatCategory.Normal);
    }

    private void SpawnEntry(string msg, ChatCategory cat)
    {
        string user = usernames[Random.Range(0, usernames.Count)];

        // instantiate & parent
        GameObject entry = Instantiate(chatEntryPrefab);

        // Get references
        RectTransform entryRect = entry.GetComponent<RectTransform>();
        ContentSizeFitter fitter = entry.GetComponent<ContentSizeFitter>();

        // Disable layout while populating
        if (fitter != null) fitter.enabled = false;

        // Set parent (without world position retention)
        entry.transform.SetParent(chatLogContainer, false);

        // Set username + message
        var texts = entry.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        texts[0].text = user;
        texts[1].text = msg;
        
        // category (for drag bins)
        entry.GetComponent<ChatEntryDraggable>().category = cat;
        
        // Re-enable layout fitter
        if (fitter != null) fitter.enabled = true;

        // Force layout now (important!)
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chatLogContainer);
        LayoutRebuilder.ForceRebuildLayoutImmediate(entryRect);
    }

    /* ─────────────── tiny enum ─────────────── */
    public enum ChatCategory { Normal, Warning, Ban }
}
