using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class DynamicCategoryCreator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject categoryButtonPrefab;
    public Transform categoriesParent;
    public TMPro.TMP_Dropdown displayModeDropdown;

    [Header("Category Display Settings")]
    public bool showNiveauCategories = true;
    public bool showTypeCategories = true;
    [SerializeField] private CategoryDisplayMode initialDisplayMode = CategoryDisplayMode.NiveauOnly;

    [Header("Layout Settings")]
    public int columnsCount = 4;
    public float spacing = 10f;
    public float padding = 15f;

    [Header("Visual Settings")]
    public Color niveauCategoryColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color typeCategoryColor = new Color(1f, 0.6f, 0.2f, 1f);
    public Color selectedCategoryColor = new Color(0.1f, 0.8f, 0.3f, 1f);

    [Header("DatabaseManager Reference")]
    [SerializeField] private DatabaseManager databaseManagerReference;

    private DataService dataService;
    private List<CategorieNiveau> allNiveauCategories;
    private List<CategorieType> allTypeCategories;
    private CategoryDisplayMode currentDisplayMode;

    public System.Action<int> OnNiveauCategorySelected;
    public System.Action<int> OnTypeCategorySelected;

    private int selectedNiveauId = -1;
    private int selectedTypeId = -1;
    private GameObject selectedButton = null;

    public enum CategoryDisplayMode
    {
        NiveauOnly,
        TypeOnly
    }

    void Start()
    {
        if (databaseManagerReference == null)
        {
            Debug.LogError("[DynamicCategoryCreator] DatabaseManager Reference is not assigned.");
            return;
        }

        dataService = databaseManagerReference.GetDataService();
        if (dataService == null)
        {
            Debug.LogError("[DynamicCategoryCreator] Failed to get DataService.");
            return;
        }

        LoadAllCategoriesData();

        if (!ValidateUIReferences())
        {
             Debug.LogError("[DynamicCategoryCreator] UI References validation failed.");
             return;
        }

        ValidateAndFixParentSetup();
        currentDisplayMode = initialDisplayMode;
        
        // Attendre que le layout soit prêt avant de créer les boutons
        StartCoroutine(InitializeAfterLayout());
    }

    private IEnumerator InitializeAfterLayout()
    {
        // Attendre plusieurs frames pour que tout soit stabilisé
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        SetupGridLayout();
        
        if (displayModeDropdown != null)
        {
            SetupDisplayModeDropdown();
        }
        else
        {
            LoadAndCreateCategoryButtons();
        }
        
        // Forcer un recalcul après l'initialisation complète
        yield return new WaitForSeconds(0.1f);
        ForceCompleteRecalculation();
    }

    void ValidateAndFixParentSetup()
    {
        if (categoriesParent == null) return;

        RectTransform parentRect = categoriesParent.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogError("[DynamicCategoryCreator] CategoriesParent must have a RectTransform component!");
            return;
        }

        if (!categoriesParent.gameObject.activeInHierarchy)
        {
            categoriesParent.gameObject.SetActive(true);
        }
    }

    void SetupGridLayout()
    {
        if (categoriesParent == null) return;

        var existingSizeFitter = categoriesParent.GetComponent<ContentSizeFitter>();
        if (existingSizeFitter != null)
        {
            if (Application.isPlaying)
                Destroy(existingSizeFitter);
            else
                DestroyImmediate(existingSizeFitter);
        }

        var gridLayout = categoriesParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = categoriesParent.gameObject.AddComponent<GridLayoutGroup>();
        }

        Vector2 calculatedCellSize = CalculateOptimalCellSize();
        
        gridLayout.cellSize = calculatedCellSize;
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnsCount;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);

        var rectTransform = categoriesParent.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = new Vector2(0, 0);
            
            if (rectTransform.sizeDelta.x <= 0)
            {
                rectTransform.sizeDelta = new Vector2(800f, rectTransform.sizeDelta.y);
            }
        }
    }

    private Vector2 CalculateOptimalCellSize()
    {
        if (categoriesParent == null) return new Vector2(200f, 80f);

        RectTransform parentRect = categoriesParent.GetComponent<RectTransform>();
        if (parentRect == null) return new Vector2(200f, 80f);

        float availableWidth = 0f;
        
        if (parentRect.rect.width > 0)
        {
            availableWidth = parentRect.rect.width;
        }
        else if (parentRect.sizeDelta.x > 0)
        {
            availableWidth = parentRect.sizeDelta.x;
        }
        else if (parentRect.parent != null)
        {
            RectTransform grandParent = parentRect.parent.GetComponent<RectTransform>();
            if (grandParent != null && grandParent.rect.width > 0)
            {
                availableWidth = grandParent.rect.width;
            }
        }
        
        if (availableWidth <= 0)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null && canvasRect.rect.width > 0)
                {
                    availableWidth = canvasRect.rect.width * 0.8f;
                }
            }
        }
        
        if (availableWidth <= 0)
        {
            availableWidth = 800f;
        }

        // Soustraire le padding des deux côtés
        float usableWidth = availableWidth - (padding * 2);
        float totalSpacing = spacing * (columnsCount - 1);
        float cellWidth = (usableWidth - totalSpacing) / columnsCount;
        
        cellWidth = Mathf.Clamp(cellWidth, 120f, 300f);
        float cellHeight = cellWidth * 0.35f;
        cellHeight = Mathf.Clamp(cellHeight, 50f, 120f);
        
        return new Vector2(cellWidth, cellHeight);
    }

    void LoadAllCategoriesData()
    {
         try
        {
            if (showNiveauCategories && dataService.GetAllCategoriesNiveau() != null)
            {
                allNiveauCategories = dataService.GetAllCategoriesNiveau().ToList();
            } else {
                 allNiveauCategories = new List<CategorieNiveau>();
            }

            if (showTypeCategories && dataService.GetAllCategoriesType() != null)
            {
                allTypeCategories = dataService.GetAllCategoriesType().ToList();
            } else {
                 allTypeCategories = new List<CategorieType>();
            }
        }
         catch (System.Exception e)
        {
            Debug.LogError($"[DynamicCategoryCreator] Error loading categories: {e.Message}");
             allNiveauCategories = new List<CategorieNiveau>();
             allTypeCategories = new List<CategorieType>();
        }
    }

    void SetupDisplayModeDropdown()
    {
        if (displayModeDropdown == null) return;

        displayModeDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            "Niveaux",
            "Types"
        };

        displayModeDropdown.AddOptions(options);
        displayModeDropdown.onValueChanged.RemoveAllListeners();
        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeDropdownChanged);

        int initialIndex = initialDisplayMode == CategoryDisplayMode.TypeOnly ? 1 : 0;
        displayModeDropdown.value = initialIndex;
        
        LoadAndCreateCategoryButtons();
    }

    private void ForceCompleteRecalculation()
    {
        if (categoriesParent == null) return;
        
        // Recalculer et appliquer le layout
        SetupGridLayout();
        
        // Si on a des boutons, les recréer avec les bonnes dimensions
        if (categoriesParent.childCount > 0)
        {
            LoadAndCreateCategoryButtons();
        }
    }

    private void OnDisplayModeDropdownChanged(int index)
    {
        if (displayModeDropdown == null || index < 0 || index >= displayModeDropdown.options.Count)
        {
            return;
        }

        string selectedText = displayModeDropdown.options[index].text;
        CategoryDisplayMode newMode = selectedText == "Types" ? CategoryDisplayMode.TypeOnly : CategoryDisplayMode.NiveauOnly;

        SetDisplayMode(newMode);
    }

    void LoadAndCreateCategoryButtons()
    {
        if (dataService == null || !ValidateUIReferences()) return;

        ClearExistingButtons();
        
        // Forcer une mise à jour du layout AVANT de créer les boutons
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(categoriesParent.GetComponent<RectTransform>());
        
        UpdateGridLayoutCellSize();

        switch (currentDisplayMode)
        {
            case CategoryDisplayMode.NiveauOnly:
                if (showNiveauCategories && allNiveauCategories != null)
                {
                    foreach (var category in allNiveauCategories)
                    {
                        CreateNiveauCategoryButton(category);
                    }
                }
                break;

            case CategoryDisplayMode.TypeOnly:
                if (showTypeCategories && allTypeCategories != null)
                {
                    foreach (var category in allTypeCategories)
                    {
                        CreateTypeCategoryButton(category);
                    }
                }
                break;
        }

        StartCoroutine(ForceLayoutRefresh());
    }

    private void UpdateGridLayoutCellSize()
    {
        if (categoriesParent == null) return;

        var gridLayout = categoriesParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            Vector2 newCellSize = CalculateOptimalCellSize();
            gridLayout.cellSize = newCellSize;
        }
    }

    private void CreateNiveauCategoryButton(CategorieNiveau category)
    {
        if (category == null || !ValidateUIReferences()) return;

        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"NiveauButton_{category.IdCategNiv}";
        button.SetActive(true);

        SetButtonText(button, category.TitreCategNiv ?? $"Niveau {category.IdCategNiv}");
        SetButtonBaseColor(button, niveauCategoryColor);

        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            int categoryId = category.IdCategNiv;
            buttonComp.onClick.AddListener(() => OnNiveauButtonClick(categoryId, button));
        }
    }

    private void CreateTypeCategoryButton(CategorieType category)
    {
        if (category == null || !ValidateUIReferences()) return;

        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"TypeButton_{category.IdCategTyp}";
        button.SetActive(true);

        SetButtonText(button, category.TitreCategTyp ?? $"Type {category.IdCategTyp}");
        SetButtonBaseColor(button, typeCategoryColor);

        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            int categoryId = category.IdCategTyp;
            buttonComp.onClick.AddListener(() => OnTypeButtonClick(categoryId, button));
        }
    }

    private void SetButtonBaseColor(GameObject button, Color color)
    {
        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            var colors = buttonComp.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = selectedCategoryColor;
            buttonComp.colors = colors;
        }
    }

    private void SetButtonText(GameObject button, string text)
    {
        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            tmpText.fontSize = Mathf.Min(tmpText.fontSize, 14f);
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMin = 8f;
            tmpText.fontSizeMax = 14f;
            return;
        }

        var legacyText = button.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
            legacyText.fontSize = Mathf.Min(legacyText.fontSize, 14);
        }
    }

    void ClearExistingButtons()
    {
        if (categoriesParent == null) return;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in categoriesParent)
        {
             childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
        {
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        selectedNiveauId = -1;
        selectedTypeId = -1;
        selectedButton = null;
    }

    private IEnumerator ForceLayoutRefresh()
    {
        yield return null;
        yield return null;

        if (categoriesParent != null)
        {
            Canvas.ForceUpdateCanvases();
            
            var rectTransform = categoriesParent.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                
                if (rectTransform.parent != null)
                {
                    var parentRectTransform = rectTransform.parent.GetComponent<RectTransform>();
                    if (parentRectTransform != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
                    }
                }
            }
        }
    }

    bool ValidateUIReferences()
    {
        return categoryButtonPrefab != null && categoriesParent != null;
    }

    private void OnNiveauButtonClick(int categoryId, GameObject button)
    {
        UpdateSelectedButton(button);
        selectedNiveauId = categoryId;
        selectedTypeId = -1;

        OnNiveauCategorySelected?.Invoke(categoryId);
    }

    private void OnTypeButtonClick(int categoryId, GameObject button)
    {
        UpdateSelectedButton(button);
        selectedTypeId = categoryId;
        selectedNiveauId = -1;

        OnTypeCategorySelected?.Invoke(categoryId);
    }

    private void UpdateSelectedButton(GameObject newSelectedButton)
    {
        if (selectedButton != null && selectedButton != newSelectedButton)
        {
            RestoreButtonColor(selectedButton);
        }

        if (selectedButton != newSelectedButton)
        {
             selectedButton = newSelectedButton;
              if (selectedButton != null)
              {
                  ApplySelectedColor(selectedButton);
              }
        }
    }

    private void ApplySelectedColor(GameObject button)
    {
         var buttonComp = button.GetComponent<Button>();
         if (buttonComp != null)
         {
             var colors = buttonComp.colors;
             colors.normalColor = selectedCategoryColor;
             colors.highlightedColor = Color.Lerp(selectedCategoryColor, Color.white, 0.2f);
             colors.pressedColor = Color.Lerp(selectedCategoryColor, Color.black, 0.2f);
             buttonComp.colors = colors;
         }
    }

    private void RestoreButtonColor(GameObject button)
    {
        if (button == null) return;

        Color originalColor = button.name.StartsWith("NiveauButton_") ? niveauCategoryColor : typeCategoryColor;
        SetButtonBaseColor(button, originalColor);
    }

    public void RefreshCategories()
    {
        if (dataService == null) return;
        LoadAndCreateCategoryButtons();
    }

    public void SetDisplayMode(CategoryDisplayMode mode)
    {
        if (currentDisplayMode == mode) return;
        
        currentDisplayMode = mode;
        RefreshCategories();
    }

    public void SetColumnsCount(int columns)
    {
        columnsCount = Mathf.Max(1, columns);
        SetupGridLayout();
        StartCoroutine(ForceLayoutRefresh());
    }

    public void ClearSelection()
    {
        if (selectedButton != null)
        {
            RestoreButtonColor(selectedButton);
            selectedButton = null;
        }
        
        selectedNiveauId = -1;
        selectedTypeId = -1;
    }

    

    public int GetSelectedNiveauId() => selectedNiveauId;
    public int GetSelectedTypeId() => selectedTypeId;
    public bool IsNoCategorySelected() => selectedNiveauId == -1 && selectedTypeId == -1;
}