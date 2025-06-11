using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class DynamicExperimentCreator : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;
    public Transform parentTransform;
    public Text debugText;
    
    [Header("Settings")]
    public float spacing = 10f;
    public float initializationDelay = 0.1f;
    public float buttonHeight = 60f;
    
    private DataService dataService;
    private List<ExperienceChimie> currentExperiments;
    private bool isInitialized = false;

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }
    
    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForSeconds(initializationDelay);
        
        int attempts = 0;
        const int maxAttempts = 50;
        
        while (attempts < maxAttempts)
        {
            if (DatabaseManager.Instance == null)
            {
                yield return new WaitForSeconds(0.2f);
                attempts++;
                continue;
            }
            
            if (DatabaseManager.Instance.IsDatabaseReady())
            {
                break;
            }
            
            yield return new WaitForSeconds(0.2f);
            attempts++;
        }
        
        if (attempts >= maxAttempts)
        {
            ToDebug("ERROR: Database initialization failed.");
            yield break;
        }
        
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f);
        
        InitializeExperimentCreator();
    }

    void InitializeExperimentCreator()
    {
        try
        {
            if (!ValidateUIReferences())
            {
                ToDebug("ERROR: UI References validation failed");
                return;
            }
            
            dataService = DatabaseManager.Instance.GetDataService();
            
            if (dataService == null)
            {
                ToDebug("ERROR: DataService is null");
                return;
            }
            
            isInitialized = true;
            
            SetupScrollViewContent();
            LoadAndCreateButtons();
            
            ToDebug("DynamicExperimentCreator initialized successfully");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing: {e.Message}");
        }
    }

    bool ValidateUIReferences()
    {
        if (buttonPrefab == null)
        {
            ToDebug("CRITICAL ERROR: ButtonPrefab is NULL");
            return false;
        }
        
        if (parentTransform == null)
        {
            ToDebug("CRITICAL ERROR: ParentTransform is NULL");
            return false;
        }
        
        return true;
    }

    void SetupScrollViewContent()
    {
        if (parentTransform == null) return;

        var layoutGroup = parentTransform.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = parentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);

        var sizeFitter = parentTransform.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = parentTransform.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var contentRect = parentTransform.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, 100f);
        }
    }

    void LoadAndCreateButtons()
    {
        if (!ValidateInitialization()) return;

        try
        {
            var experimentsEnumerable = dataService.GetAllExperiences();
            currentExperiments = experimentsEnumerable.ToList();
            
            ToDebug($"Loaded {currentExperiments.Count} experiments");

            if (currentExperiments.Count == 0)
            {
                ToDebug("No experiments found in database");
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
        if (!ValidateUIReferences()) return;

        ClearExistingButtons();

        for (int i = 0; i < currentExperiments.Count; i++)
        {
            var experiment = currentExperiments[i];
            
            try
            {
                CreateExperimentButton(experiment);
            }
            catch (System.Exception e)
            {
                ToDebug($"Error creating button for experiment {experiment.IdExp}: {e.Message}");
            }
        }

        StartCoroutine(ForceLayoutRefresh());
    }
    
    private void CreateExperimentButton(ExperienceChimie experiment)
    {
        var newButton = Instantiate(buttonPrefab, parentTransform);
        newButton.name = $"ExperimentButton_{experiment.IdExp}";
        newButton.SetActive(true);
        
        var buttonRect = newButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x, buttonHeight);
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
        }

        SetupButtonTexts(newButton, experiment);
        SetupButtonClick(newButton, experiment);
    }
    
    private void SetupButtonTexts(GameObject button, ExperienceChimie experiment)
    {
        var leftText = button.transform.Find("LeftText")?.GetComponent<TextMeshProUGUI>();
        var rightText = button.transform.Find("RightText")?.GetComponent<TextMeshProUGUI>();

        if (leftText != null) 
        {
            leftText.text = experiment.TitreExp;
            leftText.fontSize = 14f;
            leftText.alignment = TextAlignmentOptions.Left;
        }
        else 
        {
            var fallbackText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (fallbackText != null)
            {
                fallbackText.text = experiment.TitreExp;
            }
        }

        if (rightText != null) 
        {
            rightText.text = experiment.Duree;
            rightText.fontSize = 12f;
            rightText.alignment = TextAlignmentOptions.Right;
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
        }
    }
    
    private IEnumerator ForceLayoutRefresh()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();
        
        if (parentTransform != null)
        {
            var layoutGroup = parentTransform.GetComponent<VerticalLayoutGroup>();
            var sizeFitter = parentTransform.GetComponent<ContentSizeFitter>();
            
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
                yield return null;
                layoutGroup.enabled = true;
            }
            
            if (sizeFitter != null)
            {
                sizeFitter.enabled = false;
                yield return null;
                sizeFitter.enabled = true;
            }
            
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentTransform.GetComponent<RectTransform>());
        }
    }

    void ClearExistingButtons()
    {
        if (parentTransform == null) return;

        int childCount = parentTransform.childCount;

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
    }

    void OnExperimentButtonClick(int experimentId)
    {
        var experiment = currentExperiments?.FirstOrDefault(e => e.IdExp == experimentId);
        if (experiment != null)
        {
            ToDebug($"Experiment selected: {experiment.TitreExp}");
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
}