using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ExistingDBScript : MonoBehaviour {

	public Text DebugText;

	// Use this for initialization
	void Start () {
		var ds = new DataService ("existing.db");
		//ds.CreateDB ();
		var people = ds.GetExperiences ();
		ToConsole (people);

	}
	
	private void ToConsole(IEnumerable<Experience> people){
		foreach (var person in people) {
			ToConsole(person.ToString());
		}
	}

	private void ToConsole(string msg){
		DebugText.text += System.Environment.NewLine + msg;
		Debug.Log (msg);
	}

}
