using UnityEngine;
using UnityEngine.UI;
using TMPro; // Ajout de l'espace de nom pour TextMeshPro
using System.Collections.Generic;

public class DynamicButtonCreator : MonoBehaviour
{
    public GameObject buttonPrefab; // Préfab du bouton avec TextMeshPro
    public Transform parentTransform; // Parent pour les boutons (ex : ScrollView Content)
    public float spacing = 10f; // Espacement entre les boutons

    [System.Serializable]
    private class ExperimentData
    {
        public string description; // Description de l'expérience
        public string duration; // Durée de l'expérience
    }

    // Liste des expériences chimiques (conforme au cahier des charges)
    private readonly List<ExperimentData> experiments = new List<ExperimentData>
    {
        new ExperimentData { description = "Expérience avec les alcanes", duration = "10 min" },
        new ExperimentData { description = "Réaction des alcools", duration = "15 min" },
        new ExperimentData { description = "Synthèse des esters", duration = "20 min" },
        new ExperimentData { description = "Oxydation des aldéhydes", duration = "12 min" },
        new ExperimentData { description = "Polymérisation", duration = "25 min" }
    };

    void Start()
    {
        SetupScrollViewContent();
        CreateButtons();
    }

    void SetupScrollViewContent()
    {
        if (parentTransform == null)
        {
            Debug.LogError("ParentTransform non assigné !");
            return;
        }

        // Configuration du VerticalLayoutGroup
        var layoutGroup = parentTransform.GetComponent<VerticalLayoutGroup>() ?? parentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // Configuration du ContentSizeFitter
        var sizeFitter = parentTransform.GetComponent<ContentSizeFitter>() ?? parentTransform.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Configuration du RectTransform
        var contentRect = parentTransform.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, contentRect.sizeDelta.y);
        }
    }

    void CreateButtons()
    {
        if (buttonPrefab == null || parentTransform == null)
        {
            Debug.LogError("ButtonPrefab ou ParentTransform non assigné !");
            return;
        }

        for (int i = 0; i < experiments.Count; i++)
        {
            // Instanciation du bouton
            var newButton = Instantiate(buttonPrefab, parentTransform);

            // Recherche des composants TextMeshProUGUI
            var leftText = newButton.transform.Find("LeftText")?.GetComponent<TextMeshProUGUI>();
            var rightText = newButton.transform.Find("RightText")?.GetComponent<TextMeshProUGUI>();

            // Mise à jour des textes
            if (leftText != null) leftText.text = experiments[i].description;
            else Debug.LogWarning($"LeftText (TextMeshProUGUI) non trouvé sur le bouton {i}");

            if (rightText != null) rightText.text = experiments[i].duration;
            else Debug.LogWarning($"RightText (TextMeshProUGUI) non trouvé sur le bouton {i}");

            // Configuration du clic sur le bouton
            var buttonComponent = newButton.GetComponent<Button>();
            int index = i;
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => OnButtonClick(index));
            }
            else
            {
                Debug.LogWarning($"Composant Button non trouvé sur le bouton {i}");
            }
        }
    }

    void OnButtonClick(int index)
    {
        Debug.Log($"Bouton cliqué : {experiments[index].description} ({experiments[index].duration})");
        // Ajouter ici la logique pour lancer l'expérience sélectionnée (par exemple, charger une scène Unity spécifique)
    }
}