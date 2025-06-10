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
    public float initializationDelay = 0.5f; // Délai d'attente pour l'initialisation de la DB
    
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
        
        // Attendre que le DatabaseManager soit prêt
        yield return new WaitForSeconds(initializationDelay);
        
        ToDebug("Checking for DatabaseManager instance...");
        
        // Vérifier si le DatabaseManager existe
        if (DatabaseManager.Instance == null)
        {
            ToDebug("ERROR: DatabaseManager not found! Please add DatabaseManager to scene.");
            yield break;
        }
        
        ToDebug("DatabaseManager found! Checking if database is ready...");
        
        // Attendre que la base de données soit prête
        int attempts = 0;
        const int maxAttempts = 10;
        
        while (!DatabaseManager.Instance.IsDatabaseReady() && attempts < maxAttempts)
        {
            ToDebug($"Waiting for database... Attempt {attempts + 1}/{maxAttempts}");
            yield return new WaitForSeconds(0.5f);
            attempts++;
        }
        
        if (!DatabaseManager.Instance.IsDatabaseReady())
        {
            ToDebug("ERROR: Database not ready after maximum attempts");
            yield break;
        }
        
        ToDebug("Database is ready! Starting initialization...");
        InitializeExperimentCreator();
    }

    void InitializeExperimentCreator()
    {
        try
        {
            // Obtenir le service de données depuis le DatabaseManager
            dataService = DatabaseManager.Instance.GetDataService();
            
            if (dataService == null)
            {
                ToDebug("ERROR: DataService is null");
                return;
            }
            
            ToDebug("DataService obtained successfully");
            
            SetupScrollViewContent();
            LoadAndCreateButtons();
            
            isInitialized = true;
            ToDebug("DynamicExperimentCreator initialized successfully");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing DynamicExperimentCreator: {e.Message}");
        }
    }

    void SetupScrollViewContent()
    {
        if (parentTransform == null)
        {
            ToDebug("Error: ParentTransform not assigned!");
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

    void LoadAndCreateButtons()
    {
        if (!ValidateInitialization()) return;

        try
        {
            // Charger les expériences depuis la base de données
            currentExperiments = dataService.GetAllExperiences().ToList();
            ToDebug($"Loaded {currentExperiments.Count} experiments from database");

            if (currentExperiments.Count == 0)
            {
                ToDebug("No experiments found in database. The database might be empty.");
                return;
            }

            CreateButtons();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error loading experiments: {e.Message}");
        }
    }

    void CreateButtons()
    {
        if (buttonPrefab == null || parentTransform == null)
        {
            ToDebug("Error: ButtonPrefab or ParentTransform not assigned!");
            return;
        }

        // Nettoyer les boutons existants
        ClearExistingButtons();

        for (int i = 0; i < currentExperiments.Count; i++)
        {
            var experiment = currentExperiments[i];
            
            // Instanciation du bouton
            var newButton = Instantiate(buttonPrefab, parentTransform);
            newButton.name = $"ExperimentButton_{experiment.IdExp}";

            // Recherche des composants TextMeshProUGUI
            var leftText = newButton.transform.Find("LeftText")?.GetComponent<TextMeshProUGUI>();
            var rightText = newButton.transform.Find("RightText")?.GetComponent<TextMeshProUGUI>();

            // Mise à jour des textes - Affichage du nom (TitreExp) et de la durée
            if (leftText != null) 
            {
                leftText.text = experiment.TitreExp; // Nom de l'expérience
            }
            else 
            {
                ToDebug($"Warning: LeftText (TextMeshProUGUI) not found on button for experiment {experiment.IdExp}");
            }

            if (rightText != null) 
            {
                rightText.text = experiment.Duree; // Durée de l'expérience
            }
            else 
            {
                ToDebug($"Warning: RightText (TextMeshProUGUI) not found on button for experiment {experiment.IdExp}");
            }

            // Configuration du clic sur le bouton
            var buttonComponent = newButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                int experimentId = experiment.IdExp; // Capture locale pour éviter les problèmes de closure
                buttonComponent.onClick.AddListener(() => OnExperimentButtonClick(experimentId));
            }
            else
            {
                ToDebug($"Warning: Button component not found on experiment button {experiment.IdExp}");
            }
        }

        ToDebug($"Created {currentExperiments.Count} experiment buttons");
    }

    void ClearExistingButtons()
    {
        if (parentTransform == null) return;

        // Supprimer tous les boutons enfants existants
        for (int i = parentTransform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(parentTransform.GetChild(i).gameObject);
        }
    }

    void OnExperimentButtonClick(int experimentId)
    {
        var experiment = currentExperiments.FirstOrDefault(e => e.IdExp == experimentId);
        if (experiment != null)
        {
            ToDebug($"Experiment selected: {experiment.TitreExp} ({experiment.Duree})");
            
            // Ici vous pouvez ajouter la logique pour lancer l'expérience
            // Par exemple :
            StartExperiment(experiment);
        }
        else
        {
            ToDebug($"Error: Experiment with ID {experimentId} not found");
        }
    }

    void StartExperiment(ExperienceChimie experiment)
    {
        // Ajoutez ici votre logique pour démarrer l'expérience
        // Par exemple, charger une scène spécifique, ouvrir un panel, etc.
        
        ToDebug($"Starting experiment: {experiment.TitreExp}");
        ToDebug($"Description: {experiment.DescriptionExp}");
        
        // Exemple : Charger une scène basée sur l'ID de l'expérience
        // UnityEngine.SceneManagement.SceneManager.LoadScene($"Experiment_{experiment.IdExp}");
        
        // Ou ouvrir un panel spécifique
        // ShowExperimentPanel(experiment);
    }

    // Validation helper
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

    // Méthodes appelées depuis l'inspecteur pour tester
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
}