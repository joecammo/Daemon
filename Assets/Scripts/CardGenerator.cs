using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using TMPro;
using UnityEngine.UI; // For LayoutRebuilder

public class CardGenerator : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    public GameObject cardPrefab;
    public Transform pullDeckParent; // Assign the PullDeck GameObject here
    public Transform cardDropZoneParent; // Assign the CardDropZone GameObject here
    public Transform cardCanvasParent; // Assign the CardCanvas GameObject here for UI cards

    [Header("Google Sheets CSV URLs")]
    public string abilitiesCsvUrl = "https://docs.google.com/spreadsheets/d/19-xHKD4eLu4m3hMph4hMttR52oEThk9O-pj12wp98_M/gviz/tq?tqx=out:csv&sheet=Abilities";
    public string playerDeckCsvUrl = "https://docs.google.com/spreadsheets/d/19-xHKD4eLu4m3hMph4hMttR52oEThk9O-pj12wp98_M/gviz/tq?tqx=out:csv&sheet=PlayerDeck";

    private Dictionary<string, CardData> abilitiesDict = new Dictionary<string, CardData>();

    void Awake()
    {
        // CRITICAL FIX: Ensure the prefab is not active in the scene
        if (cardPrefab) 
        {
            // Find any instances of the Card prefab in the scene hierarchy and disable them
            GameObject originalCardInScene = GameObject.Find("Card");
            if (originalCardInScene != null)
            {
                Debug.Log("[CardGenerator] Found original Card prefab in scene - disabling it");
                originalCardInScene.SetActive(false);
                
                // DO NOT set scale to zero - this causes issues with instantiated clones
                // Just disable the GameObject instead
                Debug.Log("[CardGenerator] Disabled original Card prefab in scene");
            }
            
            // Also disable the prefab reference without changing its scale
            cardPrefab.SetActive(false);
            
            // Ensure it's really disabled by setting its parent's active state too if it has one
            if (cardPrefab.transform.parent != null)
            {
                // Check if this is the prefab in the hierarchy (not the asset)
                if (cardPrefab.transform.parent.gameObject.scene.isLoaded)
                {
                    Debug.Log("[CardGenerator] Found Card prefab in hierarchy - disabling its parent too");
                    cardPrefab.transform.parent.gameObject.SetActive(false);
                }
            }
            
            Debug.Log("[CardGenerator] Disabled card prefab reference without changing scale");
        }
        
        // Start downloading and parsing the CSVs
        StartCoroutine(DownloadAbilities());
    }
    
    void Start()
    {
        // CRITICAL FIX: Reset the prefab's RectTransform scale and position
        if (cardPrefab != null)
        {
            // Fix the prefab's transform
            cardPrefab.transform.localScale = Vector3.one;
            cardPrefab.transform.localPosition = new Vector3(0, 0, 0);
            
            // Fix the prefab's RectTransform
            RectTransform rectTransform = cardPrefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = Vector2.zero;
                Debug.Log($"[CardGenerator] CRITICAL FIX: Reset prefab RectTransform scale to {rectTransform.localScale} and position to {rectTransform.anchoredPosition}");
            }
            
            // Disable the prefab
            cardPrefab.SetActive(false);
            Debug.Log("[CardGenerator] Disabled card prefab in Start");
        }
        
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
    // Re-enable the prefab before instantiation (it's disabled in Awake)
    cardPrefab.SetActive(true);
    
    // CRITICAL FIX: Reset prefab scale and position to ensure clones start with correct values
    cardPrefab.transform.localScale = Vector3.one;
    cardPrefab.transform.localPosition = new Vector3(0, 0, 0);
    
    // Fix the prefab's RectTransform
    RectTransform prefabRT = cardPrefab.GetComponent<RectTransform>();
    if (prefabRT != null)
    {
        prefabRT.localScale = Vector3.one;
        prefabRT.anchoredPosition = Vector2.zero;
        Debug.Log($"[CardGenerator] Reset prefab RectTransform scale to {prefabRT.localScale} and position to {prefabRT.anchoredPosition} before instantiation");
    }
    
    // SUPER CRITICAL DEBUG: Log prefab state right before instantiation
    Debug.Log($"[CardGenerator] PREFAB STATE RIGHT BEFORE INSTANTIATION: active={cardPrefab.activeSelf}, transform.scale={cardPrefab.transform.localScale}, RectTransform.scale={cardPrefab.GetComponent<RectTransform>()?.localScale}, prefab.name={cardPrefab.name}");
    
    // Instantiate cards directly under CardCanvas for proper UI rendering
    var cardObj = Instantiate(cardPrefab, cardCanvasParent != null ? cardCanvasParent : cardDropZoneParent);
    
    // SUPER CRITICAL DEBUG: Log clone state immediately after instantiation BEFORE any changes
    Debug.Log($"[CardGenerator] CLONE STATE IMMEDIATELY AFTER INSTANTIATION (BEFORE CHANGES): active={cardObj.activeSelf}, transform.scale={cardObj.transform.localScale}, RectTransform.scale={cardObj.GetComponent<RectTransform>()?.localScale}, clone.name={cardObj.name}");
    
    // Disable the prefab again after instantiation WITHOUT changing its scale
    cardPrefab.SetActive(false);
    // DO NOT set scale to zero here - it causes issues with clones
    
    // Ensure the instantiated card is active and visible
    cardObj.SetActive(true);
    
    // CRITICAL: Force scale to (1,1,1) immediately after instantiation
    cardObj.transform.localScale = Vector3.one;
    
    // Force RectTransform scale to (1,1,1) as well
    RectTransform cardRT = cardObj.GetComponent<RectTransform>();
    if (cardRT != null)
    {
        cardRT.localScale = Vector3.one;
        Debug.Log($"[CardGenerator] Explicitly set card {card.title} RectTransform scale to {cardRT.localScale}");
    }
    
    Debug.Log($"[CardGenerator] Explicitly set card {card.title} transform scale to {cardObj.transform.localScale} immediately after instantiation");
    
    // Set a proper position in front of other cards
    RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
    if (rectTransform != null)
    {
        // Reset RectTransform scale explicitly
        rectTransform.localScale = Vector3.one;
        Debug.Log($"[CardGenerator] Explicitly set RectTransform scale to {rectTransform.localScale}");
        
        // Reset any negative position values
        rectTransform.anchoredPosition = new Vector2(50, 0);
        
        // Ensure card is visible in World Space by setting proper Z position
        if (cardCanvasParent != null && cardCanvasParent.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            // Position slightly in front of parent
            Vector3 localPos = cardObj.transform.localPosition;
            localPos.z = -0.01f * instantiatedCards.Count; // Each card slightly in front of previous (smaller offset)
            cardObj.transform.localPosition = localPos;
            Debug.Log($"[CardGenerator] Set card {card.title} Z position to {localPos.z} for World Space visibility");
        }
        else
        {
            // For UI cards, ensure Z is slightly positive to be visible
            Vector3 localPos = cardObj.transform.localPosition;
            localPos.z = 0;
            cardObj.transform.localPosition = localPos;
            Debug.Log($"[CardGenerator] Set card {card.title} Z position to {localPos.z} for UI visibility");
        }
        
        // Force update the RectTransform
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
    
    instantiatedCards.Add(cardObj);
    
    // Debug card properties
    Debug.Log($"[CardGenerator] Card {card.title} instantiated under {(cardCanvasParent != null ? "CardCanvas" : "CardDropZone")}");
    Debug.Log($"[CardGenerator] Card {card.title} position: {cardObj.transform.position}, localPosition: {cardObj.transform.localPosition}, scale: {cardObj.transform.localScale}");
    Debug.Log($"[CardGenerator] Card {card.title} RectTransform - anchoredPosition: {cardObj.GetComponent<RectTransform>().anchoredPosition}, sizeDelta: {cardObj.GetComponent<RectTransform>().sizeDelta}, pivot: {cardObj.GetComponent<RectTransform>().pivot}");
    
    // Check if card has required components
    var image = cardObj.GetComponentInChildren<UnityEngine.UI.Image>();
    Debug.Log($"[CardGenerator] Card {card.title} has Image component: {image != null}, Enabled: {image?.enabled}, Color: {image?.color}");
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
            // Force all cards to be visible with correct scale before layout
            foreach (var card in instantiatedCards)
            {
                if (card != null)
                {
                    card.transform.localScale = Vector3.one;
                    card.SetActive(true);
                    
                    // Ensure RectTransform is properly scaled
                    RectTransform rt = card.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = Vector3.one;
                    }
                    
                    Debug.Log($"[CardGenerator] Pre-layout card scale: {card.transform.localScale}");
                }
            }
            
            // Now layout the cards
            layout.LayoutCards();
            
            // Log post-layout scales
            StartCoroutine(LogPostLayoutScales(instantiatedCards));
        }
    }

    // Coroutine to log card scales after layout is complete
    private IEnumerator LogPostLayoutScales(List<GameObject> cards)
    {
        // Wait for layout to complete
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[CardGenerator] Checking post-layout card scales:");
        foreach (var card in cards)
        {
            if (card != null)
            {
                Debug.Log($"[CardGenerator] Post-layout card scale: {card.transform.localScale}, active: {card.activeSelf}");
                
                // Check RectTransform scale
                RectTransform rt = card.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Debug.Log($"[CardGenerator] Post-layout RectTransform scale: {rt.localScale}, anchoredPosition: {rt.anchoredPosition}");
                    
                    // If scale is zero, force reset it
                    if (rt.localScale == Vector3.zero)
                    {
                        rt.localScale = Vector3.one;
                        Debug.Log($"[CardGenerator] FIXED zero scale on RectTransform");
                    }
                }
                
                // Check for any Canvas or CanvasGroup components that might affect visibility
                Canvas canvas = card.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"[CardGenerator] Card has Canvas: enabled={canvas.enabled}, sortingOrder={canvas.sortingOrder}");
                }
                
                CanvasGroup canvasGroup = card.GetComponentInChildren<CanvasGroup>();
                if (canvasGroup != null)
                {
                    Debug.Log($"[CardGenerator] Card has CanvasGroup: alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}, blocksRaycasts={canvasGroup.blocksRaycasts}");
                    
                    // Ensure CanvasGroup is not making the card invisible
                    if (canvasGroup.alpha < 1.0f)
                    {
                        canvasGroup.alpha = 1.0f;
                        Debug.Log("[CardGenerator] FIXED low alpha on CanvasGroup");
                    }
                }
            }
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
