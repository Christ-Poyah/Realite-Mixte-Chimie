using UnityEngine;
using UnityEngine.UI;
using SQLite4Unity3d;
using System.IO;
using System;

public class SimpleDBCreator : MonoBehaviour
{
    public Text debugText;

    void Start()
    {
        CreateDatabase();
    }

    [ContextMenu("Create Database")]
    public void CreateDatabase()
    {
        try
        {
            ToDebug("Creating database in StreamingAssets...");
            
            // Chemin vers le fichier de base de données
            string dbPath = Path.Combine(Application.dataPath, "StreamingAssets", "existing.db");
            
            ToDebug($"Database path: {dbPath}");
            
            // Supprimer le fichier existant s'il y en a un
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                ToDebug("Deleted existing database file");
            }
            
            // Créer la connexion SQLite (cela créera le fichier)
            using (var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create))
            {
                // Créer la table Experience (ancienne)
                connection.CreateTable<Experience>();
                ToDebug("✓ Experience table created");
                
                // Créer la table Experiment (nouvelle)
                connection.CreateTable<Experiment>();
                ToDebug("✓ Experiment table created");
                
                // Insérer des données de test pour Experience
                var experiences = new[]
                {
                    new Experience
                    {
                        Id_Categorie = 1,
                        Libelle = "Test Experience 1",
                    },
                    new Experience
                    {
                        Id_Categorie = 2,
                        Libelle = "Test Experience 2",
                    }
                };
                
                connection.InsertAll(experiences);
                ToDebug($"✓ Inserted {experiences.Length} experiences");
                
                // Insérer des expériences chimiques
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
                    }
                };
                
                connection.InsertAll(experiments);
                ToDebug($"✓ Inserted {experiments.Length} experiments");
                
                // Vérifier les données
                var expCount = connection.Table<Experience>().Count();
                var experimentCount = connection.Table<Experiment>().Count();
                
                ToDebug($"✓ Database created successfully!");
                ToDebug($"  - Experiences: {expCount}");
                ToDebug($"  - Experiments: {experimentCount}");
            }
            
            // Rafraîchir Unity pour voir le fichier
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
            
            ToDebug("=== DATABASE CREATION COMPLETE ===");
            
        }
        catch (Exception e)
        {
            ToDebug($"❌ Error creating database: {e.Message}");
            ToDebug($"Stack trace: {e.StackTrace}");
        }
    }

    [ContextMenu("Test Database")]
    public void TestDatabase()
    {
        try
        {
            ToDebug("Testing database connection...");
            
            var dataService = new DataService("existing.db");
            
            // Test des expériences
            var experiments = dataService.GetAllExperiments();
            ToDebug("=== EXPERIMENTS ===");
            foreach (var exp in experiments)
            {
                ToDebug($"- {exp.Description} ({exp.Duration})");
            }
            
            // Test des experiences
            var experiences = dataService.GetExperiences();
            ToDebug("=== EXPERIENCES ===");
            foreach (var exp in experiences)
            {
                ToDebug($"- {exp.Libelle}");
            }
            
            ToDebug("✓ Database test successful!");
            
        }
        catch (Exception e)
        {
            ToDebug($"❌ Database test failed: {e.Message}");
        }
    }

    private void ToDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
        {
            debugText.text += Environment.NewLine + message;
        }
    }
}