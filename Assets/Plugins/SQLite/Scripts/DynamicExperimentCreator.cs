using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DynamicExperimentCreator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab; // Préfab du bouton avec TextMeshPro
    public Transform parentTransform; // Parent pour les boutons (ex : ScrollView Content)
    public Text debugText; // Optionnel : pour afficher les messages de debug
    
    [Header("Settings")]
    public float spacing = 10f; // Espacement entre les boutons
    public bool createDBOnStart = false; // Cocher pour recréer la DB au démarrage
    
    private DataService dataService;
    private List<Experiment> currentExperiments;

    void Start()
    {
        InitializeDatabase();
        SetupScrollViewContent();
        LoadAndCreateButtons();
    }

    void InitializeDatabase()
    {
        try
        {
            dataService = new DataService("existing.db");
            
            if (createDBOnStart)
            {
                dataService.CreateDB();
                ToDebug("Database created and populated with default experiments");
            }
            
            ToDebug("Database connection established");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing database: {e.Message}");
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
        if (dataService == null)
        {
            ToDebug("Error: DataService not initialized");
            return;
        }

        try
        {
            // Charger les expériences depuis la base de données
            currentExperiments = dataService.GetAllExperiments().ToList();
            ToDebug($"Loaded {currentExperiments.Count} experiments from database");

            if (currentExperiments.Count == 0)
            {
                ToDebug("No experiments found in database. Consider setting createDBOnStart to true.");
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
            newButton.name = $"ExperimentButton_{experiment.Id}";

            // Recherche des composants TextMeshProUGUI
            var leftText = newButton.transform.Find("LeftText")?.GetComponent<TextMeshProUGUI>();
            var rightText = newButton.transform.Find("RightText")?.GetComponent<TextMeshProUGUI>();

            // Mise à jour des textes
            if (leftText != null) 
            {
                leftText.text = experiment.Description;
            }
            else 
            {
                ToDebug($"Warning: LeftText (TextMeshProUGUI) not found on button for experiment {experiment.Id}");
            }

            if (rightText != null) 
            {
                rightText.text = experiment.Duration;
            }
            else 
            {
                ToDebug($"Warning: RightText (TextMeshProUGUI) not found on button for experiment {experiment.Id}");
            }

            // Configuration du clic sur le bouton
            var buttonComponent = newButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                int experimentId = experiment.Id; // Capture locale pour éviter les problèmes de closure
                buttonComponent.onClick.AddListener(() => OnExperimentButtonClick(experimentId));
            }
            else
            {
                ToDebug($"Warning: Button component not found on experiment button {experiment.Id}");
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
        var experiment = currentExperiments.FirstOrDefault(e => e.Id == experimentId);
        if (experiment != null)
        {
            ToDebug($"Experiment selected: {experiment.Description} ({experiment.Duration})");
            
            // Ici vous pouvez ajouter la logique pour lancer l'expérience
            // Par exemple :
            StartExperiment(experiment);
        }
        else
        {
            ToDebug($"Error: Experiment with ID {experimentId} not found");
        }
    }

    void StartExperiment(Experiment experiment)
    {
        // Ajoutez ici votre logique pour démarrer l'expérience
        // Par exemple, charger une scène spécifique, ouvrir un panel, etc.
        
        ToDebug($"Starting experiment: {experiment.Description}");
        
        // Exemple : Charger une scène basée sur l'ID de l'expérience
        // UnityEngine.SceneManagement.SceneManager.LoadScene($"Experiment_{experiment.Id}");
        
        // Ou ouvrir un panel spécifique
        // ShowExperimentPanel(experiment);
    }

    // Méthodes publiques pour la gestion dynamique
    public void RefreshExperiments()
    {
        LoadAndCreateButtons();
    }

    public void AddNewExperiment(string description, string duration, string category = "General")
    {
        if (dataService == null) return;

        var newExperiment = new Experiment
        {
            Description = description,
            Duration = duration,
            Category = category,
            IsActive = true
        };

        try
        {
            dataService.AddExperiment(newExperiment);
            ToDebug($"New experiment added: {description}");
            RefreshExperiments();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error adding experiment: {e.Message}");
        }
    }

    public void FilterExperimentsByCategory(string category)
    {
        if (dataService == null) return;

        try
        {
            if (string.IsNullOrEmpty(category) || category == "All")
            {
                currentExperiments = dataService.GetAllExperiments().ToList();
            }
            else
            {
                currentExperiments = dataService.GetExperimentsByCategory(category).ToList();
            }
            
            CreateButtons();
            ToDebug($"Filtered experiments by category: {category}. Found {currentExperiments.Count} experiments.");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error filtering experiments: {e.Message}");
        }
    }

    private void ToDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
        {
            debugText.text += System.Environment.NewLine + message;
        }
    }

    // Méthode appelée depuis l'inspecteur pour tester
    [ContextMenu("Test - Recreate Database")]
    public void RecreateDatabase()
    {
        createDBOnStart = true;
        InitializeDatabase();
        LoadAndCreateButtons();
    }

    [ContextMenu("Test - Add Sample Experiment")]
    public void AddSampleExperiment()
    {
        AddNewExperiment("Test Experiment", "5 min", "Test");
    }
}