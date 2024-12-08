using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSword : Weapon
{

	private void Attack()
	{
		if (GetCooldown() > 0 || !CanSwing() || owner == null) return;

		List<Transform> hits = SwingMulti(aim);
		bool didHit = false;

		if (hits.Count > 0) 
			foreach (Transform hit in hits)
			{
				Entity stats = hit.GetComponent<Entity>();
				if (stats != null && !owner.IsSameFaction(stats))
				{
					didHit = true;
					Vector2 push = (hit.position - owner.transform.position).normalized * weight;
					stats.Damage(GetDamage(), push);
				}
			}

		if (didHit)
		{
			if (owner.CompareTag("Player"))
				CameraScript.Shake(3);
		}
		/* example code for hitting one person!
		if (hit && hit.GetComponent<Entity>())
		{
			Entity stats = hit.GetComponent<Entity>();
			Vector2 push = (hit.position - player.position).normalized * power;
			stats.Damage(power, push);
			Damage(1);
		} */
	}

	public override void Primary()
	{
		Attack();
	}

	public override void Primary(InputAction.CallbackContext ctx)
	{
		if (ctx.performed) Primary();
	} /// Used by the player only

	private void Awake()
	{
		Equip();
	}

}
