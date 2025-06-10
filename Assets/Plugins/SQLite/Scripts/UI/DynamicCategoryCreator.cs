using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;

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
    // Suppression de cellSize - sera calculé automatiquement

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
            Debug.LogError("[DynamicCategoryCreator] DatabaseManager Reference is not assigned. Cannot initialize.");
            return;
        }

        dataService = databaseManagerReference.GetDataService();

        if (dataService == null)
        {
            Debug.LogError("[DynamicCategoryCreator] Failed to get DataService. Database might not be ready or DataService is null.");
            return;
        }

        Debug.Log("[DynamicCategoryCreator] Initialized with DataService.");

        LoadAllCategoriesData();

        if (!ValidateUIReferences())
        {
             Debug.LogError("[DynamicCategoryCreator] Initialization failed: UI References are missing or invalid.");
             return;
        }

        ValidateAndFixParentSetup();
        SetupGridLayout();

        // CORRECTION 1: Initialiser currentDisplayMode AVANT de configurer le dropdown
        currentDisplayMode = initialDisplayMode;

        if (displayModeDropdown != null)
        {
            SetupDisplayModeDropdown();
        }
        else
        {
            Debug.LogWarning("[DynamicCategoryCreator] Display Mode Dropdown is not assigned. Using initialDisplayMode setting and initializing directly.");
            // CORRECTION 2: Charger les boutons immédiatement après l'initialisation
            LoadAndCreateCategoryButtons();
        }
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
            Debug.LogWarning("[DynamicCategoryCreator] CategoriesParent was inactive. Activating it.");
            categoriesParent.gameObject.SetActive(true);
        }

        if (parentRect.sizeDelta.x <= 0 || parentRect.sizeDelta.y <= 0)
        {
            Debug.LogWarning($"[DynamicCategoryCreator] Parent RectTransform has zero size: {parentRect.sizeDelta}. This may cause display issues.");
        }

        Debug.Log($"[DynamicCategoryCreator] Parent validation complete. Size: {parentRect.sizeDelta}, Active: {categoriesParent.gameObject.activeInHierarchy}");
    }

    void SetupGridLayout()
    {
        if (categoriesParent == null) return;

        var existingSizeFitter = categoriesParent.GetComponent<ContentSizeFitter>();
        if (existingSizeFitter != null)
        {
            Debug.Log("[DynamicCategoryCreator] Removing ContentSizeFitter to avoid conflicts with GridLayoutGroup.");
            if (Application.isPlaying)
                Destroy(existingSizeFitter);
            else
                DestroyImmediate(existingSizeFitter);
        }

        var gridLayout = categoriesParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = categoriesParent.gameObject.AddComponent<GridLayoutGroup>();
            Debug.Log("[DynamicCategoryCreator] Added GridLayoutGroup.");
        }

        // CORRECTION 3: Calcul automatique de la taille des cellules
        Vector2 calculatedCellSize = CalculateOptimalCellSize();
        
        gridLayout.cellSize = calculatedCellSize;
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnsCount;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

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

        Debug.Log($"[DynamicCategoryCreator] Grid layout setup complete with calculated cell size: {calculatedCellSize}");
    }

    // CORRECTION 4: Nouvelle méthode pour calculer la taille optimale des cellules (MISE À JOUR)
    private Vector2 CalculateOptimalCellSize()
    {
        if (categoriesParent == null) return new Vector2(200f, 80f);

        RectTransform parentRect = categoriesParent.GetComponent<RectTransform>();
        if (parentRect == null) return new Vector2(200f, 80f);

        float availableWidth = 0f;
        
        // Méthode 1: Utiliser rect.width si disponible
        if (parentRect.rect.width > 0)
        {
            availableWidth = parentRect.rect.width;
        }
        // Méthode 2: Utiliser sizeDelta.x si disponible
        else if (parentRect.sizeDelta.x > 0)
        {
            availableWidth = parentRect.sizeDelta.x;
        }
        // Méthode 3: Essayer de calculer depuis le parent
        else if (parentRect.parent != null)
        {
            RectTransform grandParent = parentRect.parent.GetComponent<RectTransform>();
            if (grandParent != null && grandParent.rect.width > 0)
            {
                availableWidth = grandParent.rect.width;
            }
        }
        
        // Méthode 4: Utiliser Canvas comme référence
        if (availableWidth <= 0)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null && canvasRect.rect.width > 0)
                {
                    availableWidth = canvasRect.rect.width * 0.8f; // 80% de la largeur du canvas
                }
            }
        }
        
        // Valeur par défaut si aucune méthode ne fonctionne
        if (availableWidth <= 0)
        {
            availableWidth = 800f;
        }

        // Calculer la largeur des cellules en tenant compte du spacing
        float totalSpacing = spacing * (columnsCount - 1);
        float cellWidth = (availableWidth - totalSpacing) / columnsCount;
        
        // S'assurer que la largeur n'est pas trop petite ou trop grande
        cellWidth = Mathf.Clamp(cellWidth, 120f, 300f);
        
        // Hauteur proportionnelle (ratio adaptatif)
        float cellHeight = cellWidth * 0.35f;
        cellHeight = Mathf.Clamp(cellHeight, 50f, 120f);

        Debug.Log($"[DynamicCategoryCreator] Calculated cell size: {cellWidth}x{cellHeight} (available width: {availableWidth})");
        
        return new Vector2(cellWidth, cellHeight);
    }

    void LoadAllCategoriesData()
    {
         try
        {
            if (showNiveauCategories && dataService.GetAllCategoriesNiveau() != null)
            {
                allNiveauCategories = dataService.GetAllCategoriesNiveau().ToList();
                Debug.Log($"[DynamicCategoryCreator] Loaded {allNiveauCategories.Count} total niveau categories from DB.");
            } else {
                 allNiveauCategories = new List<CategorieNiveau>();
                 if (!showNiveauCategories) Debug.Log($"[DynamicCategoryCreator] showNiveauCategories is false. Skipping loading niveau categories.");
                 else Debug.LogWarning($"[DynamicCategoryCreator] dataService.GetAllCategoriesNiveau() returned null.");
            }

            if (showTypeCategories && dataService.GetAllCategoriesType() != null)
            {
                allTypeCategories = dataService.GetAllCategoriesType().ToList();
                Debug.Log($"[DynamicCategoryCreator] Loaded {allTypeCategories.Count} total type categories from DB.");
            } else {
                 allTypeCategories = new List<CategorieType>();
                 if (!showTypeCategories) Debug.Log($"[DynamicCategoryCreator] showTypeCategories is false. Skipping loading type categories.");
                 else Debug.LogWarning($"[DynamicCategoryCreator] dataService.GetAllCategoriesType() returned null.");
            }

             if (allNiveauCategories.Count == 0 && allTypeCategories.Count == 0)
             {
                 Debug.LogWarning("[DynamicCategoryCreator] No categories found in database after initial load (check data and showNiveau/TypeCategories flags).");
             }
        }
         catch (System.Exception e)
        {
            Debug.LogError($"[DynamicCategoryCreator] Error loading all categories data: {e.Message}");
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

        int initialIndex = 0;
        if (initialDisplayMode == CategoryDisplayMode.TypeOnly)
        {
            initialIndex = options.IndexOf("Types");
            if (initialIndex == -1) initialIndex = 0;
        }
        if (initialIndex < 0 || initialIndex >= options.Count) initialIndex = 0;

        displayModeDropdown.value = initialIndex;
        
        // CORRECTION 5: Charger les boutons immédiatement après la configuration du dropdown
        LoadAndCreateCategoryButtons();

        Debug.Log("[DynamicCategoryCreator] Dropdown setup complete. Categories loaded for initial mode.");
    }

    private void OnDisplayModeDropdownChanged(int index)
    {
        if (displayModeDropdown == null || index < 0 || index >= displayModeDropdown.options.Count)
        {
            Debug.LogError("[DynamicCategoryCreator] Invalid dropdown index or dropdown reference.");
            return;
        }

        string selectedText = displayModeDropdown.options[index].text;
        CategoryDisplayMode newMode = currentDisplayMode;

        switch (selectedText)
        {
            case "Niveaux":
                newMode = CategoryDisplayMode.NiveauOnly;
                break;
            case "Types":
                newMode = CategoryDisplayMode.TypeOnly;
                break;
            default:
                Debug.LogWarning($"[DynamicCategoryCreator] Unrecognized dropdown option '{selectedText}' selected.");
                return;
        }

        SetDisplayMode(newMode);
    }

    // MISE À JOUR de LoadAndCreateCategoryButtons()
    void LoadAndCreateCategoryButtons()
    {
        if (dataService == null || !ValidateUIReferences()) return;

        Debug.Log($"[DynamicCategoryCreator] Clearing existing buttons and creating buttons for mode: {currentDisplayMode}");

        ClearExistingButtons();

        // CORRECTION : Recalculer la taille des cellules avant de créer les boutons
        UpdateGridLayoutCellSize();

        int buttonsCreatedCount = 0;

        switch (currentDisplayMode)
        {
            case CategoryDisplayMode.NiveauOnly:
                 if (showNiveauCategories && allNiveauCategories != null && allNiveauCategories.Count > 0)
                {
                    foreach (var category in allNiveauCategories)
                    {
                        CreateNiveauCategoryButton(category);
                        buttonsCreatedCount++;
                    }
                    Debug.Log($"[DynamicCategoryCreator] Created {buttonsCreatedCount} Niveau buttons.");
                } else { Debug.Log("[DynamicCategoryCreator] No Niveau categories to create for NiveauOnly mode."); }
                break;

            case CategoryDisplayMode.TypeOnly:
                 if (showTypeCategories && allTypeCategories != null && allTypeCategories.Count > 0)
                 {
                     foreach (var category in allTypeCategories)
                     {
                        CreateTypeCategoryButton(category);
                        buttonsCreatedCount++;
                     }
                     Debug.Log($"[DynamicCategoryCreator] Created {buttonsCreatedCount} Type buttons.");
                 } else { Debug.Log("[DynamicCategoryCreator] No Type categories to create for TypeOnly mode."); }
                break;

             default:
                 Debug.LogWarning($"[DynamicCategoryCreator] Unexpected currentDisplayMode: {currentDisplayMode}. No category buttons created.");
                 break;
        }

        if (buttonsCreatedCount == 0)
        {
             Debug.LogWarning("[DynamicCategoryCreator] No category buttons created for the current display mode from loaded data.");
        } else {
             Debug.Log($"[DynamicCategoryCreator] Total buttons created: {buttonsCreatedCount}.");
        }

        // Commencer le recalcul et le refresh du layout
        StartCoroutine(RecalculateCellSizeAfterLayout());
    }

    // CORRECTION 7: Nouvelle méthode pour mettre à jour la taille des cellules du grid
    private void UpdateGridLayoutCellSize()
    {
        if (categoriesParent == null) return;

        var gridLayout = categoriesParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            Vector2 newCellSize = CalculateOptimalCellSize();
            gridLayout.cellSize = newCellSize;
            Debug.Log($"[DynamicCategoryCreator] Updated grid layout cell size to: {newCellSize}");
        }
    }

    // NOUVELLE MÉTHODE: Recalculer après que le layout soit établi
    private IEnumerator RecalculateCellSizeAfterLayout()
    {
        // Attendre plusieurs frames pour que le layout soit complètement établi
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        UpdateGridLayoutCellSize();
        
        // Forcer une autre mise à jour du layout
        yield return StartCoroutine(ForceLayoutRefresh());
    }

    private void CreateNiveauCategoryButton(CategorieNiveau category)
    {
        if (category == null || !ValidateUIReferences() || categoryButtonPrefab == null || categoriesParent == null) return;

        Debug.Log($"[DynamicCategoryCreator] Creating Niveau button for: {category.TitreCategNiv} (ID: {category.IdCategNiv})");

        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"NiveauButton_{category.IdCategNiv}";
        button.SetActive(true);

        if (button.transform.parent != categoriesParent)
        {
            Debug.LogError($"[DynamicCategoryCreator] Button parent assignment failed for {category.TitreCategNiv}");
            return;
        }

        SetButtonText(button, category.TitreCategNiv ?? $"Niveau {category.IdCategNiv}");
        SetButtonBaseColor(button, niveauCategoryColor);

        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            int categoryId = category.IdCategNiv;
            buttonComp.onClick.AddListener(() => OnNiveauButtonClick(categoryId, button));
            Debug.Log($"[DynamicCategoryCreator] Successfully created and configured Niveau button: {category.TitreCategNiv}");
        }
         else
        {
             Debug.LogWarning($"[DynamicCategoryCreator] Button prefab for '{category.TitreCategNiv ?? "Niveau"}' is missing Button component.");
        }
    }

    private void CreateTypeCategoryButton(CategorieType category)
    {
        if (category == null || !ValidateUIReferences() || categoryButtonPrefab == null || categoriesParent == null) return;

        Debug.Log($"[DynamicCategoryCreator] Creating Type button for: {category.TitreCategTyp} (ID: {category.IdCategTyp})");

        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"TypeButton_{category.IdCategTyp}";
        button.SetActive(true);

        if (button.transform.parent != categoriesParent)
        {
            Debug.LogError($"[DynamicCategoryCreator] Button parent assignment failed for {category.TitreCategTyp}");
            return;
        }

        SetButtonText(button, category.TitreCategTyp ?? $"Type {category.IdCategTyp}");
        SetButtonBaseColor(button, typeCategoryColor);

        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            int categoryId = category.IdCategTyp;
            buttonComp.onClick.AddListener(() => OnTypeButtonClick(categoryId, button));
            Debug.Log($"[DynamicCategoryCreator] Successfully created and configured Type button: {category.TitreCategTyp}");
        }
        else
        {
             Debug.LogWarning($"[DynamicCategoryCreator] Button prefab for '{category.TitreCategTyp ?? "Type"}' is missing Button component.");
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
         else
        {
             Debug.LogWarning($"[DynamicCategoryCreator] Button '{button.name}' missing Button component to set colors.");
        }
    }

    private void SetButtonText(GameObject button, string text)
    {
        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            // Ajuster la taille de police pour s'adapter aux cellules plus petites
            tmpText.fontSize = Mathf.Min(tmpText.fontSize, 14f);
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMin = 8f;
            tmpText.fontSizeMax = 14f;
            Debug.Log($"[DynamicCategoryCreator] Set TextMeshPro text to '{text}' for button {button.name}");
            return;
        }

        var legacyText = button.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
            legacyText.fontSize = Mathf.Min(legacyText.fontSize, 14);
            Debug.Log($"[DynamicCategoryCreator] Set legacy Text to '{text}' for button {button.name}");
        }
        else
        {
             Debug.LogWarning($"[DynamicCategoryCreator] Button '{button.name}' has no Text or TextMeshProUGUI component in children.");
        }
    }

    void ClearExistingButtons()
    {
        if (categoriesParent == null) return;

        int childCount = categoriesParent.childCount;
        if (childCount == 0)
        {
             Debug.Log("[DynamicCategoryCreator] No existing buttons to clear.");
             return;
        }

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

        Debug.Log($"[DynamicCategoryCreator] Cleared {childCount} existing category buttons.");
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

                Debug.Log($"[DynamicCategoryCreator] Layout refresh completed. Children count: {categoriesParent.childCount}, Parent size: {rectTransform.sizeDelta}");
            }
        }
    }

    bool ValidateUIReferences()
    {
        bool isValid = true;
        if (categoryButtonPrefab == null)
        {
            Debug.LogError("[DynamicCategoryCreator] CRITICAL ERROR: CategoryButtonPrefab is NULL!");
            isValid = false;
        }
        if (categoriesParent == null)
        {
            Debug.LogError("[DynamicCategoryCreator] CRITICAL ERROR: CategoriesParent is NULL!");
            isValid = false;
        }
         if (displayModeDropdown == null)
         {
             Debug.LogWarning("[DynamicCategoryCreator] DisplayModeDropdown is NULL. Category filtering via dropdown will not work; using initialDisplayMode.");
         }

        return isValid;
    }

    private void OnNiveauButtonClick(int categoryId, GameObject button)
    {
        var category = allNiveauCategories?.FirstOrDefault(c => c.IdCategNiv == categoryId);
        string categoryTitle = category != null ? category.TitreCategNiv : $"Unknown Niveau (ID: {categoryId})";

        Debug.Log($"[DynamicCategoryCreator] Niveau category selected: {categoryTitle}");

        UpdateSelectedButton(button);
        selectedNiveauId = categoryId;
        selectedTypeId = -1;

        OnNiveauCategorySelected?.Invoke(categoryId);
    }

    private void OnTypeButtonClick(int categoryId, GameObject button)
    {
        var category = allTypeCategories?.FirstOrDefault(c => c.IdCategTyp == categoryId);
        string categoryTitle = category != null ? category.TitreCategTyp : $"Unknown Type (ID: {categoryId})";

        Debug.Log($"[DynamicCategoryCreator] Type category selected: {categoryTitle}");

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
             Debug.Log($"[DynamicCategoryCreator] Applied selected color to button: {button.name}");
         }
         else
         {
              Debug.LogWarning($"[DynamicCategoryCreator] Cannot apply selected color: Button component missing on {button.name}.");
         }
    }

    private void RestoreButtonColor(GameObject button)
    {
        if (button == null) return;

        Color originalColor;

        if (button.name.StartsWith("NiveauButton_"))
        {
            originalColor = niveauCategoryColor;
        }
        else if (button.name.StartsWith("TypeButton_"))
        {
            originalColor = typeCategoryColor;
        }
        else
        {
            Debug.LogWarning($"[DynamicCategoryCreator] Attempted to restore color for unknown button type: {button.name}. Using default white.");
            originalColor = Color.white;
        }

        SetButtonBaseColor(button, originalColor);
         Debug.Log($"[DynamicCategoryCreator] Restored base color for button: {button.name}");
    }

    public void RefreshCategories()
    {
        if (dataService == null)
        {
            Debug.LogError("[DynamicCategoryCreator] Cannot refresh categories: DataService is null.");
            return;
        }
        Debug.Log("[DynamicCategoryCreator] Refreshing categories display...");
        LoadAndCreateCategoryButtons();
    }

    public void SetDisplayMode(CategoryDisplayMode mode)
    {
        if (currentDisplayMode == mode)
        {
             Debug.Log($"[DynamicCategoryCreator] Display mode already {mode}. No change needed.");
             return;
        }
        currentDisplayMode = mode;
        Debug.Log($"[DynamicCategoryCreator] Display mode set to: {mode}.");
        RefreshCategories();
    }

    public void SetColumnsCount(int columns)
    {
        columnsCount = Mathf.Max(1, columns);
        SetupGridLayout();
        Debug.Log($"[DynamicCategoryCreator] Columns count set to: {columnsCount}.");
        StartCoroutine(ForceLayoutRefresh());
    }

    public int GetSelectedNiveauId() => selectedNiveauId;
    public int GetSelectedTypeId() => selectedTypeId;
    public bool IsNoCategorySelected() => selectedNiveauId == -1 && selectedTypeId == -1;
}