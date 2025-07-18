using UnityEngine;

public enum Affinity
{
    Blue,
    Purple,
    Red
}

public static class AffinityColor
{
    public static Color GetColor(Affinity affinity)
    {
        switch (affinity)
        {
            case Affinity.Blue:
                return new Color(0.2f, 0.4f, 1f); // Example blue
            case Affinity.Purple:
                return new Color(0.6f, 0.2f, 0.8f); // Example purple
            case Affinity.Red:
                return new Color(1f, 0.2f, 0.2f); // Example red
            default:
                return Color.gray;
        }
    }

    public static Affinity FromString(string name)
    {
        switch (name.ToLower())
        {
            case "blue": return Affinity.Blue;
            case "purple": return Affinity.Purple;
            case "red": return Affinity.Red;
            default: return Affinity.Blue;
        }
    }
}
