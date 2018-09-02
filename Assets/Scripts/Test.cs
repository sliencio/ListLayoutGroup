using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class Test : MonoBehaviour {

	public ListLayoutGroup listViewVertical = null;
	public ListLayoutGroup listViewHorizontal = null;
	// Use this for initialization
	void Start () {
		listViewVertical.InitList (1000, (item, index) => {
			OnValueChange (item, index);
		});

		listViewHorizontal.InitList (1000, (item, index) => {
			OnValueChange (item, index);
		});
	}

	void OnValueChange (Transform item, int index) {
		item.GetComponentInChildren<Text> ().text = index.ToString ();
		item.GetComponent<Image>().color = new Color(Random.Range(0, 1f),Random.Range(0, 1f),Random.Range(0, 1f),1);
		
	}
}
