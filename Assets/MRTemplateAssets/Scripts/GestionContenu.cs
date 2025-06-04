using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DynamicButtonCreator : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform parentTransform;
    public float spacing = 10f;

    [System.Serializable]
    private class ExperimentData
    {
        public string description;
        public string duration;
    }

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
        if (parentTransform == null) return;

        var layoutGroup = parentTransform.GetComponent<VerticalLayoutGroup>() ?? parentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        var sizeFitter = parentTransform.GetComponent<ContentSizeFitter>() ?? parentTransform.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
        if (buttonPrefab == null || parentTransform == null) return;

        for (int i = 0; i < experiments.Count; i++)
        {
            var newButton = Instantiate(buttonPrefab, parentTransform);

            var leftText = newButton.transform.Find("LeftText")?.GetComponent<Text>();
            var rightText = newButton.transform.Find("RightText")?.GetComponent<Text>();

            if (leftText != null) leftText.text = experiments[i].description;
            if (rightText != null) rightText.text = experiments[i].duration;

            var buttonComponent = newButton.GetComponent<Button>();
            int index = i;
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => OnButtonClick(index));
            }
        }
    }

    void OnButtonClick(int index)
    {
        Debug.Log($"Bouton cliqué : {experiments[index].description} ({experiments[index].duration})");
    }
}
