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
    public Text debugText;
    
    [Header("Category Display Settings")]
    public bool showNiveauCategories = true;
    public bool showTypeCategories = true;
    public CategoryDisplayMode displayMode = CategoryDisplayMode.Mixed;
    
    [Header("Layout Settings")]
    public int columnsCount = 4;
    public float spacing = 10f;
    public Vector2 cellSize = new Vector2(300f, 100f);
    public float initializationDelay = 0.1f;
    
    [Header("Visual Settings")]
    public Color niveauCategoryColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color typeCategoryColor = new Color(1f, 0.6f, 0.2f, 1f);
    public Color selectedCategoryColor = new Color(0.1f, 0.8f, 0.3f, 1f);
    
    [Header("DatabaseManager Reference")]
    [SerializeField] private DatabaseManager databaseManagerReference; // Référence directe
    
    private DataService dataService;
    private List<CategorieNiveau> niveauCategories;
    private List<CategorieType> typeCategories;
    private bool isInitialized = false;
    
    public System.Action<int> OnNiveauCategorySelected;
    public System.Action<int> OnTypeCategorySelected;
    public System.Action OnAllCategoriesSelected;
    
    private int selectedNiveauId = -1;
    private int selectedTypeId = -1;
    private GameObject selectedButton = null;

    public enum CategoryDisplayMode
    {
        NiveauOnly,
        TypeOnly,
        Mixed,
        Separated
    }

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }
    
    private IEnumerator InitializeWithDelay()
    {
        ToDebug("Starting initialization process...");
        
        yield return new WaitForSeconds(initializationDelay);
        
        // Méthode 1: Vérifier la référence directe d'abord
        if (databaseManagerReference != null)
        {
            ToDebug("Using direct DatabaseManager reference");
            if (TryInitializeWithDatabaseManager(databaseManagerReference))
            {
                yield break; // Succès avec la référence directe
            }
        }
        
        // Méthode 2: Chercher par FindObjectOfType
        ToDebug("Searching for DatabaseManager with FindObjectOfType...");
        var foundDatabaseManager = FindObjectOfType<DatabaseManager>();
        if (foundDatabaseManager != null)
        {
            ToDebug("Found DatabaseManager with FindObjectOfType");
            if (TryInitializeWithDatabaseManager(foundDatabaseManager))
            {
                yield break; // Succès avec FindObjectOfType
            }
        }
        
        // Méthode 3: Attendre le singleton (méthode originale améliorée)
        ToDebug("Waiting for DatabaseManager singleton...");
        yield return StartCoroutine(WaitForDatabaseManagerSingleton());
    }
    
    private bool TryInitializeWithDatabaseManager(DatabaseManager dbManager)
    {
        try
        {
            ToDebug($"Trying to initialize with DatabaseManager: {dbManager.name}");
            
            // Vérifier si le DatabaseManager est prêt
            if (!dbManager.IsDatabaseReady())
            {
                ToDebug("DatabaseManager found but database not ready");
                return false;
            }
            
            var service = dbManager.GetDataService();
            if (service == null)
            {
                ToDebug("DatabaseManager found but DataService is null");
                return false;
            }
            
            dataService = service;
            ToDebug("DataService obtained successfully");
            
            InitializeCategoryCreator();
            return true;
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing with DatabaseManager: {e.Message}");
            return false;
        }
    }
    
    private IEnumerator WaitForDatabaseManagerSingleton()
    {
        int attempts = 0;
        const int maxAttempts = 100; // Augmenté pour plus de patience
        
        while (attempts < maxAttempts)
        {
            try
            {
                // Vérifier si la classe DatabaseManager existe et a une instance
                if (DatabaseManager.Instance != null)
                {
                    ToDebug("DatabaseManager singleton found!");
                    
                    if (DatabaseManager.Instance.IsDatabaseReady())
                    {
                        ToDebug("Database is ready!");
                        dataService = DatabaseManager.Instance.GetDataService();
                        
                        if (dataService != null)
                        {
                            ToDebug("DataService obtained from singleton");
                            InitializeCategoryCreator();
                            yield break; // Succès
                        }
                    }
                    else
                    {
                        ToDebug($"Database not ready, waiting... Attempt {attempts + 1}/{maxAttempts}");
                    }
                }
                else
                {
                    ToDebug($"DatabaseManager singleton not found. Attempt {attempts + 1}/{maxAttempts}");
                }
            }
            catch (System.Exception e)
            {
                ToDebug($"Error checking DatabaseManager singleton: {e.Message}");
            }
            
            yield return new WaitForSeconds(0.2f);
            attempts++;
        }
        
        ToDebug("ERROR: Could not initialize DatabaseManager after all attempts");
        ToDebug("Creating fallback test buttons...");
        
        // Fallback: créer des boutons de test
        CreateTestButtons();
    }

    void InitializeCategoryCreator()
    {
        try
        {
            ToDebug("Starting InitializeCategoryCreator...");
            
            if (!ValidateUIReferences())
            {
                ToDebug("ERROR: UI References validation failed");
                CreateTestButtons(); // Fallback
                return;
            }
            
            if (dataService == null)
            {
                ToDebug("ERROR: DataService is null");
                CreateTestButtons(); // Fallback
                return;
            }
            
            isInitialized = true;
            SetupGridLayout();
            LoadAndCreateCategoryButtons();
            
            ToDebug("DynamicCategoryCreator initialized successfully");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing DynamicCategoryCreator: {e.Message}");
            ToDebug("Creating fallback test buttons...");
            CreateTestButtons();
        }
    }

    bool ValidateUIReferences()
    {
        ToDebug("=== UI REFERENCES VALIDATION ===");
        
        if (categoryButtonPrefab == null)
        {
            ToDebug("CRITICAL ERROR: CategoryButtonPrefab is NULL!");
            return false;
        }
        ToDebug($"✓ CategoryButtonPrefab: {categoryButtonPrefab.name}");
        
        if (categoriesParent == null)
        {
            ToDebug("CRITICAL ERROR: CategoriesParent is NULL!");
            return false;
        }
        ToDebug($"✓ CategoriesParent: {categoriesParent.name}");
        
        var buttonComp = categoryButtonPrefab.GetComponent<Button>();
        if (buttonComp == null)
        {
            ToDebug("WARNING: CategoryButtonPrefab doesn't have a Button component!");
        }
        else
        {
            ToDebug("✓ CategoryButtonPrefab has Button component");
        }
        
        var textComp = categoryButtonPrefab.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp == null)
        {
            var legacyText = categoryButtonPrefab.GetComponentInChildren<Text>();
            if (legacyText == null)
            {
                ToDebug("WARNING: CategoryButtonPrefab doesn't have Text component!");
            }
            else
            {
                ToDebug("✓ CategoryButtonPrefab has legacy Text component");
            }
        }
        else
        {
            ToDebug("✓ CategoryButtonPrefab has TextMeshProUGUI component");
        }
        
        ToDebug("=== END UI VALIDATION ===");
        return true;
    }

    void SetupGridLayout()
    {
        if (categoriesParent == null) return;

        ToDebug("Setting up Grid Layout...");

        var gridLayout = categoriesParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = categoriesParent.gameObject.AddComponent<GridLayoutGroup>();
            ToDebug("Added GridLayoutGroup component");
        }
        
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnsCount;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        var sizeFitter = categoriesParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = categoriesParent.gameObject.AddComponent<ContentSizeFitter>();
            ToDebug("Added ContentSizeFitter component");
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ToDebug("Grid Layout setup completed");
    }

    void LoadAndCreateCategoryButtons()
    {
        if (!ValidateInitialization())
        {
            ToDebug("Validation failed, creating test buttons");
            CreateTestButtons();
            return;
        }

        try
        {
            ToDebug("Loading categories from database...");
            
            if (showNiveauCategories)
            {
                var niveauxEnumerable = dataService.GetAllCategoriesNiveau();
                niveauCategories = niveauxEnumerable?.ToList() ?? new List<CategorieNiveau>();
                ToDebug($"Loaded {niveauCategories.Count} niveau categories");
            }
            
            if (showTypeCategories)
            {
                var typesEnumerable = dataService.GetAllCategoriesType();
                typeCategories = typesEnumerable?.ToList() ?? new List<CategorieType>();
                ToDebug($"Loaded {typeCategories.Count} type categories");
            }

            if ((niveauCategories?.Count ?? 0) == 0 && (typeCategories?.Count ?? 0) == 0)
            {
                ToDebug("No categories found in database, creating test buttons");
                CreateTestButtons();
                return;
            }

            CreateCategoryButtons();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error loading categories: {e.Message}");
            CreateTestButtons();
        }
    }

    void CreateCategoryButtons()
    {
        if (!ValidateUIReferences())
        {
            ToDebug("ERROR: Cannot create buttons - UI references validation failed");
            return;
        }

        ToDebug("Starting category buttons creation...");
        ClearExistingButtons();
        CreateAllCategoriesButton();

        switch (displayMode)
        {
            case CategoryDisplayMode.NiveauOnly:
                CreateNiveauButtons();
                break;
            case CategoryDisplayMode.TypeOnly:
                CreateTypeButtons();
                break;
            case CategoryDisplayMode.Mixed:
                CreateMixedButtons();
                break;
            case CategoryDisplayMode.Separated:
                CreateSeparatedButtons();
                break;
        }

        ToDebug("Category buttons creation completed");
        StartCoroutine(ForceLayoutRefresh());
    }
    
    private void CreateAllCategoriesButton()
    {
        ToDebug("Creating 'All Categories' button");
        
        var allButton = Instantiate(categoryButtonPrefab, categoriesParent);
        allButton.name = "AllCategoriesButton";
        allButton.SetActive(true);
        
        SetButtonText(allButton, "Toutes les catégories");
        SetButtonColor(allButton, Color.white);
        
        var buttonComp = allButton.GetComponent<Button>();
        if (buttonComp != null)
        {
            buttonComp.onClick.AddListener(() => OnAllCategoriesButtonClick(allButton));
        }
    }

    private void CreateNiveauButtons()
    {
        if (niveauCategories == null || niveauCategories.Count == 0)
        {
            ToDebug("No niveau categories to create");
            return;
        }

        ToDebug($"Creating {niveauCategories.Count} niveau category buttons");

        foreach (var category in niveauCategories)
        {
            CreateNiveauCategoryButton(category);
        }
    }

    private void CreateTypeButtons()
    {
        if (typeCategories == null || typeCategories.Count == 0)
        {
            ToDebug("No type categories to create");
            return;
        }

        ToDebug($"Creating {typeCategories.Count} type category buttons");

        foreach (var category in typeCategories)
        {
            CreateTypeCategoryButton(category);
        }
    }

    private void CreateMixedButtons()
    {
        CreateNiveauButtons();
        CreateTypeButtons();
    }

    private void CreateSeparatedButtons()
    {
        CreateNiveauButtons();
        CreateTypeButtons();
    }

    private void CreateNiveauCategoryButton(CategorieNiveau category)
    {
        if (category == null)
        {
            ToDebug("Cannot create button: category is null");
            return;
        }
        
        ToDebug($"Creating niveau button: {category.TitreCategNiv}");
        
        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"NiveauButton_{category.IdCategNiv}";
        button.SetActive(true);
        
        SetButtonText(button, category.TitreCategNiv ?? "Niveau");
        SetButtonColor(button, niveauCategoryColor);
        
        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            int categoryId = category.IdCategNiv;
            buttonComp.onClick.AddListener(() => OnNiveauButtonClick(categoryId, button));
        }
    }

    private void CreateTypeCategoryButton(CategorieType category)
    {
        if (category == null)
        {
            ToDebug("Cannot create button: category is null");
            return;
        }
        
        ToDebug($"Creating type button: {category.TitreCategTyp}");
        
        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"TypeButton_{category.IdCategTyp}";
        button.SetActive(true);
        
        SetButtonText(button, category.TitreCategTyp ?? "Type");
        SetButtonColor(button, typeCategoryColor);
        
        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            int categoryId = category.IdCategTyp;
            buttonComp.onClick.AddListener(() => OnTypeButtonClick(categoryId, button));
        }
    }

    private void SetButtonText(GameObject button, string text)
    {
        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            tmpText.fontSize = 16f;
            tmpText.alignment = TextAlignmentOptions.Center;
            return;
        }
        
        var legacyText = button.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = text;
            legacyText.fontSize = 16;
            legacyText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void SetButtonColor(GameObject button, Color color)
    {
        var buttonComp = button.GetComponent<Button>();
        if (buttonComp != null)
        {
            var colors = buttonComp.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            buttonComp.colors = colors;
        }
    }

    private void CreateTestButtons()
    {
        ToDebug("Creating test category buttons...");
        
        if (!ValidateUIReferences())
        {
            ToDebug("Cannot create test buttons - UI validation failed");
            return;
        }
        
        ClearExistingButtons();
        SetupGridLayout(); // S'assurer que le layout est configuré
        
        string[] testCategories = { "Toutes", "Débutant", "Intermédiaire", "Avancé", "Acides", "Bases", "Sels" };
        
        for (int i = 0; i < testCategories.Length; i++)
        {
            var testButton = Instantiate(categoryButtonPrefab, categoriesParent);
            testButton.name = $"TestButton_{i}";
            testButton.SetActive(true);
            
            SetButtonText(testButton, testCategories[i]);
            
            Color buttonColor = Color.white;
            if (i == 0) buttonColor = Color.white; // Toutes
            else if (i <= 3) buttonColor = niveauCategoryColor; // Niveaux
            else buttonColor = typeCategoryColor; // Types
            
            SetButtonColor(testButton, buttonColor);
            
            var buttonComp = testButton.GetComponent<Button>();
            if (buttonComp != null)
            {
                string categoryName = testCategories[i];
                int index = i;
                buttonComp.onClick.AddListener(() => {
                    ToDebug($"Test button clicked: {categoryName}");
                    if (index == 0)
                    {
                        OnAllCategoriesButtonClick(testButton);
                    }
                });
            }
        }
        
        ToDebug("Test buttons created successfully");
        StartCoroutine(ForceLayoutRefresh());
    }

    void ClearExistingButtons()
    {
        if (categoriesParent == null) return;

        int childCount = categoriesParent.childCount;
        ToDebug($"Clearing {childCount} existing category buttons");

        for (int i = childCount - 1; i >= 0; i--)
        {
            var child = categoriesParent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        selectedNiveauId = -1;
        selectedTypeId = -1;
        selectedButton = null;
        
        ToDebug("Existing category buttons cleared");
    }

    private IEnumerator ForceLayoutRefresh()
    {
        ToDebug("Starting layout refresh process...");
        
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();
        
        if (categoriesParent != null)
        {
            var gridLayout = categoriesParent.GetComponent<GridLayoutGroup>();
            var sizeFitter = categoriesParent.GetComponent<ContentSizeFitter>();
            
            if (gridLayout != null)
            {
                gridLayout.enabled = false;
                yield return null;
                gridLayout.enabled = true;
                ToDebug("GridLayoutGroup refreshed");
            }
            
            if (sizeFitter != null)
            {
                sizeFitter.enabled = false;
                yield return null;
                sizeFitter.enabled = true;
                ToDebug("ContentSizeFitter refreshed");
            }
            
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(categoriesParent.GetComponent<RectTransform>());
            
            LogCategoryButtonsStatus();
        }
    }

    private void LogCategoryButtonsStatus()
    {
        if (categoriesParent == null) return;
        
        int totalButtons = categoriesParent.childCount;
        int activeButtons = 0;
        
        ToDebug($"=== CATEGORY BUTTONS STATUS ===");
        ToDebug($"Total buttons: {totalButtons}");
        
        for (int i = 0; i < totalButtons; i++)
        {
            var child = categoriesParent.GetChild(i);
            bool isActive = child.gameObject.activeInHierarchy;
            if (isActive) activeButtons++;
            
            ToDebug($"Button {i}: {child.name} - Active: {isActive}");
        }
        
        ToDebug($"Active buttons: {activeButtons}/{totalButtons}");
        ToDebug($"=== END BUTTONS STATUS ===");
    }

    // Gestionnaires d'événements
    private void OnAllCategoriesButtonClick(GameObject button)
    {
        ToDebug("All categories button clicked");
        
        UpdateSelectedButton(button);
        selectedNiveauId = -1;
        selectedTypeId = -1;
        
        OnAllCategoriesSelected?.Invoke();
    }

    private void OnNiveauButtonClick(int categoryId, GameObject button)
    {
        var category = niveauCategories?.FirstOrDefault(c => c.IdCategNiv == categoryId);
        if (category != null)
        {
            ToDebug($"Niveau category selected: {category.TitreCategNiv} (ID: {categoryId})");
            
            UpdateSelectedButton(button);
            selectedNiveauId = categoryId;
            selectedTypeId = -1;
            
            OnNiveauCategorySelected?.Invoke(categoryId);
        }
    }

    private void OnTypeButtonClick(int categoryId, GameObject button)
    {
        var category = typeCategories?.FirstOrDefault(c => c.IdCategTyp == categoryId);
        if (category != null)
        {
            ToDebug($"Type category selected: {category.TitreCategTyp} (ID: {categoryId})");
            
            UpdateSelectedButton(button);
            selectedTypeId = categoryId;
            selectedNiveauId = -1;
            
            OnTypeCategorySelected?.Invoke(categoryId);
        }
    }

    private void UpdateSelectedButton(GameObject newSelectedButton)
    {
        if (selectedButton != null)
        {
            RestoreButtonColor(selectedButton);
        }
        
        selectedButton = newSelectedButton;
        if (selectedButton != null)
        {
            SetButtonColor(selectedButton, selectedCategoryColor);
        }
    }

    private void RestoreButtonColor(GameObject button)
    {
        if (button == null) return;
        
        Color originalColor = Color.white;
        
        if (button.name.StartsWith("NiveauButton_"))
        {
            originalColor = niveauCategoryColor;
        }
        else if (button.name.StartsWith("TypeButton_"))
        {
            originalColor = typeCategoryColor;
        }
        
        SetButtonColor(button, originalColor);
    }

    private bool ValidateInitialization()
    {
        if (dataService == null)
        {
            ToDebug("Error: DataService not initialized");
            return false;
        }
        
        if (!isInitialized)
        {
            ToDebug("Error: DynamicCategoryCreator not properly initialized");
            return false;
        }
        
        return true;
    }

    // Méthodes publiques
    public void RefreshCategories()
    {
        ToDebug("Refreshing categories...");
        if (ValidateInitialization())
        {
            LoadAndCreateCategoryButtons();
        }
        else
        {
            CreateTestButtons();
        }
    }

    public void SetDisplayMode(CategoryDisplayMode mode)
    {
        displayMode = mode;
        ToDebug($"Display mode changed to: {mode}");
        RefreshCategories();
    }

    public void SetColumnsCount(int columns)
    {
        columnsCount = Mathf.Max(1, columns);
        SetupGridLayout();
        ToDebug($"Columns count set to: {columnsCount}");
    }

    public int GetSelectedNiveauId() => selectedNiveauId;
    public int GetSelectedTypeId() => selectedTypeId;

    private void ToDebug(string message)
    {
        string fullMessage = $"[DynamicCategoryCreator] {message}";
        Debug.Log(fullMessage);
        
        if (debugText != null)
        {
            debugText.text += System.Environment.NewLine + fullMessage;
        }
    }

    // Méthodes de test
    [ContextMenu("Test - Refresh Categories")]
    public void TestRefreshCategories() => RefreshCategories();

    [ContextMenu("Test - Switch to Niveau Only")]
    public void TestNiveauOnly() => SetDisplayMode(CategoryDisplayMode.NiveauOnly);

    [ContextMenu("Test - Switch to Type Only")]  
    public void TestTypeOnly() => SetDisplayMode(CategoryDisplayMode.TypeOnly);

    [ContextMenu("Test - Switch to Mixed")]
    public void TestMixed() => SetDisplayMode(CategoryDisplayMode.Mixed);

    [ContextMenu("Test - Create Test Buttons")]
    public void TestCreateTestButtons() => CreateTestButtons();

    [ContextMenu("Test - Validate UI References")]
    public void TestValidateUI() => ValidateUIReferences();

    [ContextMenu("Test - Log Buttons Status")]
    public void TestLogButtons() => LogCategoryButtonsStatus();

    [ContextMenu("Test - Force Initialize")]
    public void TestForceInitialize()
    {
        ToDebug("Force initialize requested");
        StopAllCoroutines();
        StartCoroutine(InitializeWithDelay());
    }
}