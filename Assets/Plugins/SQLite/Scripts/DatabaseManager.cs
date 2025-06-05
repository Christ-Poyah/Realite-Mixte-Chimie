using UnityEngine;
using System.IO;
using SQLite4Unity3d;

public class DatabaseManager : MonoBehaviour
{
    [Header("Database Settings")]
    public string databaseName = "chemistry_experiments.db";
    public bool forceRecreateDatabase = false;
    public bool createDatabaseOnStart = true;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private DataService dataService;
    private string databasePath;
    
    // Singleton pattern pour accès global
    public static DatabaseManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        if (createDatabaseOnStart)
        {
            InitializeDatabase();
        }
    }
    
    public void InitializeDatabase()
    {
        try
        {
            DebugLog("Starting database initialization...");
            
            // Vérifier si la base de données doit être recréée
            if (forceRecreateDatabase)
            {
                DeleteExistingDatabase();
            }
            
            // Créer le service de données
            dataService = new DataService(databaseName);
            DebugLog("DataService created successfully");
            
            // Vérifier si la base de données contient des données
            if (ShouldCreateTables())
            {
                DebugLog("Creating database tables and inserting default data...");
                dataService.CreateDB();
                DebugLog("Database created and populated successfully");
            }
            else
            {
                DebugLog("Database already exists with data");
            }
            
            // Vérifier l'intégrité des données
            VerifyDatabaseIntegrity();
            
        }
        catch (System.Exception e)
        {
            DebugLog($"ERROR during database initialization: {e.Message}");
            DebugLog($"Stack trace: {e.StackTrace}");
            
            // Tentative de récupération
            TryRecoverDatabase();
        }
    }
    
    private void DeleteExistingDatabase()
    {
        try
        {
            GetDatabasePath();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
                DebugLog($"Existing database deleted: {databasePath}");
            }
        }
        catch (System.Exception e)
        {
            DebugLog($"Error deleting existing database: {e.Message}");
        }
    }
    
    private void GetDatabasePath()
    {
#if UNITY_EDITOR
        databasePath = Path.Combine(Application.dataPath, "StreamingAssets", databaseName);
#else
        databasePath = Path.Combine(Application.persistentDataPath, databaseName);
#endif
    }
    
    private bool ShouldCreateTables()
    {
        try
        {
            // Vérifier si des expériences existent déjà
            var experiments = dataService.GetAllExperiences();
            int count = 0;
            foreach (var exp in experiments)
            {
                count++;
                if (count > 0) break; // On a trouvé au moins une expérience
            }
            
            return count == 0; // Créer les tables seulement si aucune expérience n'existe
        }
        catch
        {
            // Si erreur lors de la vérification, on crée les tables
            return true;
        }
    }
    
    private void VerifyDatabaseIntegrity()
    {
        try
        {
            var niveaux = dataService.GetAllCategoriesNiveau();
            var types = dataService.GetAllCategoriesType();
            var experiences = dataService.GetAllExperiences();
            
            int niveauCount = 0, typeCount = 0, expCount = 0;
            
            foreach (var n in niveaux) niveauCount++;
            foreach (var t in types) typeCount++;
            foreach (var e in experiences) expCount++;
            
            DebugLog($"Database integrity check:");
            DebugLog($"- Categories Niveau: {niveauCount}");
            DebugLog($"- Categories Type: {typeCount}");
            DebugLog($"- Experiences: {expCount}");
            
            if (niveauCount == 0 || typeCount == 0 || expCount == 0)
            {
                DebugLog("WARNING: Some tables appear to be empty. Consider recreating the database.");
            }
        }
        catch (System.Exception e)
        {
            DebugLog($"Error during integrity check: {e.Message}");
        }
    }
    
    private void TryRecoverDatabase()
    {
        DebugLog("Attempting database recovery...");
        
        try
        {
            // Supprimer la base de données corrompue
            DeleteExistingDatabase();
            
            // Recréer
            dataService = new DataService(databaseName);
            dataService.CreateDB();
            
            DebugLog("Database recovery successful");
        }
        catch (System.Exception e)
        {
            DebugLog($"Database recovery failed: {e.Message}");
        }
    }
    
    public DataService GetDataService()
    {
        if (dataService == null)
        {
            DebugLog("DataService is null, attempting to initialize...");
            InitializeDatabase();
        }
        
        return dataService;
    }
    
    public bool IsDatabaseReady()
    {
        bool isReady = dataService != null;
        DebugLog($"IsDatabaseReady called: {isReady}");
        return isReady;
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DatabaseManager] {message}");
        }
    }
    
    // Méthodes publiques pour gestion depuis l'inspecteur
    [ContextMenu("Force Recreate Database")]
    public void ForceRecreateDatabase()
    {
        forceRecreateDatabase = true;
        InitializeDatabase();
        forceRecreateDatabase = false;
    }
    
    [ContextMenu("Verify Database")]
    public void VerifyDatabase()
    {
        if (dataService != null)
        {
            VerifyDatabaseIntegrity();
        }
        else
        {
            DebugLog("Database not initialized");
        }
    }
    
    [ContextMenu("Show Database Path")]
    public void ShowDatabasePath()
    {
        GetDatabasePath();
        DebugLog($"Database path: {databasePath}");
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && dataService != null)
        {
            // Sauvegarder/fermer proprement si nécessaire
            DebugLog("Application paused - database safe");
        }
    }
}