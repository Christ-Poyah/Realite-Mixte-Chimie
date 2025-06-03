using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceItem : MonoBehaviour
{
    public Image background;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI timeText;

    public Color selectedBgColor = new Color(0f, 0.75f, 1f, 1f); // Bleu clair
    public Color normalBgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f); // Gris foncé

    public Color selectedTextColor = new Color(0f, 0.45f, 0.8f, 1f); // Bleu foncé
    public Color normalTextColor = Color.white;

    private static ExperienceItem currentlySelected;

    public void OnClick()
    {
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.Deselect();
        }

        currentlySelected = this;
        background.color = selectedBgColor;
        titleText.color = selectedTextColor;
        timeText.color = selectedTextColor;
    }

    public void Deselect()
    {
        background.color = normalBgColor;
        titleText.color = normalTextColor;
        timeText.color = normalTextColor;
    }
}
