using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ExistingDBScript : MonoBehaviour {

	public Text DebugText;

	// Use this for initialization
	void Start () {
		var ds = new DataService ("existing.db");
		
		// Vérifier si les tables existent et les créer si nécessaire
		if (!CheckTablesExist(ds)) {
			ToConsole("Tables manquantes, création de la base de données...");
			ds.CreateDB();
			ToConsole("Base de données créée avec succès!");
		} else {
			ToConsole("Tables existantes trouvées.");
		}
		
		// Charger et afficher les expériences
		try {
			var people = ds.GetAllExperiences();
			ToConsole($"Nombre d'expériences trouvées: {System.Linq.Enumerable.Count(people)}");
			ToConsole(people);
		}
		catch (System.Exception e) {
			ToConsole("Erreur lors du chargement des expériences: " + e.Message);
		}
	}
	
	private bool CheckTablesExist(DataService ds) {
		try {
			// Essayer de compter les expériences pour vérifier si les tables existent
			var count = System.Linq.Enumerable.Count(ds.GetAllExperiences());
			return true;
		}
		catch {
			// Si une exception est levée, les tables n'existent probablement pas
			return false;
		}
	}
	
	private void ToConsole(IEnumerable<ExperienceChimie> people){
		foreach (var person in people) {
			ToConsole(person.ToString());
		}
	}

	private void ToConsole(string msg){
		DebugText.text += System.Environment.NewLine + msg;
		Debug.Log (msg);
	}
}