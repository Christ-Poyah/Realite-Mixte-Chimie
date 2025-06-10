using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class CreateDBScript : MonoBehaviour {

	public Text DebugText;

	// Use this for initialization
	void Start () {
		StartSync();
	}

    private void StartSync()
    {
        var ds = new DataService("existing.db");
        ds.CreateDB();
        
        var people = ds.GetAllExperiences(); // Corrigé : GetExperiences() -> GetAllExperiences()
        ToConsole (people);
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