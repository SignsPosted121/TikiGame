using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBoss : BossBase
{
    public List<AI> enemies;
    private bool secondStage;

    new void Update()
    {
        base.Update();
        if (!secondStage && GetHealth() <= GetMaxHealth() / 2)
        {
            foreach (AI enemy in enemies) enemy.gameObject.SetActive(true);

            

            secondStage = true;
        }
    }
}
