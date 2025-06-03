using UnityEngine;
using TMPro;

public class ExperienceSelector : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;

    [TextArea]
    public string[] descriptions;

    public void SelectExperience(int index)
    {
        if (index >= 0 && index < descriptions.Length)
        {
            descriptionText.text = descriptions[index];
        }
    }
}
