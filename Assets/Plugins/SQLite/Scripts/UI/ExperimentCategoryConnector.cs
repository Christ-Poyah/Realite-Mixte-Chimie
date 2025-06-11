using UnityEngine;
using System.Collections;

public class ExperimentCategoryConnector : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private DynamicCategoryCreator categoryCreator;
    [SerializeField] private DynamicExperimentCreator experimentCreator;
    [SerializeField] private UIScreenManager screenManager;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Timing Settings")]
    [SerializeField] private float filterDelayBeforeNavigation = 0.2f; // Augmenté le délai

    // Stockage des titres des catégories sélectionnées
    private string selectedCategoryTitle = "";
    private int selectedCategoryId = -1;
    private CategoryType selectedCategoryType = CategoryType.None;

    public enum CategoryType
    {
        None,
        Niveau,
        Type
    }

    // Événements pour notifier du changement de titre
    public System.Action<string> OnCategoryTitleChanged;

    void Start()
    {
        // Vérifier que les références sont assignées
        if (categoryCreator == null)
        {
            categoryCreator = FindObjectOfType<DynamicCategoryCreator>();
            if (categoryCreator == null)
            {
                Debug.LogError("[ExperimentCategoryConnector] DynamicCategoryCreator not found!");
                return;
            }
        }

        if (experimentCreator == null)
        {
            experimentCreator = FindObjectOfType<DynamicExperimentCreator>();
            if (experimentCreator == null)
            {
                Debug.LogError("[ExperimentCategoryConnector] DynamicExperimentCreator not found!");
                return;
            }
        }

        if (screenManager == null)
        {
            screenManager = FindObjectOfType<UIScreenManager>();
            if (screenManager == null)
            {
                Debug.LogError("[ExperimentCategoryConnector] UIScreenManager not found!");
                return;
            }
        }

        // S'abonner aux événements de sélection de catégorie
        SetupCategoryEventListeners();
        
        // S'abonner aux événements du screen manager
        SetupScreenManagerEventListeners();
    }

    private void SetupCategoryEventListeners()
    {
        if (categoryCreator == null) return;

        // S'abonner aux événements
        categoryCreator.OnNiveauCategorySelected += OnNiveauCategorySelected;
        categoryCreator.OnTypeCategorySelected += OnTypeCategorySelected;

        DebugLog("Category event listeners setup completed");
    }
    
    private void SetupScreenManagerEventListeners()
    {
        if (screenManager == null) return;
        
        screenManager.OnScreenChanged += OnScreenChanged;
        DebugLog("Screen manager event listeners setup completed");
    }

    private void OnNiveauCategorySelected(int niveauId, string categoryTitle)
    {
        DebugLog($"Niveau category selected: {niveauId} - Title: {categoryTitle}");
        
        selectedCategoryId = niveauId;
        selectedCategoryTitle = categoryTitle;
        selectedCategoryType = CategoryType.Niveau;
        
        // Notifier du changement de titre
        OnCategoryTitleChanged?.Invoke(selectedCategoryTitle);
        
        // Appliquer le filtre puis naviguer
        StartCoroutine(FilterAndNavigate(() => {
            if (experimentCreator != null)
            {
                DebugLog($"Applying niveau filter: {niveauId}");
                experimentCreator.FilterExperimentsByNiveau(niveauId);
            }
        }));
    }

    private void OnTypeCategorySelected(int typeId, string categoryTitle)
    {
        DebugLog($"Type category selected: {typeId} - Title: {categoryTitle}");
        
        selectedCategoryId = typeId;
        selectedCategoryTitle = categoryTitle;
        selectedCategoryType = CategoryType.Type;
        
        // Notifier du changement de titre
        OnCategoryTitleChanged?.Invoke(selectedCategoryTitle);
        
        // Appliquer le filtre puis naviguer
        StartCoroutine(FilterAndNavigate(() => {
            if (experimentCreator != null)
            {
                DebugLog($"Applying type filter: {typeId}");
                experimentCreator.FilterExperimentsByType(typeId);
            }
        }));
    }
    
    private IEnumerator FilterAndNavigate(System.Action filterAction)
    {
        // S'assurer que l'expériment creator est prêt
        if (experimentCreator != null)
        {
            // Attendre que le frame actuel soit terminé
            yield return new WaitForEndOfFrame();
            
            // Appliquer le filtre
            filterAction?.Invoke();
            
            // Attendre que le filtrage soit appliqué
            yield return new WaitForSeconds(filterDelayBeforeNavigation);
            
            // Vérifier que le filtre a bien été appliqué
            DebugLog("Filter applied, navigating to experiments screen");
        }
        
        // Puis naviguer vers l'écran des expériences
        if (screenManager != null)
        {
            screenManager.NavigateToExperimentsAfterFiltering();
        }
    }
    
    private void OnScreenChanged(UIScreenManager.CurrentScreen newScreen)
    {
        DebugLog($"Screen changed to: {newScreen}");
        
        // Logique supplémentaire selon l'écran actuel
        switch (newScreen)
        {
            case UIScreenManager.CurrentScreen.Categories:
                // Réinitialiser les sélections quand on revient aux catégories
                ResetSelection();
                break;
                
            case UIScreenManager.CurrentScreen.Experiments:
                // NE PAS rafraîchir ici car cela annulerait le filtre !
                // Le filtrage a déjà été appliqué avant la navigation
                DebugLog($"Navigated to experiments screen - showing category: {selectedCategoryTitle}");
                break;
        }
    }

    private void ResetSelection()
    {
        selectedCategoryTitle = "";
        selectedCategoryId = -1;
        selectedCategoryType = CategoryType.None;
        OnCategoryTitleChanged?.Invoke("");
        DebugLog("Selection reset");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ExperimentCategoryConnector] {message}");
        }
    }

    void OnDestroy()
    {
        // Se désabonner des événements pour éviter les fuites mémoire
        if (categoryCreator != null)
        {
            categoryCreator.OnNiveauCategorySelected -= OnNiveauCategorySelected;
            categoryCreator.OnTypeCategorySelected -= OnTypeCategorySelected;
        }
        
        if (screenManager != null)
        {
            screenManager.OnScreenChanged -= OnScreenChanged;
        }
    }

    // Méthodes publiques pour contrôle externe
    public void ResetExperimentFilter()
    {
        if (experimentCreator != null)
        {
            experimentCreator.RefreshExperiments();
            DebugLog("Experiment filter reset - showing all experiments");
        }
        ResetSelection();
    }

    public void FilterByCurrentSelection()
    {
        if (selectedCategoryType == CategoryType.None || selectedCategoryId == -1)
        {
            ResetExperimentFilter();
            return;
        }

        if (experimentCreator == null) return;

        switch (selectedCategoryType)
        {
            case CategoryType.Niveau:
                experimentCreator.FilterExperimentsByNiveau(selectedCategoryId);
                DebugLog($"Re-applied niveau filter: {selectedCategoryId}");
                break;
            case CategoryType.Type:
                experimentCreator.FilterExperimentsByType(selectedCategoryId);
                DebugLog($"Re-applied type filter: {selectedCategoryId}");
                break;
        }
    }
    
    // Nouvelle méthode pour revenir aux catégories
    public void BackToCategories()
    {
        if (screenManager != null)
        {
            screenManager.ShowCategoryScreen();
        }
    }
    
    // Méthode pour aller directement aux expériences (avec reset du filtre)
    public void GoToExperimentsWithoutFilter()
    {
        ResetSelection();
        
        if (experimentCreator != null)
        {
            experimentCreator.RefreshExperiments(); // Reset des filtres
        }
        
        if (screenManager != null)
        {
            screenManager.ShowExperimentScreen();
        }
    }

    // Getters pour l'accès externe
    public string GetSelectedCategoryTitle() => selectedCategoryTitle;
    public int GetSelectedCategoryId() => selectedCategoryId;
    public CategoryType GetSelectedCategoryType() => selectedCategoryType;
}