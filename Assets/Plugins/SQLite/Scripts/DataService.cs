using SQLite4Unity3d;
using UnityEngine;
#if !UNITY_EDITOR
using System.Collections;
using System.IO;
#endif
using System.Collections.Generic;

public class DataService  {

	private SQLiteConnection _connection;

	public DataService(string DatabaseName){

#if UNITY_EDITOR
            var dbPath = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);
#else
        // check if file exists in Application.persistentDataPath
        var filepath = string.Format("{0}/{1}", Application.persistentDataPath, DatabaseName);

        if (!File.Exists(filepath))
        {
            Debug.Log("Database not in Persistent path");
            // if it doesn't ->
            // open StreamingAssets directory and load the db ->

#if UNITY_ANDROID 
            var loadDb = new WWW("jar:file://" + Application.dataPath + "!/assets/" + DatabaseName);  // this is the path to your StreamingAssets in android
            while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
            // then save to Application.persistentDataPath
            File.WriteAllBytes(filepath, loadDb.bytes);
#elif UNITY_IOS
                 var loadDb = Application.dataPath + "/Raw/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);
#elif UNITY_WP8
                var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);

#elif UNITY_WINRT
		var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
		
#elif UNITY_STANDALONE_OSX
		var loadDb = Application.dataPath + "/Resources/Data/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
#else
	var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
	// then save to Application.persistentDataPath
	File.Copy(loadDb, filepath);

#endif

            Debug.Log("Database written");
        }

        var dbPath = filepath;
#endif
            _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        Debug.Log("Final PATH: " + dbPath);     

	}

	public void CreateDB()
	{
		// Création des tables existantes
		_connection.DropTable<Experience>();
		_connection.CreateTable<Experience>();

		// Création de la nouvelle table Experiment
		_connection.DropTable<Experiment>();
		_connection.CreateTable<Experiment>();

		// Insertion des données existantes Experience (si nécessaire)
		_connection.InsertAll(new[]{
			new Experience{
				Id_Experiences = 1,
				Id_Categorie = 1,
				Libelle = "Perez",
			},
		});

		// Insertion des expériences chimiques
		InsertDefaultExperiments();
	}

	private void InsertDefaultExperiments()
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
			}
		};

		_connection.InsertAll(experiments);
		Debug.Log($"Inserted {experiments.Length} experiments into database");
	}

	// Méthodes existantes pour Experience
	public IEnumerable<Experience> GetExperiences(){
		return _connection.Table<Experience>();
	}

	public IEnumerable<Experience> GetExperienceX(){
		return _connection.Table<Experience>().Where(x => x.Libelle == "Roberto");
	}

	public Experience GetJohnny(){
		return _connection.Table<Experience>().Where(x => x.Libelle == "Johnny").FirstOrDefault();
	}

	public Experience CreateExperiences(){
		var p = new Experience{
				Id_Categorie = 1,
				Libelle = "Mnemonic",
				Id_Experiences = 1
		};
		_connection.Insert (p);
		return p;
	}

	// Nouvelles méthodes pour Experiment
	public IEnumerable<Experiment> GetAllExperiments()
	{
		return _connection.Table<Experiment>().Where(x => x.IsActive == true);
	}

	public IEnumerable<Experiment> GetExperimentsByCategory(string category)
	{
		return _connection.Table<Experiment>().Where(x => x.Category == category && x.IsActive == true);
	}

	public Experiment GetExperimentById(int id)
	{
		return _connection.Table<Experiment>().Where(x => x.Id == id).FirstOrDefault();
	}

	public int AddExperiment(Experiment experiment)
	{
		return _connection.Insert(experiment);
	}

	public int UpdateExperiment(Experiment experiment)
	{
		return _connection.Update(experiment);
	}

	public int DeleteExperiment(int id)
	{
		var experiment = GetExperimentById(id);
		if (experiment != null)
		{
			experiment.IsActive = false;
			return _connection.Update(experiment);
		}
		return 0;
	}
}