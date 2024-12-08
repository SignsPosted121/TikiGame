using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

	private Player player;
	public Transform storage;

	public Transform Health;
	public Transform Stamina;

	private int lastHealth = 0;

	private void SetHealthBars()
	{
		Transform bar = Health.Find("Bar");
		foreach (Transform tick in bar)
		{
			Destroy(tick.gameObject);
		}
		Transform tick_prefab = storage.Find("Tick");
		for (int i = 0; i < player.GetMaxHealth(); i++)
		{
			Transform newTick = Instantiate(tick_prefab, bar);
			if (player.GetHealth() <= i)
			{
				newTick.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
			}
		}
	}

	private void SetStamina()
	{
		Transform bar = Stamina.Find("Bar");
		bar.GetChild(0).localScale = new Vector3(player.GetStamina() / player.GetMaxStamina(), 1, 1);
	}

	private void Update()
	{
		if (lastHealth != player.GetHealth())
		{
			lastHealth = player.GetHealth();
			SetHealthBars();
		}
		SetStamina();
	}

	private void Awake()
	{
		player = transform.parent.GetComponent<Player>();
	}

}
