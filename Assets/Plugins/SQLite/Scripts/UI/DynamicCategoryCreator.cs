using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class DynamicCategoryCreator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject categoryButtonPrefab; // Préfab du bouton de catégorie
    public Transform categoriesParent; // Parent pour les boutons (ex : Grid Layout Group)
    public Text debugText; // Optionnel : pour afficher les messages de debug
    
    [Header("Category Display Settings")]
    public bool showNiveauCategories = true; // Afficher les catégories de niveau
    public bool showTypeCategories = true; // Afficher les catégories de type
    public CategoryDisplayMode displayMode = CategoryDisplayMode.Mixed; // Mode d'affichage
    
    [Header("Layout Settings")]
    public int columnsCount = 4; // Nombre de colonnes dans la grille
    public float spacing = 10f; // Espacement entre les boutons
    public Vector2 cellSize = new Vector2(300f, 100f); // Taille des cellules
    public float initializationDelay = 0.1f; // Délai d'initialisation
    
    [Header("Visual Settings")]
    public Color niveauCategoryColor = new Color(0.2f, 0.6f, 1f, 1f); // Couleur pour les catégories niveau
    public Color typeCategoryColor = new Color(1f, 0.6f, 0.2f, 1f); // Couleur pour les catégories type
    public Color selectedCategoryColor = new Color(0.1f, 0.8f, 0.3f, 1f); // Couleur pour la catégorie sélectionnée
    
    private DataService dataService;
    private List<CategorieNiveau> niveauCategories;
    private List<CategorieType> typeCategories;
    private bool isInitialized = false;
    
    // Événements pour la sélection de catégories
    public System.Action<int> OnNiveauCategorySelected;
    public System.Action<int> OnTypeCategorySelected;
    public System.Action OnAllCategoriesSelected;
    
    // Catégories sélectionnées actuellement
    private int selectedNiveauId = -1;
    private int selectedTypeId = -1;
    private GameObject selectedButton = null;

    public enum CategoryDisplayMode
    {
        NiveauOnly,     // Seulement les catégories de niveau
        TypeOnly,       // Seulement les catégories de type
        Mixed,          // Les deux types mélangés
        Separated       // Les deux types séparés avec des sections
    }

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }
    
    private IEnumerator InitializeWithDelay()
    {
        ToDebug("Waiting for DatabaseManager initialization...");
        
        yield return new WaitForSeconds(initializationDelay);
        
        ToDebug("Checking for DatabaseManager instance...");
        
        int attempts = 0;
        const int maxAttempts = 50;
        
        while (attempts < maxAttempts)
        {
            if (DatabaseManager.Instance == null)
            {
                ToDebug($"DatabaseManager not found. Attempt {attempts + 1}/{maxAttempts}");
                yield return new WaitForSeconds(0.2f);
                attempts++;
                continue;
            }
            
            ToDebug("DatabaseManager found! Checking if database is ready...");
            
            if (DatabaseManager.Instance.IsDatabaseReady())
            {
                ToDebug("Database is ready! Starting category initialization...");
                break;
            }
            
            ToDebug($"Waiting for database to be ready... Attempt {attempts + 1}/{maxAttempts}");
            yield return new WaitForSeconds(0.2f);
            attempts++;
        }
        
        if (attempts >= maxAttempts)
        {
            ToDebug("ERROR: Maximum attempts reached. Database initialization failed.");
            yield break;
        }
        
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f);
        
        InitializeCategoryCreator();
    }

    void InitializeCategoryCreator()
    {
        try
        {
            ToDebug("Starting InitializeCategoryCreator...");
            
            if (!ValidateUIReferences())
            {
                ToDebug("ERROR: UI References validation failed - stopping initialization");
                return;
            }
            
            dataService = DatabaseManager.Instance.GetDataService();
            
            if (dataService == null)
            {
                ToDebug("ERROR: DataService is null");
                return;
            }
            
            ToDebug("DataService obtained successfully");
            
            isInitialized = true;
            
            SetupGridLayout();
            LoadAndCreateCategoryButtons();
            
            ToDebug("DynamicCategoryCreator initialized successfully");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing DynamicCategoryCreator: {e.Message}");
            ToDebug($"Stack trace: {e.StackTrace}");
        }
    }

    bool ValidateUIReferences()
    {
        ToDebug("=== UI REFERENCES VALIDATION ===");
        
        if (categoryButtonPrefab == null)
        {
            ToDebug("CRITICAL ERROR: CategoryButtonPrefab is NULL! Assign it in the inspector.");
            return false;
        }
        ToDebug($"✓ CategoryButtonPrefab: {categoryButtonPrefab.name}");
        
        if (categoriesParent == null)
        {
            ToDebug("CRITICAL ERROR: CategoriesParent is NULL! Assign it in the inspector.");
            return false;
        }
        ToDebug($"✓ CategoriesParent: {categoriesParent.name}");
        
        // Vérifier les composants nécessaires du prefab
        var buttonComp = categoryButtonPrefab.GetComponent<Button>();
        if (buttonComp == null)
        {
            ToDebug("WARNING: CategoryButtonPrefab doesn't have a Button component!");
        }
        else
        {
            ToDebug("✓ CategoryButtonPrefab has Button component");
        }
        
        // Vérifier le texte du bouton
        var textComp = categoryButtonPrefab.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp == null)
        {
            var legacyText = categoryButtonPrefab.GetComponentInChildren<Text>();
            if (legacyText == null)
            {
                ToDebug("WARNING: CategoryButtonPrefab doesn't have Text or TextMeshProUGUI component!");
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

        // Configuration du GridLayoutGroup
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

        // Configuration du ContentSizeFitter
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
        if (!ValidateInitialization()) return;

        try
        {
            ToDebug("Loading categories from database...");
            
            // Charger les catégories depuis la base de données
            if (showNiveauCategories)
            {
                var niveauxEnumerable = dataService.GetAllCategoriesNiveau();
                niveauCategories = niveauxEnumerable.ToList();
                ToDebug($"Loaded {niveauCategories.Count} niveau categories");
            }
            
            if (showTypeCategories)
            {
                var typesEnumerable = dataService.GetAllCategoriesType();
                typeCategories = typesEnumerable.ToList();
                ToDebug($"Loaded {typeCategories.Count} type categories");
            }

            CreateCategoryButtons();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error loading categories: {e.Message}");
            ToDebug($"Stack trace: {e.StackTrace}");
            
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

        // Créer le bouton "Toutes les catégories" en premier
        CreateAllCategoriesButton();

        // Créer les boutons selon le mode d'affichage
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
        
        // Configuration du texte
        SetButtonText(allButton, "Toutes les catégories");
        
        // Configuration de la couleur
        SetButtonColor(allButton, Color.white);
        
        // Configuration du clic
        var buttonComp = allButton.GetComponent<Button>();
        if (buttonComp != null)
        {
            buttonComp.onClick.AddListener(() => OnAllCategoriesButtonClick(allButton));
        }
    }

    private void CreateNiveauButtons()
    {
        if (niveauCategories == null) return;

        ToDebug($"Creating {niveauCategories.Count} niveau category buttons");

        for (int i = 0; i < niveauCategories.Count; i++)
        {
            var category = niveauCategories[i];
            CreateNiveauCategoryButton(category);
        }
    }

    private void CreateTypeButtons()
    {
        if (typeCategories == null) return;

        ToDebug($"Creating {typeCategories.Count} type category buttons");

        for (int i = 0; i < typeCategories.Count; i++)
        {
            var category = typeCategories[i];
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
        // Dans ce mode, on pourrait ajouter des headers/séparateurs
        // Pour la simplicité, on fait comme Mixed mais on pourrait améliorer
        CreateNiveauButtons();
        CreateTypeButtons();
    }

    private void CreateNiveauCategoryButton(CategorieNiveau category)
    {
        ToDebug($"Creating niveau button: {category.TitreCategNiv}");
        
        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"NiveauButton_{category.IdCategNiv}";
        button.SetActive(true);
        
        SetButtonText(button, category.TitreCategNiv);
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
        ToDebug($"Creating type button: {category.TitreCategTyp}");
        
        var button = Instantiate(categoryButtonPrefab, categoriesParent);
        button.name = $"TypeButton_{category.IdCategTyp}";
        button.SetActive(true);
        
        SetButtonText(button, category.TitreCategTyp);
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
        // Essayer TextMeshProUGUI d'abord
        var tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            tmpText.fontSize = 16f;
            tmpText.alignment = TextAlignmentOptions.Center;
            return;
        }
        
        // Puis essayer Text legacy
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
        
        ClearExistingButtons();
        
        // Créer quelques boutons de test
        string[] testCategories = { "Débutant", "Intermédiaire", "Avancé", "Acides", "Bases", "Sels" };
        
        for (int i = 0; i < testCategories.Length; i++)
        {
            var testButton = Instantiate(categoryButtonPrefab, categoriesParent);
            testButton.name = $"TestButton_{i}";
            testButton.SetActive(true);
            
            SetButtonText(testButton, testCategories[i]);
            SetButtonColor(testButton, i < 3 ? niveauCategoryColor : typeCategoryColor);
            
            var buttonComp = testButton.GetComponent<Button>();
            if (buttonComp != null)
            {
                string categoryName = testCategories[i];
                buttonComp.onClick.AddListener(() => ToDebug($"Test button clicked: {categoryName}"));
            }
        }
        
        ToDebug("Test buttons created");
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
        
        // Réinitialiser les sélections
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

    // Gestionnaires d'événements pour les clics
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
            selectedTypeId = -1; // Reset type selection
            
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
            selectedNiveauId = -1; // Reset niveau selection
            
            OnTypeCategorySelected?.Invoke(categoryId);
        }
    }

    private void UpdateSelectedButton(GameObject newSelectedButton)
    {
        // Restaurer la couleur du bouton précédemment sélectionné
        if (selectedButton != null)
        {
            RestoreButtonColor(selectedButton);
        }
        
        // Mettre en surbrillance le nouveau bouton sélectionné
        selectedButton = newSelectedButton;
        if (selectedButton != null)
        {
            SetButtonColor(selectedButton, selectedCategoryColor);
        }
    }

    private void RestoreButtonColor(GameObject button)
    {
        if (button == null) return;
        
        // Déterminer la couleur originale basée sur le nom du bouton
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

    // Méthodes publiques pour la gestion
    public void RefreshCategories()
    {
        if (!ValidateInitialization()) return;
        ToDebug("Refreshing categories...");
        LoadAndCreateCategoryButtons();
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

    public int GetSelectedNiveauId()
    {
        return selectedNiveauId;
    }

    public int GetSelectedTypeId()
    {
        return selectedTypeId;
    }

    private void ToDebug(string message)
    {
        Debug.Log($"[DynamicCategoryCreator] {message}");
        if (debugText != null)
        {
            debugText.text += System.Environment.NewLine + message;
        }
    }

    // Méthodes de test depuis l'inspecteur
    [ContextMenu("Test - Refresh Categories")]
    public void TestRefreshCategories()
    {
        RefreshCategories();
    }

    [ContextMenu("Test - Switch to Niveau Only")]
    public void TestNiveauOnly()
    {
        SetDisplayMode(CategoryDisplayMode.NiveauOnly);
    }

    [ContextMenu("Test - Switch to Type Only")]
    public void TestTypeOnly()
    {
        SetDisplayMode(CategoryDisplayMode.TypeOnly);
    }

    [ContextMenu("Test - Switch to Mixed")]
    public void TestMixed()
    {
        SetDisplayMode(CategoryDisplayMode.Mixed);
    }

    [ContextMenu("Test - Create Test Buttons")]
    public void TestCreateTestButtons()
    {
        CreateTestButtons();
    }

    [ContextMenu("Test - Validate UI References")]
    public void TestValidateUI()
    {
        ValidateUIReferences();
    }

    [ContextMenu("Test - Log Buttons Status")]
    public void TestLogButtons()
    {
        LogCategoryButtonsStatus();
    }
}