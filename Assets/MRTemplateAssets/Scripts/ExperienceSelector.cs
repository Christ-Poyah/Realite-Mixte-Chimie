using UnityEngine;
using TMPro;

public class ExperienceSelector : MonoBehaviour
{
    [TextArea]
    public string descriptionText;  // Texte à afficher quand on clique
    
    public TextMeshProUGUI descriptionTarget; // Zone de texte à modifier

    void Awake()
    {
        if (descriptionTarget == null)
        {
            Debug.LogWarning($"[{nameof(ExperienceSelector)}] descriptionTarget non assigné sur {gameObject.name}.");
        }
    }

    public void ShowDescription(string desciription)
    {
        if (descriptionTarget != null)
        {
            descriptionTarget.text = desciription;
        }
        else
        {
            Debug.LogWarning($"[{nameof(ExperienceSelector)}] descriptionTarget est null.");
        }
    }
}
