using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Event : MonoBehaviour
{

	[SerializeField] protected bool DEBUG = false;
	private bool triggered = false;

	public bool IsTriggered()
	{
		return triggered;
	}

	public void SetUsed()
	{
		triggered = true;
	}

	public virtual void Trigger()
	{
		if (!triggered)
		{
			triggered = true;
			if (DEBUG)
			{
				Debug.Log(this + " triggered.");
			}
		}
	}

}
