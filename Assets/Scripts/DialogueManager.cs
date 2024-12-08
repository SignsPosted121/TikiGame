using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{

	[SerializeField] private TextMeshProUGUI text;

	public void StartDialogue(Event_Dialogue dialogue)
	{
		text.text = dialogue.lines[0];
	}

}
