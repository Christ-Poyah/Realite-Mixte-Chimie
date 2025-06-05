using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InitExperimentsDB : MonoBehaviour
{
    [Header("UI References")]
    public Text debugText;
    
    [Header("Database Settings")]
    public bool initializeOnStart = true;
    public bool resetDatabaseEachTime = false;

    private DataService dataService;

    void Start()
    {
        if (initializeOnStart)
        {
            InitializeDatabase();
        }
    }

    [ContextMenu("Initialize Database")]
    public void InitializeDatabase()
    {
        try
        {
            ToDebug("Initializing experiments database...");
            
            dataService = new DataService("existing.db");
            
            if (resetDatabaseEachTime)
            {
                dataService.CreateDB();
                ToDebug("Database reset and recreated with default experiments");
            }
            else
            {
                // Vérifier si des expériences existent déjà
                var existingExperiments = dataService.GetAllExperiments();
                var experimentsList = new List<Experiment>(existingExperiments);
                
                if (experimentsList.Count == 0)
                {
                    ToDebug("No experiments found. Creating default experiments...");
                    CreateDefaultExperiments();
                }
                else
                {
                    ToDebug($"Found {experimentsList.Count} existing experiments in database");
                }
            }
            
            // Afficher toutes les expériences
            DisplayAllExperiments();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error initializing database: {e.Message}");
        }
    }

    private void CreateDefaultExperiments()
    {
        var experiments = new[]
        {
            new Experiment
            {
                Description = "Expérience avec les alcanes",
                Duration = "10 min",
                Category = "Chimie Organique",
                IsActive = true
            },
            new Experiment
            {
                Description = "Réaction des alcools",
                Duration = "15 min",
                Category = "Chimie Organique",
                IsActive = true
            },
            new Experiment
            {
                Description = "Synthèse des esters",
                Duration = "20 min",
                Category = "Chimie Organique",
                IsActive = true
            },
            new Experiment
            {
                Description = "Oxydation des aldéhydes",
                Duration = "12 min",
                Category = "Chimie Organique",
                IsActive = true
            },
            new Experiment
            {
                Description = "Polymérisation",
                Duration = "25 min",
                Category = "Chimie des Polymères",
                IsActive = true
            },
            new Experiment
            {
                Description = "Cristallisation du sel",
                Duration = "30 min",
                Category = "Chimie Inorganique",
                IsActive = true
            },
            new Experiment
            {
                Description = "Électrolyse de l'eau",
                Duration = "18 min",
                Category = "Électrochimie",
                IsActive = true
            }
        };

        foreach (var experiment in experiments)
        {
            dataService.AddExperiment(experiment);
        }

        ToDebug($"Created {experiments.Length} default experiments");
    }

    private void DisplayAllExperiments()
    {
        if (dataService == null) return;

        try
        {
            var experiments = dataService.GetAllExperiments();
            ToDebug("=== ALL EXPERIMENTS ===");
            
            foreach (var experiment in experiments)
            {
                ToDebug($"ID: {experiment.Id} | {experiment.Description} | {experiment.Duration} | {experiment.Category}");
            }
            
            ToDebug("======================");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error displaying experiments: {e.Message}");
        }
    }

    [ContextMenu("Add Sample Experiment")]
    public void AddSampleExperiment()
    {
        if (dataService == null)
        {
            dataService = new DataService("existing.db");
        }

        var newExperiment = new Experiment
        {
            Description = "Nouvelle expérience test",
            Duration = "8 min",
            Category = "Test",
            IsActive = true
        };

        try
        {
            dataService.AddExperiment(newExperiment);
            ToDebug("Sample experiment added successfully");
            DisplayAllExperiments();
        }
        catch (System.Exception e)
        {
            ToDebug($"Error adding sample experiment: {e.Message}");
        }
    }

    [ContextMenu("Clear All Experiments")]
    public void ClearAllExperiments()
    {
        if (dataService == null)
        {
            dataService = new DataService("existing.db");
        }

        try
        {
            // Mettre tous les expériences à IsActive = false
            var experiments = dataService.GetAllExperiments();
            foreach (var experiment in experiments)
            {
                experiment.IsActive = false;
                dataService.UpdateExperiment(experiment);
            }
            
            ToDebug("All experiments have been deactivated");
        }
        catch (System.Exception e)
        {
            ToDebug($"Error clearing experiments: {e.Message}");
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
}