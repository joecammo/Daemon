using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EnergyRefillButton : MonoBehaviour
{
    private Button refillButton;
    
    void Start()
    {
        refillButton = GetComponent<Button>();
        refillButton.onClick.AddListener(RefillEnergy);
    }
    
    void RefillEnergy()
    {
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.RefillEnergy();
            Debug.Log("Energy refilled!");
        }
        else
        {
            Debug.LogError("No EnergyManager instance found!");
        }
    }
}
