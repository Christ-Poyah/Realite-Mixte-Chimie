using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIScreenManager : MonoBehaviour
{
    [Header("Screen References")]
    [SerializeField] private GameObject categoryScreen;
    [SerializeField] private GameObject[] experimentScreenElements; // Changé en array
    
    [Header("Navigation UI")]
    [SerializeField] private Button backToCategoriesButton;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private TransitionType transitionType = TransitionType.SlideHorizontal;
    
    [Header("Script References")]
    [SerializeField] private DynamicCategoryCreator categoryCreator;
    [SerializeField] private DynamicExperimentCreator experimentCreator;
    [SerializeField] private ExperimentCategoryConnector connector;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private bool isTransitioning = false;
    private CurrentScreen currentScreen = CurrentScreen.Categories;
    
    public enum CurrentScreen
    {
        Categories,
        Experiments
    }
    
    public enum TransitionType
    {
        Fade,
        SlideHorizontal,
        SlideVertical,
        Scale
    }
    
    // Events
    public System.Action<CurrentScreen> OnScreenChanged;
    
    void Start()
    {
        InitializeScreenManager();
        SetupEventListeners();
        ShowCategoryScreen(false); // Show categories initially without animation
    }
    
    private void InitializeScreenManager()
    {
        // Validate screen references
        if (categoryScreen == null)
        {
            Debug.LogError("[UIScreenManager] Category Screen reference is missing!");
            return;
        }
        
        if (experimentScreenElements == null || experimentScreenElements.Length == 0)
        {
            Debug.LogError("[UIScreenManager] Experiment Screen Elements references are missing!");
            return;
        }
        
        // Validate each experiment screen element
        for (int i = 0; i < experimentScreenElements.Length; i++)
        {
            if (experimentScreenElements[i] == null)
            {
                Debug.LogError($"[UIScreenManager] Experiment Screen Element {i} reference is missing!");
            }
        }
        
        // Setup back button
        if (backToCategoriesButton != null)
        {
            backToCategoriesButton.onClick.RemoveAllListeners();
            backToCategoriesButton.onClick.AddListener(ShowCategoryScreen);
        }
        
        // Find references if not assigned
        if (categoryCreator == null)
            categoryCreator = FindObjectOfType<DynamicCategoryCreator>();
            
        if (experimentCreator == null)
            experimentCreator = FindObjectOfType<DynamicExperimentCreator>();
            
        if (connector == null)
            connector = FindObjectOfType<ExperimentCategoryConnector>();
    }
    
    private void SetupEventListeners()
    {
        // SUPPRIMÉ: Ne plus s'abonner directement aux événements de catégorie
        // Laisser le ExperimentCategoryConnector gérer la logique de filtrage
        // On s'abonne seulement à l'événement du connector si nécessaire
        
        // Optionnel: S'abonner à un événement custom du connector pour la navigation
        if (connector != null)
        {
            // Le connector pourra déclencher la navigation après avoir appliqué les filtres
        }
    }
    
    // NOUVELLE MÉTHODE: Appelée par le ExperimentCategoryConnector après filtrage
    public void NavigateToExperimentsAfterFiltering()
    {
        DebugLog("Navigating to experiments after category filtering");
        ShowExperimentScreen();
    }
    
    public void ShowCategoryScreen()
    {
        ShowCategoryScreen(true);
    }
    
    public void ShowCategoryScreen(bool animate)
    {
        if (isTransitioning && animate) return;
        
        DebugLog("Showing category screen");
        
        if (animate)
        {
            StartCoroutine(TransitionToScreen(new GameObject[] { categoryScreen }, experimentScreenElements, CurrentScreen.Categories));
        }
        else
        {
            categoryScreen.SetActive(true);
            SetExperimentScreenElementsActive(false);
            currentScreen = CurrentScreen.Categories;
            OnScreenChanged?.Invoke(currentScreen);
        }
    }
    
    public void ShowExperimentScreen()
    {
        ShowExperimentScreen(true);
    }
    
    public void ShowExperimentScreen(bool animate)
    {
        if (isTransitioning && animate) return;
        
        DebugLog("Showing experiment screen");
        
        if (animate)
        {
            StartCoroutine(TransitionToScreen(experimentScreenElements, new GameObject[] { categoryScreen }, CurrentScreen.Experiments));
        }
        else
        {
            SetExperimentScreenElementsActive(true);
            categoryScreen.SetActive(false);
            currentScreen = CurrentScreen.Experiments;
            OnScreenChanged?.Invoke(currentScreen);
        }
    }
    
    private void SetExperimentScreenElementsActive(bool active)
    {
        foreach (GameObject element in experimentScreenElements)
        {
            if (element != null)
            {
                element.SetActive(active);
            }
        }
    }
    
    private IEnumerator TransitionToScreen(GameObject[] targetScreens, GameObject[] currentScreens, CurrentScreen newScreen)
    {
        isTransitioning = true;
        
        // Prepare all screens
        foreach (GameObject screen in targetScreens)
        {
            if (screen != null) screen.SetActive(true);
        }
        foreach (GameObject screen in currentScreens)
        {
            if (screen != null) screen.SetActive(true);
        }
        
        // Get CanvasGroups and RectTransforms for all screens
        List<CanvasGroup> currentGroups = new List<CanvasGroup>();
        List<CanvasGroup> targetGroups = new List<CanvasGroup>();
        List<RectTransform> currentRects = new List<RectTransform>();
        List<RectTransform> targetRects = new List<RectTransform>();
        List<Vector3> currentOriginalPositions = new List<Vector3>();
        List<Vector3> targetOriginalPositions = new List<Vector3>();
        
        // Setup current screens
        foreach (GameObject screen in currentScreens)
        {
            if (screen != null)
            {
                CanvasGroup group = GetOrAddCanvasGroup(screen);
                RectTransform rect = screen.GetComponent<RectTransform>();
                currentGroups.Add(group);
                currentRects.Add(rect);
                currentOriginalPositions.Add(rect.anchoredPosition);
            }
        }
        
        // Setup target screens
        foreach (GameObject screen in targetScreens)
        {
            if (screen != null)
            {
                CanvasGroup group = GetOrAddCanvasGroup(screen);
                RectTransform rect = screen.GetComponent<RectTransform>();
                targetGroups.Add(group);
                targetRects.Add(rect);
                targetOriginalPositions.Add(rect.anchoredPosition);
            }
        }
        
        // Setup initial states based on transition type
        SetupTransitionInitialStates(currentGroups, targetGroups, currentRects, targetRects, false);
        
        // Perform transition
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / transitionDuration;
            float easedProgress = transitionCurve.Evaluate(progress);
            
            ApplyTransitionProgressToAll(currentGroups, targetGroups, currentRects, targetRects, 
                                       currentOriginalPositions, targetOriginalPositions, easedProgress);
            
            yield return null;
        }
        
        // Finalize transition
        ApplyTransitionProgressToAll(currentGroups, targetGroups, currentRects, targetRects, 
                                   currentOriginalPositions, targetOriginalPositions, 1f);
        
        // Cleanup - deactivate current screens and reset positions
        foreach (GameObject screen in currentScreens)
        {
            if (screen != null) screen.SetActive(false);
        }
        
        for (int i = 0; i < currentRects.Count; i++)
        {
            if (currentRects[i] != null)
                currentRects[i].anchoredPosition = currentOriginalPositions[i];
        }
        
        for (int i = 0; i < targetRects.Count; i++)
        {
            if (targetRects[i] != null)
                targetRects[i].anchoredPosition = targetOriginalPositions[i];
        }
        
        currentScreen = newScreen;
        isTransitioning = false;
        
        OnScreenChanged?.Invoke(currentScreen);
        DebugLog($"Transition completed to {newScreen}");
    }
    
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = obj.AddComponent<CanvasGroup>();
        }
        return group;
    }
    
    private void SetupTransitionInitialStates(List<CanvasGroup> currentGroups, List<CanvasGroup> targetGroups, 
                                            List<RectTransform> currentRects, List<RectTransform> targetRects, bool reverse)
    {
        switch (transitionType)
        {
            case TransitionType.Fade:
                foreach (CanvasGroup group in currentGroups)
                    if (group != null) group.alpha = reverse ? 0f : 1f;
                foreach (CanvasGroup group in targetGroups)
                    if (group != null) group.alpha = reverse ? 1f : 0f;
                break;
                
            case TransitionType.SlideHorizontal:
                float screenWidth = Screen.width;
                foreach (RectTransform rect in targetRects)
                {
                    if (rect != null)
                    {
                        float offsetX = reverse ? -screenWidth : screenWidth;
                        rect.anchoredPosition = new Vector3(offsetX, rect.anchoredPosition.y, 0);
                    }
                }
                break;
                
            case TransitionType.SlideVertical:
                float screenHeight = Screen.height;
                foreach (RectTransform rect in targetRects)
                {
                    if (rect != null)
                    {
                        float offsetY = reverse ? -screenHeight : screenHeight;
                        rect.anchoredPosition = new Vector3(rect.anchoredPosition.x, offsetY, 0);
                    }
                }
                break;
                
            case TransitionType.Scale:
                foreach (CanvasGroup group in currentGroups)
                    if (group != null) group.alpha = reverse ? 0f : 1f;
                foreach (CanvasGroup group in targetGroups)
                    if (group != null) group.alpha = reverse ? 1f : 0f;
                foreach (RectTransform rect in targetRects)
                    if (rect != null) rect.localScale = reverse ? Vector3.one : Vector3.zero;
                break;
        }
    }
    
    private void ApplyTransitionProgressToAll(List<CanvasGroup> currentGroups, List<CanvasGroup> targetGroups,
                                            List<RectTransform> currentRects, List<RectTransform> targetRects,
                                            List<Vector3> currentOriginalPositions, List<Vector3> targetOriginalPositions, float progress)
    {
        switch (transitionType)
        {
            case TransitionType.Fade:
                foreach (CanvasGroup group in currentGroups)
                    if (group != null) group.alpha = 1f - progress;
                foreach (CanvasGroup group in targetGroups)
                    if (group != null) group.alpha = progress;
                break;
                
            case TransitionType.SlideHorizontal:
                float screenWidth = Screen.width;
                for (int i = 0; i < currentRects.Count; i++)
                {
                    if (currentRects[i] != null && i < currentOriginalPositions.Count)
                    {
                        currentRects[i].anchoredPosition = Vector3.Lerp(currentOriginalPositions[i], 
                            new Vector3(-screenWidth, currentOriginalPositions[i].y, 0), progress);
                    }
                }
                for (int i = 0; i < targetRects.Count; i++)
                {
                    if (targetRects[i] != null && i < targetOriginalPositions.Count)
                    {
                        targetRects[i].anchoredPosition = Vector3.Lerp(
                            new Vector3(screenWidth, targetOriginalPositions[i].y, 0), targetOriginalPositions[i], progress);
                    }
                }
                break;
                
            case TransitionType.SlideVertical:
                float screenHeight = Screen.height;
                for (int i = 0; i < currentRects.Count; i++)
                {
                    if (currentRects[i] != null && i < currentOriginalPositions.Count)
                    {
                        currentRects[i].anchoredPosition = Vector3.Lerp(currentOriginalPositions[i], 
                            new Vector3(currentOriginalPositions[i].x, -screenHeight, 0), progress);
                    }
                }
                for (int i = 0; i < targetRects.Count; i++)
                {
                    if (targetRects[i] != null && i < targetOriginalPositions.Count)
                    {
                        targetRects[i].anchoredPosition = Vector3.Lerp(
                            new Vector3(targetOriginalPositions[i].x, screenHeight, 0), targetOriginalPositions[i], progress);
                    }
                }
                break;
                
            case TransitionType.Scale:
                foreach (CanvasGroup group in currentGroups)
                    if (group != null) group.alpha = 1f - progress;
                foreach (CanvasGroup group in targetGroups)
                    if (group != null) group.alpha = progress;
                foreach (RectTransform rect in targetRects)
                    if (rect != null) rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, progress);
                break;
        }
    }
    
    // Public methods for external control
    public void SetTransitionType(TransitionType newType)
    {
        transitionType = newType;
    }
    
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Max(0.1f, duration);
    }
    
    public CurrentScreen GetCurrentScreen()
    {
        return currentScreen;
    }
    
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    // Method to reset to categories (useful for external scripts)
    public void ResetToCategories()
    {
        if (categoryCreator != null)
        {
            categoryCreator.ClearSelection();
        }
        
        ShowCategoryScreen();
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[UIScreenManager] {message}");
        }
    }
    
    void OnDestroy()
    {
        // Cleanup event listeners
        if (backToCategoriesButton != null)
        {
            backToCategoriesButton.onClick.RemoveAllListeners();
        }
    }
}