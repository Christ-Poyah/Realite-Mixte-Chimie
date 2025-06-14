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
    public TextMeshProUGUI backButtonText; // NOUVEAU : Référence vers le bouton retour

    [Header("Search")]
    public TMP_InputField searchInputField; // barre de recherche.

    [Header("Help UI")]
    public Button helpButton;            // le bouton Aide
    public GameObject helpPanel;  

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

    // Événements modifiés avec paramètre string pour le titre
    public System.Action<int, string> OnNiveauCategorySelected;
    public System.Action<int, string> OnTypeCategorySelected;

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

          if (helpPanel != null)
        helpPanel.SetActive(false);

        // 2) Abonne le bouton Aide
        if (helpButton != null)
        {
            helpButton.onClick.RemoveAllListeners();
            helpButton.onClick.AddListener(OnHelpButtonClicked);
        }

        ValidateAndFixParentSetup();
        currentDisplayMode = initialDisplayMode;
        
        // Attendre que le layout soit prêt avant de créer les boutons
        StartCoroutine(InitializeAfterLayout());
    }

    private void OnHelpButtonClicked()
{
    if (helpPanel == null) return;
    // Toggle (affiche si caché, cache si affiché)
    bool isActive = helpPanel.activeSelf;
    helpPanel.SetActive(!isActive);
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
        
        if (searchInputField != null)
        {
            // Dès qu’on tape, on appelle OnSearchTextChanged
            searchInputField.onValueChanged.RemoveAllListeners();
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }

        // Forcer un recalcul après l'initialisation complète
        yield return new WaitForSeconds(0.1f);
        ForceCompleteRecalculation();

    }

    /// <summary>
/// Filtre les catégories affichées selon le texte tapé.
/// </summary>
private void OnSearchTextChanged(string search)
{
    // Si la recherche est vide, on recharge tout normalement
    if (string.IsNullOrWhiteSpace(search))
    {
        LoadAndCreateCategoryButtons();
        return;
    }

    // Sinon on filtre selon le mode courant
    string lower = search.Trim().ToLowerInvariant();
    ClearExistingButtons();

    switch (currentDisplayMode)
    {
        case CategoryDisplayMode.NiveauOnly:
            foreach (var cat in allNiveauCategories
                     .Where(c => c.TitreCategNiv != null &&
                                 c.TitreCategNiv.ToLowerInvariant().Contains(lower)))
            {
                CreateNiveauCategoryButton(cat);
            }
            break;

        case CategoryDisplayMode.TypeOnly:
            foreach (var cat in allTypeCategories
                     .Where(c => c.TitreCategTyp != null &&
                                 c.TitreCategTyp.ToLowerInvariant().Contains(lower)))
            {
                CreateTypeCategoryButton(cat);
            }
            break;
    }

    // On force le layout
    StartCoroutine(ForceLayoutRefresh());
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

    // MÉTHODES MODIFIÉES : Incluent maintenant la mise à jour du bouton retour
    private void OnNiveauButtonClick(int categoryId, GameObject button)
    {
        UpdateSelectedButton(button);
        selectedNiveauId = categoryId;
        selectedTypeId = -1;

        // Récupérer le titre de la catégorie
        string categoryTitle = GetCategoryTitle(categoryId, true); // true pour niveau
        
        // NOUVEAU : Mettre à jour le bouton retour
        UpdateBackButton(categoryTitle);
        
        OnNiveauCategorySelected?.Invoke(categoryId, categoryTitle);
    }

    private void OnTypeButtonClick(int categoryId, GameObject button)
    {
        UpdateSelectedButton(button);
        selectedTypeId = categoryId;
        selectedNiveauId = -1;

        // Récupérer le titre de la catégorie
        string categoryTitle = GetCategoryTitle(categoryId, false); // false pour type
        
        // NOUVEAU : Mettre à jour le bouton retour
        UpdateBackButton(categoryTitle);
        
        OnTypeCategorySelected?.Invoke(categoryId, categoryTitle);
    }

    // NOUVELLE MÉTHODE : Met à jour le texte du bouton retour
    private void UpdateBackButton(string categoryTitle)
    {
        if (backButtonText != null)
        {
            backButtonText.text = categoryTitle;
        }
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

    // Nouvelle méthode pour récupérer le titre d'une catégorie
    private string GetCategoryTitle(int categoryId, bool isNiveau)
    {
        if (isNiveau && allNiveauCategories != null)
        {
            var category = allNiveauCategories.FirstOrDefault(c => c.IdCategNiv == categoryId);
            return category?.TitreCategNiv ?? $"Niveau {categoryId}";
        }
        else if (!isNiveau && allTypeCategories != null)
        {
            var category = allTypeCategories.FirstOrDefault(c => c.IdCategTyp == categoryId);
            return category?.TitreCategTyp ?? $"Type {categoryId}";
        }
        
        return $"Catégorie {categoryId}";
    }

    // Nouvelle méthode publique pour récupérer le titre de la catégorie sélectionnée
    public string GetSelectedCategoryTitle()
    {
        if (selectedNiveauId != -1)
        {
            return GetCategoryTitle(selectedNiveauId, true);
        }
        else if (selectedTypeId != -1)
        {
            return GetCategoryTitle(selectedTypeId, false);
        }
        return "";
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