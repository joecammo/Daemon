using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DaemonAffinityManager : MonoBehaviour
{
    public static DaemonAffinityManager Instance;
    private Dictionary<string, Affinity> daemonAffinityDict = new Dictionary<string, Affinity>();
    public string daemonsCsvUrl = "https://docs.google.com/spreadsheets/d/19-xHKD4eLu4m3hMph4hMttR52oEThk9O-pj12wp98_M/gviz/tq?tqx=out:csv&sheet=Daemons";
    public bool IsLoaded { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void LoadDaemonsAffinity(System.Action onLoaded = null)
    {
        StartCoroutine(DownloadAndParseDaemonCsv(onLoaded));
    }

    private IEnumerator DownloadAndParseDaemonCsv(System.Action onLoaded)
    {
        UnityWebRequest www = UnityWebRequest.Get(daemonsCsvUrl);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download Daemons CSV: " + www.error);
            yield break;
        }
        ParseDaemonCsv(www.downloadHandler.text);
        IsLoaded = true;
        onLoaded?.Invoke();
    }

    // Minimal CSV parser that handles quoted fields and commas inside fields
    private List<string[]> ParseCsv(string csv)
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

    private void ParseDaemonCsv(string csv)
    {
        var rows = ParseCsv(csv);
        if (rows.Count < 2) return;
        var header = rows[0];
        int nameIdx = System.Array.IndexOf(header, "Name");
        int affinityIdx = System.Array.IndexOf(header, "Affinity");
        if (nameIdx < 0 || affinityIdx < 0)
        {
            Debug.LogError("[DaemonAffinityManager] Could not find 'Name' or 'Affinity' columns in Daemons CSV header.");
            return;
        }
        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length <= Mathf.Max(nameIdx, affinityIdx))
            {
                Debug.LogWarning($"[DaemonAffinityManager] Skipping malformed row {i+1}: {string.Join(",", row)}");
                continue;
            }
            string daemonName = row[nameIdx].Trim();
            string affinityStr = row[affinityIdx].Trim();
            Affinity affinity = AffinityColor.FromString(affinityStr);
            Debug.Log($"[DaemonAffinityManager] Loaded Daemon: {daemonName}, Affinity: {affinity}");
            if (!daemonAffinityDict.ContainsKey(daemonName))
                daemonAffinityDict.Add(daemonName, affinity);
        }
    }

    public bool TryGetAffinity(string daemonName, out Affinity affinity)
    {
        return daemonAffinityDict.TryGetValue(daemonName, out affinity);
    }

    public Affinity GetAffinityOrDefault(string daemonName, Affinity defaultAffinity = Affinity.Blue)
    {
        if (daemonAffinityDict.TryGetValue(daemonName, out var aff))
            return aff;
        return defaultAffinity;
    }
}
