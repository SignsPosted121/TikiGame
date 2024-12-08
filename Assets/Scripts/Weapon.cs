using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{

	protected Entity owner;
	[SerializeField] private GameObject effect;
	protected enum WeaponType { Melee, Ranged };
	[SerializeField] protected WeaponType type;

	[SerializeField] private int damage = 1;
	[SerializeField] protected int weight = 20;
	[SerializeField] private float speed = 1;
	[SerializeField, Range(15, 360)] private float sweep = 90;
	[SerializeField, Range(1, 5)] private float range = 1.5f;
	[SerializeField] private float staminaUse = 0.5f;
	protected Vector2 aim;

	private float cooldown = 0;
	[SerializeField] private float recoveryTime = 0;
	private bool swinging = false;

	// Link up methods

	public virtual void Primary()
	{
		return;
	}

	public virtual void Primary(InputAction.CallbackContext ctx)
	{
		return;
	}

	public virtual void Secondary(InputAction.CallbackContext ctx)
	{
		return;
	}

	public void Aim(Vector2 dir)
	{
		if (dir.magnitude > 0 && !swinging)
		{
			float arc = Mathf.Atan2(dir.y, dir.x);
			arc += sweep / 2 * Mathf.Deg2Rad;
			bool flip = false;
			if (dir.x < 0)
			{
				flip = true;
				arc += 180;
			}
			Vector2 rest = new Vector2(Mathf.Cos(arc), Mathf.Sin(arc));
			transform.up = rest;
			GetComponent<SpriteRenderer>().flipX = flip; // All of this just moves the weapon back according to sweep (angle of hit)

			aim = dir;
		}
	}

	public void Equip()
	{
		owner = transform.parent.GetComponent<Entity>();
		owner.SetWeapon(this);
		if (type == WeaponType.Ranged)
		{
			Camera.main.GetComponent<CameraScript>().SetFarsight(range);
		} else
		{
			Camera.main.GetComponent<CameraScript>().SetFarsight(0);
		}
	}

	// Methods for every tool that will be used everywhere

	public int GetDamage()
	{
		return damage;
	}

	public float GetCooldown()
	{
		return cooldown;
	}

	public float GetAttackSpeed()
	{
		return speed;
	}

	// Helpful methods for every tool such as swings

	protected bool IsInList(List<Transform> list, Transform obj)
	{
		bool found = false;
		foreach (Transform parsed in list)
		{
			if (parsed == obj)
			{
				found = true;
			}
		}
		return found;
	}

	private List<Transform> ParseMultiSwing(Vector2 dir, float arc)
	{
		List<Transform> hits = new List<Transform>();
		dir = dir.normalized;
		float dirArc = Mathf.Atan2(dir.y, dir.x);

		owner.GetComponent<Collider2D>().enabled = false;

		for (float i = -arc / 2; i <= arc / 2; i += 15f * Mathf.Deg2Rad)
		{
			Vector2 fixedDir = new Vector2(Mathf.Cos(i + dirArc), Mathf.Sin(i + dirArc));
			RaycastHit2D hit = Physics2D.Raycast(owner.transform.position, fixedDir, range, ~LayerMask.GetMask("Map"));
			if (hit && hit.transform)
			{
				if (!IsInList(hits, hit.transform))
				{
					hit.transform.GetComponent<Collider2D>().enabled = false;
					hits.Add(hit.transform);
				}
			}
		}

		foreach(Transform hit in hits)
		{
			hit.GetComponent<Collider2D>().enabled = true;
		}

		owner.GetComponent<Collider2D>().enabled = true;

		return hits;
	} /// Core method that registers hits!

	private Transform ParseSwing(Vector2 dir, float arc)
	{
		List<Transform> hits = ParseMultiSwing(dir, arc);
		Transform closest = null;
		if (hits.Count <= 0)
		{
			return null;
		}
		else if (hits.Count == 1)
		{
			return hits[0];
		}
		else
		{
			foreach (Transform hit in hits)
			{
				if (closest)
				{
					if ((closest.position - owner.transform.position).magnitude > (hit.position - owner.transform.position).magnitude)
					{
						closest = hit;
					}
				}
				else
				{
					closest = hit;
				}
			}
		}
		return closest;
	} /// Gets closest from multi

	protected Vector2 ArcToDir(float arc)
	{
		return new Vector2(Mathf.Cos(arc), Mathf.Sin(arc));
	}

	// Animation methods
	
	private void PlayAnimation(Vector2 dir, float arc, string anim)
	{
		switch (anim)
		{
			default:
				StartCoroutine(HorizontalSwing(dir, arc));
				break;
		}
		StartCoroutine(Effect(dir, effect));
	} /// Core animation method (defaults to HorizontalSwing)

	private IEnumerator HorizontalSwing(Vector2 dir, float arc)
	{
		if (swinging)
		{
			yield return null;
		}
		swinging = true;
		float sign = -1;
		if (dir.x < 0)
		{
			sign = 1;
		}
		float dirArc = Mathf.Atan2(dir.y, dir.x);
		float currentArc = -arc / 2;
		while (currentArc <= arc / 2)
		{
			transform.up = ArcToDir(currentArc * sign + dirArc);
			currentArc += Mathf.PI * 2 * Time.deltaTime / speed;
			yield return new WaitForEndOfFrame();
		}
		transform.up = ArcToDir(arc * sign / 2 + dirArc);
		while (cooldown > 0)
		{
			yield return new WaitForEndOfFrame();
		}
		transform.up = ArcToDir(-arc * sign / 2 + dirArc);
		swinging = false;
	}

	private IEnumerator Effect(Vector2 dir, GameObject effectPrefab) /// Effects pop out here!
	{
		Transform effect = Instantiate(effectPrefab, owner.transform.parent).transform;
		Vector3 forward = ArcToDir(Mathf.Atan2(dir.y, dir.x)); // True forward (image and object is rotated -45 degrees)

		effect.up = ArcToDir(Mathf.Atan2(dir.y, dir.x) + 45 * Mathf.Deg2Rad); // Image forward (rotated 45 degrees)

		float size = (owner.transform.localScale.x - 1) / 4;

		Vector3 startPos = owner.transform.position + forward * range + forward * size;
		effect.position = startPos;
		effect.localScale *= (sweep / 90);

		float startTimer = 0.3f;
		float timer = startTimer;

		Destroy(effect.gameObject, startTimer + 0.01f);

		float rangeOffset = range - size;

		while (timer > 0)
		{
			if (effect == null) break;

			effect.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, Mathf.Pow(1, timer / startTimer));
			effect.position = startPos + (1 - Mathf.Pow(rangeOffset, timer / startTimer)) * rangeOffset * forward;
			effect.localScale += (sweep / 90) * Time.deltaTime * Vector3.one;

			timer -= Time.deltaTime;

			yield return new WaitForEndOfFrame();
		}
	}

	// Swing (Use) methods

	protected bool CanSwing()
	{
		if (owner.CompareTag("Player"))
		{
			Player player = owner.GetComponent<Player>();
			if (player.GetStamina() < staminaUse)
			{
				return false;
			}
			player.UseStamina(staminaUse);
		}
		return true;
	}

	private void SwingMaster(Vector2 dir, string swingType)
	{
		cooldown = speed;
		PlayAnimation(dir, sweep * Mathf.Deg2Rad, swingType);
		owner.GetComponent<Entity>().Stun(recoveryTime);
	}

	public Transform SwingSingle(Vector2 dir)
	{
		SwingMaster(dir, "Horizontal");
		return ParseSwing(dir, sweep);
	}

	public List<Transform> SwingMulti(Vector2 dir)
	{
		SwingMaster(dir, "Horizontal");
		return ParseMultiSwing(dir, sweep);
	}

	// Core

	public void Update()
	{
		cooldown -= Time.deltaTime;
	}

}
