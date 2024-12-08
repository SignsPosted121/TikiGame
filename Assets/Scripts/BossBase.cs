using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBase : AI
{

	public AudioSource tempMusic;
	public ulong tempStart = 94;
	[SerializeField] private Transform bossUI;
	private Transform bossHealthBar;

	private void UpdateHealthBar()
	{
		bossHealthBar.GetChild(0).localScale = new Vector3((float) GetHealth() / GetMaxHealth(), 1, 1);
	}

	protected new void Update()
	{
		base.Update();
		UpdateHealthBar();
	}

	protected new void Awake()
	{
		base.Awake();
		bossHealthBar = bossUI.GetChild(0).Find("Health");
		tempMusic.Play();
		tempMusic.time = tempStart;
	}

}
