using UnityEngine;
using System.Collections;

public class ExperimentCategoryConnector : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private DynamicCategoryCreator categoryCreator;
    [SerializeField] private DynamicExperimentCreator experimentCreator;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Timing Settings")]
    [SerializeField] private float filterDelayBeforeNavigation = 0.1f; // Délai avant navigation

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

        // S'abonner aux événements de sélection de catégorie
        SetupCategoryEventListeners();
    }

    private void SetupCategoryEventListeners()
    {
        if (categoryCreator == null) return;

        // S'abonner aux événements
        categoryCreator.OnNiveauCategorySelected += OnNiveauCategorySelected;
        categoryCreator.OnTypeCategorySelected += OnTypeCategorySelected;

        DebugLog("Category event listeners setup completed");
    }

    private void OnNiveauCategorySelected(int niveauId)
    {
        DebugLog($"Niveau category selected: {niveauId}");
        
        // Appliquer le filtre puis naviguer
        StartCoroutine(FilterAndNavigate(() => {
            if (experimentCreator != null)
            {
                experimentCreator.FilterExperimentsByNiveau(niveauId);
            }
        }));
    }

    private void OnTypeCategorySelected(int typeId)
    {
        DebugLog($"Type category selected: {typeId}");
        
        // Appliquer le filtre puis naviguer
        StartCoroutine(FilterAndNavigate(() => {
            if (experimentCreator != null)
            {
                experimentCreator.FilterExperimentsByType(typeId);
            }
        }));
    }
    
    private IEnumerator FilterAndNavigate(System.Action filterAction)
    {
        // Appliquer le filtre d'abord
        filterAction?.Invoke();
        
        // Attendre un peu pour s'assurer que le filtrage est terminé
        yield return new WaitForSeconds(filterDelayBeforeNavigation);
        
        // Puis naviguer vers l'écran des expériences
        if (screenManager != null)
        {
            screenManager.NavigateToExperimentsAfterFiltering();
        }
    }
<<<<<<< Updated upstream
=======
    
    private void OnScreenChanged(UIScreenManager.CurrentScreen newScreen)
    {
        DebugLog($"Screen changed to: {newScreen}");
        
        // Logique supplémentaire selon l'écran actuel
        switch (newScreen)
        {
            case UIScreenManager.CurrentScreen.Categories:
                // Optionnel : réinitialiser les filtres ou autres actions
                break;
                
            case UIScreenManager.CurrentScreen.Experiments:
                // NE PAS rafraîchir ici car cela annulerait le filtre !
                // Le filtrage a déjà été appliqué avant la navigation
                DebugLog("Navigated to experiments screen - keeping current filter");
                break;
        }
    }
>>>>>>> Stashed changes

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
    }

    // Méthodes publiques pour contrôle externe
    public void ResetExperimentFilter()
    {
        if (experimentCreator != null)
        {
            experimentCreator.RefreshExperiments();
            DebugLog("Experiment filter reset - showing all experiments");
        }
    }

    public void FilterByCurrentSelection()
    {
        if (categoryCreator == null || experimentCreator == null) return;

        int selectedNiveauId = categoryCreator.GetSelectedNiveauId();
        int selectedTypeId = categoryCreator.GetSelectedTypeId();

        if (selectedNiveauId != -1)
        {
            experimentCreator.FilterExperimentsByNiveau(selectedNiveauId);
            DebugLog($"Filtered by current niveau selection: {selectedNiveauId}");
        }
        else if (selectedTypeId != -1)
        {
            experimentCreator.FilterExperimentsByType(selectedTypeId);
            DebugLog($"Filtered by current type selection: {selectedTypeId}");
        }
        else
        {
            ResetExperimentFilter();
        }
    }
<<<<<<< Updated upstream
=======
    
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
        if (experimentCreator != null)
        {
            experimentCreator.RefreshExperiments(); // Reset des filtres
        }
        
        if (screenManager != null)
        {
            screenManager.ShowExperimentScreen();
        }
    }
>>>>>>> Stashed changes
}