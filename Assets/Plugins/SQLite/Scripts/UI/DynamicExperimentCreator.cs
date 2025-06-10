using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class DynamicExperimentCreator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab; // Préfab du bouton avec TextMeshPro
    public Transform parentTransform; // Parent pour les boutons (ex : ScrollView Content)
    public Text debugText; // Optionnel : pour afficher les messages de debug
    
    [Header("Settings")]
    public float spacing = 10f; // Espacement entre les boutons
    public float initializationDelay = 0.1f; // Délai d'attente réduit
    public float buttonHeight = 60f; // Hauteur fixe pour les boutons
    
    private DataService dataService;
    private List<ExperienceChimie> currentExperiments;
    private bool isInitialized = false;

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }
    
    private IEnumerator InitializeWithDelay()
    {
        ToDebug("Waiting for DatabaseManager initialization...");
        
        // Attendre un court délai initial
        yield return new WaitForSeconds(initializationDelay);
        
        ToDebug("Checking for DatabaseManager instance...");
        
        // Attendre que le DatabaseManager existe et soit prêt
        int attempts = 0;
        const int maxAttempts = 50; // Augmenté pour plus de temps
        
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
                ToDebug("Database is ready! Starting initialization...");
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
        
        // Attendre plusieurs frames pour s'assurer que l'UI est prête
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f); // Délai supplémentaire
        
        InitializeExperimentCreator();
    }

    void InitializeExperimentCreator()
    {
        try
        {
            ToDebug("Starting InitializeExperimentCreator...");
            
            // Validation critique des références UI
            if (!ValidateUIReferences())
            {
                ToDebug("ERROR: UI References validation failed - stopping initialization");
                return;
            }
            
            // Obtenir le service de données depuis le DatabaseManager
            dataService = DatabaseManager.Instance.GetDataService();
            
            if (dataService == null)
            {
                ToDebug("ERROR: DataService is null");
                return;
            }
            
            ToDebug("DataService obtained successfully");
            
            // IMPORTANT: Marquer comme initialisé AVANT les autres opérations
            isInitialized = true;
            
            SetupScrollViewContent();
            LoadAndCreateButtons();
            
            ToDebug("DynamicExperimentCreator initialized successfully");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing DynamicExperimentCreator: {e.Message}");
            ToDebug($"Stack trace: {e.StackTrace}");
        }
    }

    // MÉTHODE AMÉLIORÉE : Validation critique des références UI
    bool ValidateUIReferences()
    {
        ToDebug("=== UI REFERENCES VALIDATION ===");
        
        if (buttonPrefab == null)
        {
            ToDebug("CRITICAL ERROR: ButtonPrefab is NULL! Assign it in the inspector.");
            return false;
        }
        ToDebug($"✓ ButtonPrefab: {buttonPrefab.name}");
        
        if (parentTransform == null)
        {
            ToDebug("CRITICAL ERROR: ParentTransform is NULL! Assign it in the inspector.");
            return false;
        }
        ToDebug($"✓ ParentTransform: {parentTransform.name}");
        
        // Vérifier que le buttonPrefab a un composant Button
        var buttonComp = buttonPrefab.GetComponent<Button>();
        if (buttonComp == null)
        {
            ToDebug("WARNING: ButtonPrefab doesn't have a Button component!");
        }
        else
        {
            ToDebug("✓ ButtonPrefab has Button component");
        }
        
        // Vérifier la structure du buttonPrefab pour les TextMeshPro
        var leftText = buttonPrefab.transform.Find("LeftText");
        var rightText = buttonPrefab.transform.Find("RightText");
        
        if (leftText == null)
        {
            ToDebug("WARNING: ButtonPrefab missing 'LeftText' child object");
        }
        else
        {
            var leftTMP = leftText.GetComponent<TextMeshProUGUI>();
            if (leftTMP == null)
            {
                ToDebug("WARNING: LeftText doesn't have TextMeshProUGUI component");
            }
            else
            {
                ToDebug("✓ LeftText has TextMeshProUGUI component");
            }
        }
        
        if (rightText == null)
        {
            ToDebug("WARNING: ButtonPrefab missing 'RightText' child object");
        }
        else
        {
            var rightTMP = rightText.GetComponent<TextMeshProUGUI>();
            if (rightTMP == null)
            {
                ToDebug("WARNING: RightText doesn't have TextMeshProUGUI component");
            }
            else
            {
                ToDebug("✓ RightText has TextMeshProUGUI component");
            }
        }
        
        ToDebug("=== END UI VALIDATION ===");
        return true;
    }

    void SetupScrollViewContent()
    {
        if (parentTransform == null)
        {
            ToDebug("Error: ParentTransform not assigned!");
            return;
        }

        ToDebug("Setting up ScrollView content...");

        // Configuration du VerticalLayoutGroup
        var layoutGroup = parentTransform.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = parentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            ToDebug("Added VerticalLayoutGroup component");
        }
        
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);

        // Configuration du ContentSizeFitter
        var sizeFitter = parentTransform.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = parentTransform.gameObject.AddComponent<ContentSizeFitter>();
            ToDebug("Added ContentSizeFitter component");
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Configuration du RectTransform du parent
        var contentRect = parentTransform.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            // Forcer une taille minimale
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 100f);
        }
        
        ToDebug("ScrollView content setup completed");
    }

    void LoadAndCreateButtons()
    {
        if (!ValidateInitialization()) return;

        try
        {
            ToDebug("Loading experiments from database...");
            
            // Charger les expériences depuis la base de données
            var experimentsEnumerable = dataService.GetAllExperiences();
            currentExperiments = experimentsEnumerable.ToList();
            
            ToDebug($"Loaded {currentExperiments.Count} experiments from database");

            if (currentExperiments.Count == 0)
            {
                ToDebug("No experiments found in database. Creating a test button...");
                CreateTestButton();
                return;
            }

            // Log des premières expériences pour debug
            for (int i = 0; i < Mathf.Min(3, currentExperiments.Count); i++)
            {
                var exp = currentExperiments[i];
                ToDebug($"Experiment {i + 1}: ID={exp.IdExp}, Title='{exp.TitreExp}', Duration='{exp.Duree}'");
            }

            CreateButtons();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error loading experiments: {e.Message}");
            ToDebug($"Stack trace: {e.StackTrace}");
            
            // En cas d'erreur, créer un bouton de test
            CreateTestButton();
        }
    }

    void CreateButtons()
    {
        if (!ValidateUIReferences())
        {
            ToDebug("ERROR: Cannot create buttons - UI references validation failed");
            return;
        }

        ToDebug("Starting button creation...");

        // Nettoyer les boutons existants
        ClearExistingButtons();

        for (int i = 0; i < currentExperiments.Count; i++)
        {
            var experiment = currentExperiments[i];
            
            try
            {
                CreateExperimentButton(experiment, i);
            }
            catch (System.Exception e)
            {
                ToDebug($"Error creating button for experiment {experiment.IdExp}: {e.Message}");
            }
        }

        ToDebug($"Button creation completed. Created {currentExperiments.Count} buttons");
        
        // Forcer le refresh du layout
        StartCoroutine(ForceLayoutRefresh());
    }
    
    private void CreateExperimentButton(ExperienceChimie experiment, int index)
    {
        ToDebug($"Creating button {index + 1}/{currentExperiments.Count} for experiment: {experiment.TitreExp}");
        
        // Instanciation du bouton
        var newButton = Instantiate(buttonPrefab, parentTransform);
        newButton.name = $"ExperimentButton_{experiment.IdExp}";
        
        // FORCER L'ACTIVATION DU BOUTON
        newButton.SetActive(true);
        
        // Configuration du RectTransform du bouton
        var buttonRect = newButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, buttonHeight);
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
        }
        
        ToDebug($"Button instantiated: {newButton.name}, Active: {newButton.activeInHierarchy}");

        // Recherche et configuration des composants TextMeshProUGUI
        SetupButtonTexts(newButton, experiment);
        
        // Configuration du clic sur le bouton
        SetupButtonClick(newButton, experiment);
        
        // Log des informations du bouton
        LogButtonInfo(newButton, buttonRect);
    }
    
    private void SetupButtonTexts(GameObject button, ExperienceChimie experiment)
    {
        var leftText = button.transform.Find("LeftText")?.GetComponent<TextMeshProUGUI>();
        var rightText = button.transform.Find("RightText")?.GetComponent<TextMeshProUGUI>();

        // Mise à jour du texte de gauche
        if (leftText != null) 
        {
            leftText.text = experiment.TitreExp;
            leftText.fontSize = 14f; // Taille de police appropriée
            leftText.alignment = TextAlignmentOptions.Left;
            ToDebug($"✓ Set left text: '{experiment.TitreExp}'");
        }
        else 
        {
            ToDebug($"✗ LeftText component not found");
            
            // Tentative de création d'un texte par défaut
            var fallbackText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (fallbackText != null)
            {
                fallbackText.text = experiment.TitreExp;
                ToDebug($"✓ Used fallback text component: '{experiment.TitreExp}'");
            }
        }

        // Mise à jour du texte de droite
        if (rightText != null) 
        {
            rightText.text = experiment.Duree;
            rightText.fontSize = 12f;
            rightText.alignment = TextAlignmentOptions.Right;
            ToDebug($"✓ Set right text: '{experiment.Duree}'");
        }
    }
    
    private void SetupButtonClick(GameObject button, ExperienceChimie experiment)
    {
        var buttonComponent = button.GetComponent<Button>();
        if (buttonComponent != null)
        {
            int experimentId = experiment.IdExp;
            buttonComponent.onClick.RemoveAllListeners();
            buttonComponent.onClick.AddListener(() => OnExperimentButtonClick(experimentId));
            ToDebug($"✓ Button click handler configured");
        }
        else
        {
            ToDebug($"✗ Button component not found on {button.name}");
        }
    }
    
    private void LogButtonInfo(GameObject button, RectTransform buttonRect)
    {
        if (buttonRect != null)
        {
            ToDebug($"Button '{button.name}' - Size: {buttonRect.sizeDelta}, Position: {buttonRect.anchoredPosition}");
            ToDebug($"Button Active: {button.activeInHierarchy}, Parent: {button.transform.parent.name}");
        }
    }
    
    // MÉTHODE pour créer un bouton de test en cas de problème
    private void CreateTestButton()
    {
        ToDebug("Creating test button...");
        
        if (!ValidateUIReferences())
        {
            ToDebug("Cannot create test button - UI references invalid");
            return;
        }
        
        var testButton = Instantiate(buttonPrefab, parentTransform);
        testButton.name = "TEST_BUTTON";
        testButton.SetActive(true);
        
        // Configuration du RectTransform
        var buttonRect = testButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, buttonHeight);
        }
        
        // Configuration des textes
        var leftText = testButton.transform.Find("LeftText")?.GetComponent<TextMeshProUGUI>();
        if (leftText != null) 
        {
            leftText.text = "TEST EXPERIMENT";
            leftText.fontSize = 14f;
        }
        
        var rightText = testButton.transform.Find("RightText")?.GetComponent<TextMeshProUGUI>();
        if (rightText != null) 
        {
            rightText.text = "5 min";
            rightText.fontSize = 12f;
        }
        
        // Configuration du clic
        var buttonComponent = testButton.GetComponent<Button>();
        if (buttonComponent != null)
        {
            buttonComponent.onClick.AddListener(() => ToDebug("Test button clicked!"));
        }
        
        ToDebug("Test button created successfully");
        
        // Forcer le refresh du layout
        StartCoroutine(ForceLayoutRefresh());
    }
    
    // MÉTHODE AMÉLIORÉE pour forcer le refresh du layout
    private IEnumerator ForceLayoutRefresh()
    {
        ToDebug("Starting layout refresh process...");
        
        // Attendre plusieurs frames
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();
        
        if (parentTransform != null)
        {
            // Désactiver et réactiver les composants de layout
            var layoutGroup = parentTransform.GetComponent<VerticalLayoutGroup>();
            var sizeFitter = parentTransform.GetComponent<ContentSizeFitter>();
            
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
                yield return null;
                layoutGroup.enabled = true;
                ToDebug("VerticalLayoutGroup refreshed");
            }
            
            if (sizeFitter != null)
            {
                sizeFitter.enabled = false;
                yield return null;
                sizeFitter.enabled = true;
                ToDebug("ContentSizeFitter refreshed");
            }
            
            // Forcer le recalcul multiple fois
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            
            // Forcer le layout du parent
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentTransform.GetComponent<RectTransform>());
            
            // Vérifier et logger les enfants
            LogChildrenStatus();
        }
    }
    
    private void LogChildrenStatus()
    {
        if (parentTransform == null) return;
        
        int visibleChildren = 0;
        int totalChildren = parentTransform.childCount;
        
        ToDebug($"=== CHILDREN STATUS ===");
        ToDebug($"Total children: {totalChildren}");
        
        for (int i = 0; i < totalChildren; i++)
        {
            var child = parentTransform.GetChild(i);
            bool isActive = child.gameObject.activeInHierarchy;
            if (isActive) visibleChildren++;
            
            var rectTransform = child.GetComponent<RectTransform>();
            string sizeInfo = rectTransform != null ? $"Size: {rectTransform.sizeDelta}" : "No RectTransform";
            
            ToDebug($"Child {i}: {child.name} - Active: {isActive} - {sizeInfo}");
        }
        
        ToDebug($"Visible children: {visibleChildren}/{totalChildren}");
        ToDebug($"=== END CHILDREN STATUS ===");
    }

    void ClearExistingButtons()
    {
        if (parentTransform == null) return;

        int childCount = parentTransform.childCount;
        ToDebug($"Clearing {childCount} existing buttons");

        // Supprimer tous les boutons enfants existants
        for (int i = childCount - 1; i >= 0; i--)
        {
            var child = parentTransform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        ToDebug("Existing buttons cleared");
    }

    void OnExperimentButtonClick(int experimentId)
    {
        var experiment = currentExperiments?.FirstOrDefault(e => e.IdExp == experimentId);
        if (experiment != null)
        {
            ToDebug($"Experiment selected: {experiment.TitreExp} ({experiment.Duree})");
            StartExperiment(experiment);
        }
        else
        {
            ToDebug($"Error: Experiment with ID {experimentId} not found");
        }
    }

    void StartExperiment(ExperienceChimie experiment)
    {
        ToDebug($"Starting experiment: {experiment.TitreExp}");
        ToDebug($"Description: {experiment.DescriptionExp}");
        // Ici, vous pouvez ajouter la logique pour démarrer l'expérience
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
            ToDebug("Error: DynamicExperimentCreator not properly initialized");
            return false;
        }
        
        return true;
    }

    // Méthodes publiques pour la gestion dynamique
    public void RefreshExperiments()
    {
        if (!ValidateInitialization()) return;
        ToDebug("Refreshing experiments...");
        LoadAndCreateButtons();
    }

    public void AddNewExperiment(string titre, string description, string duree, int idCategNiv = 1, int idCategTyp = 1)
    {
        if (!ValidateInitialization()) return;

        var newExperiment = new ExperienceChimie
        {
            TitreExp = titre,
            DescriptionExp = description,
            Duree = duree,
            IdCategNiv = idCategNiv,
            IdCategTyp = idCategTyp
        };

        try
        {
            dataService.AddExperience(newExperiment);
            ToDebug($"New experiment added: {titre}");
            RefreshExperiments();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error adding experiment: {e.Message}");
        }
    }

    public void FilterExperimentsByNiveau(int idCategNiv)
    {
        if (!ValidateInitialization()) return;

        try
        {
            if (idCategNiv <= 0)
            {
                currentExperiments = dataService.GetAllExperiences().ToList();
            }
            else
            {
                currentExperiments = dataService.GetExperiencesByNiveau(idCategNiv).ToList();
            }
            
            CreateButtons();
            ToDebug($"Filtered experiments by niveau ID: {idCategNiv}. Found {currentExperiments.Count} experiments.");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error filtering experiments by niveau: {e.Message}");
        }
    }

    public void FilterExperimentsByType(int idCategTyp)
    {
        if (!ValidateInitialization()) return;

        try
        {
            if (idCategTyp <= 0)
            {
                currentExperiments = dataService.GetAllExperiences().ToList();
            }
            else
            {
                currentExperiments = dataService.GetExperiencesByType(idCategTyp).ToList();
            }
            
            CreateButtons();
            ToDebug($"Filtered experiments by type ID: {idCategTyp}. Found {currentExperiments.Count} experiments.");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error filtering experiments by type: {e.Message}");
        }
    }

    public void FilterExperimentsByNiveauAndType(int idCategNiv, int idCategTyp)
    {
        if (!ValidateInitialization()) return;

        try
        {
            currentExperiments = dataService.GetExperiencesByNiveauAndType(idCategNiv, idCategTyp).ToList();
            CreateButtons();
            ToDebug($"Filtered experiments by niveau {idCategNiv} and type {idCategTyp}. Found {currentExperiments.Count} experiments.");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error filtering experiments by niveau and type: {e.Message}");
        }
    }

    private void ToDebug(string message)
    {
        Debug.Log($"[DynamicExperimentCreator] {message}");
        if (debugText != null)
        {
            debugText.text += System.Environment.NewLine + message;
        }
    }

    // Méthodes de test depuis l'inspecteur
    [ContextMenu("Test - Refresh Experiments")]
    public void TestRefreshExperiments()
    {
        RefreshExperiments();
    }

    [ContextMenu("Test - Add Sample Experiment")]
    public void AddSampleExperiment()
    {
        AddNewExperiment("Test Expérience", "Description de test", "10 min", 1, 1);
    }
    
    [ContextMenu("Test - Force Reinitialize")]
    public void ForceReinitialize()
    {
        StartCoroutine(InitializeWithDelay());
    }
    
    [ContextMenu("Test - Validate UI References")]
    public void TestValidateUI()
    {
        ValidateUIReferences();
    }
    
    [ContextMenu("Test - Create Test Button (Menu)")]
    public void CreateTestButtonFromMenu()
    {
        CreateTestButton();
    }
    
    [ContextMenu("Test - Log Children Status")]
    public void TestLogChildren()
    {
        LogChildrenStatus();
    }
}