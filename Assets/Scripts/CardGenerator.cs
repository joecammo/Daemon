using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using TMPro;

public class CardGenerator : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    public GameObject cardPrefab;
    public Transform pullDeckParent; // Assign the PullDeck GameObject here
    public Transform cardDropZoneParent; // Assign the CardDropZone GameObject here

    [Header("Google Sheets CSV URLs")]
    public string abilitiesCsvUrl = "https://docs.google.com/spreadsheets/d/19-xHKD4eLu4m3hMph4hMttR52oEThk9O-pj12wp98_M/gviz/tq?tqx=out:csv&sheet=Abilities";
    public string playerDeckCsvUrl = "https://docs.google.com/spreadsheets/d/19-xHKD4eLu4m3hMph4hMttR52oEThk9O-pj12wp98_M/gviz/tq?tqx=out:csv&sheet=PlayerDeck";

    private Dictionary<string, CardData> abilitiesDict = new Dictionary<string, CardData>();

    void Start()
    {
        if (DaemonAffinityManager.Instance != null)
        {
            DaemonAffinityManager.Instance.LoadDaemonsAffinity(() =>
            {
                StartCoroutine(GenerateCardsFromSpreadsheet());
            });
        }
        else
        {
            Debug.LogError("DaemonAffinityManager.Instance is null! Make sure it's in the scene before CardGenerator runs.");
        }
    }

    IEnumerator GenerateCardsFromSpreadsheet()
    {
        // Download and parse Abilities
        yield return StartCoroutine(DownloadAbilities());
        // Download and parse PlayerDeck, then generate cards
        yield return StartCoroutine(DownloadAndGeneratePlayerDeck());
    }

    IEnumerator DownloadAbilities()
    {
        UnityWebRequest www = UnityWebRequest.Get(abilitiesCsvUrl);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download Abilities CSV: " + www.error);
            yield break;
        }
        ParseAbilitiesCsv(www.downloadHandler.text);
    }

    IEnumerator DownloadAndGeneratePlayerDeck()
    {
        UnityWebRequest www = UnityWebRequest.Get(playerDeckCsvUrl);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download PlayerDeck CSV: " + www.error);
            yield break;
        }
        ParsePlayerDeckCsvAndGenerate(www.downloadHandler.text);
    }

    // Minimal CSV parser that handles quoted fields and commas inside fields
    List<string[]> ParseCsv(string csv)
    {
        var result = new List<string[]>();
        var lines = csv.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = new List<string>();
            bool inQuotes = false;
            string curField = "";
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        curField += '"';
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(curField);
                    curField = "";
                }
                else
                {
                    curField += c;
                }
            }
            fields.Add(curField); // last field
            result.Add(fields.ToArray());
        }
        return result;
    }

    void ParseAbilitiesCsv(string csv)
    {
        var rows = ParseCsv(csv);
        Debug.Log($"[CardGenerator] Abilities CSV: Parsed {rows.Count} rows");
        if (rows.Count < 2) return;
        var header = rows[0];
        int titleIdx = System.Array.IndexOf(header, "Title");
        int affinityIdx = System.Array.IndexOf(header, "Affinity");
        int costIdx = System.Array.IndexOf(header, "Cost");
        int typeIdx = System.Array.IndexOf(header, "Type");
        int effectIdx = System.Array.IndexOf(header, "Effect");

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length <= Mathf.Max(titleIdx, affinityIdx, costIdx, typeIdx, effectIdx)) continue;
            if (string.IsNullOrWhiteSpace(row[titleIdx])) continue;

            var card = new CardData
            {
                title = row[titleIdx].Trim(),
                affinity = row[affinityIdx].Trim(),
                cost = row[costIdx].Trim(),
                type = row[typeIdx].Trim(),
                effect = row[effectIdx].Trim()
            };
            abilitiesDict[card.title] = card;
            Debug.Log($"[CardGenerator] Loaded ability: {card.title} ({card.type})");
        }
    }

    void ParsePlayerDeckCsvAndGenerate(string csv)
    {
        var rows = ParseCsv(csv);
        Debug.Log($"[CardGenerator] PlayerDeck CSV: Parsed {rows.Count} rows");
        if (rows.Count < 2) return;
        var header = rows[0];
        int titleIdx = System.Array.IndexOf(header, "Title");
        int daemonIdx = System.Array.IndexOf(header, "Daemon");
        int qtyIdx = System.Array.IndexOf(header, "Quantity");
        List<DeckEntry> deckList = new List<DeckEntry>();
for (int i = 1; i < rows.Count; i++)
{
    var row = rows[i];
    if (row.Length <= Mathf.Max(titleIdx, daemonIdx, qtyIdx)) continue;
    if (string.IsNullOrWhiteSpace(row[titleIdx])) continue;
    string title = row[titleIdx].Trim();
    string daemonName = row[daemonIdx].Trim();
    int quantity = int.TryParse(row[qtyIdx], out int q) ? q : 1;
    if (!abilitiesDict.ContainsKey(title)) {
        Debug.LogWarning($"[CardGenerator] PlayerDeck references unknown card: {title}");
        continue;
    }
    var cardData = abilitiesDict[title];
    for (int j = 0; j < quantity; j++)
    {
        deckList.Add(new DeckEntry { card = cardData, daemonName = daemonName });
        Debug.Log($"[CardGenerator] Added card to deck: {cardData.title} for daemon {daemonName}");
    }
}
        Debug.Log($"[CardGenerator] Total cards to instantiate: {deckList.Count}");
        // Shuffle deckList
        deckList = deckList.OrderBy(x => Random.value).ToList();
// Instantiate cards
List<GameObject> instantiatedCards = new List<GameObject>();
foreach (var entry in deckList)
{
    var card = entry.card;
    string daemonName = entry.daemonName;
    // Instantiate cards directly under CardDropZone (the hand)
    var cardObj = Instantiate(cardPrefab, cardDropZoneParent);
    instantiatedCards.Add(cardObj);
    Debug.Log($"[CardGenerator] Instantiated card: {card.title} for daemon {daemonName}");
    var display = cardObj.GetComponent<CardDisplay>();
    if (display)
    {
        Color affinityColor = Color.white;
        if (!string.IsNullOrEmpty(daemonName))
        {
            if (DaemonAffinityManager.Instance != null && DaemonAffinityManager.Instance.TryGetAffinity(daemonName, out var affinity))
            {
                affinityColor = AffinityColor.GetColor(affinity);
            }
            else
            {
                // fallback: use card.affinity from Abilities sheet
                affinityColor = AffinityColor.GetColor(AffinityColor.FromString(card.affinity));
            }
        }
        else
        {
            affinityColor = AffinityColor.GetColor(AffinityColor.FromString(card.affinity));
        }
        display.SetCard(card.title, card.cost, card.type, card.effect, affinityColor);
    }
}
        // Arrange cards in fan layout after instantiation
        var layout = cardDropZoneParent.GetComponent<HandLayoutAnimator>();
        if (layout != null)
        {
            layout.LayoutCards();
        }
    }

    

    class DeckEntry
{
    public CardData card;
    public string daemonName;
}

class CardData
{
    public string title;
    public string affinity;
    public string cost;
    public string type;
    public string effect;
}
}
