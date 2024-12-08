using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Event_Dialogue : Event
{

    private static GameObject dialogue_prefab;
    public string[] lines;

    void Awake()
    {
        dialogue_prefab = (GameObject) Resources.Load("DialogueManager");
    }

	public override void Trigger()
	{
		base.Trigger();
        Transform dialogue = Instantiate(dialogue_prefab, transform).transform;
        dialogue.GetComponent<DialogueManager>().StartDialogue(this);
    }
}
