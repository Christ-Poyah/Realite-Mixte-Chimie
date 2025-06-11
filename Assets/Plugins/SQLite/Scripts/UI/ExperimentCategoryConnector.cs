using UnityEngine;

public class ExperimentCategoryConnector : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private DynamicCategoryCreator categoryCreator;
    [SerializeField] private DynamicExperimentCreator experimentCreator;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

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
        
        if (experimentCreator != null)
        {
            // Filtrer les expériences par niveau
            experimentCreator.FilterExperimentsByNiveau(niveauId);
        }
    }

    private void OnTypeCategorySelected(int typeId)
    {
        DebugLog($"Type category selected: {typeId}");
        
        if (experimentCreator != null)
        {
            // Filtrer les expériences par type
            experimentCreator.FilterExperimentsByType(typeId);
        }
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
}