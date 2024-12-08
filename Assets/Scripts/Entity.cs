using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class Entity : MonoBehaviour, IFactions
{

	public Weapon weapon;
	public IFactions.Faction faction;

	[SerializeField] private int health = 3;
	private int maxHealth = 3;
	[SerializeField] private float speed = 2;

	private float bobble = 0;
	private float stun = 0;

	private Transform mask;
	private enum BobbleTypes { Tilt, JiggleV, JiggleH, Stretch, Breathe };
	[SerializeField] private BobbleTypes maskBobble = 0;
	[SerializeField] private float maskBobbleSpeed = 1;

	protected Vector2 moveDir;
	protected Rigidbody2D rb;
	private Animator animator;

	// Methods used by all entities
		 
	public int GetHealth()
	{
		return health;
	}

	public int GetMaxHealth()
	{
		return maxHealth;
	}

	protected virtual void Kill()
	{
		Destroy(gameObject);
		StopAllCoroutines();
	}

	public virtual void Damage(int damage)
	{
		Damage(damage, Vector2.zero);
	}

	public virtual void Damage(int damage, Vector2 push)
	{
		health = Mathf.Clamp(health - damage, 0, maxHealth);
		if (health <= 0)
		{
			Kill();
			return;
		}
		if (push.magnitude > 0)
		{
			Stun(damage / 10);
			SetVelocity(push);
		}
		if (damage > 0) StartCoroutine(DamageAnimation());
	}

	public void Stun(float stunTime)
	{
		stun += stunTime;
	}

	public void Push(Vector2 dir, float force)
	{
		SetVelocity(rb.velocity + dir * force);
	}

	public virtual Vector2 GetForward()
	{
		return Vector2.up;
	}

	public void SetWeapon(Weapon weapon)
	{
		this.weapon = weapon;
	}

	public bool IsSameFaction(Entity compareTo)
	{
		return IsSameFaction(compareTo.faction);
	}

	public bool IsSameFaction(IFactions.Faction compareTo)
	{
		if (compareTo == IFactions.Faction.NONE || faction == IFactions.Faction.NONE) return false;
		return faction == compareTo;
	}

	// Animation methods

	private IEnumerator DamageAnimation()
	{
		SpriteRenderer render = GetComponent<SpriteRenderer>();
		if (mask != null) render = mask.GetComponent<SpriteRenderer>();

		float tintAmount = 0.5f;
		Color hurt = new Color(0, tintAmount, tintAmount, 0);
		render.color -= hurt;

		while(hurt.g > 0)
		{
			Color change = new Color(0, -1, -1, 0) * Time.deltaTime * tintAmount;
			hurt += change;
			render.color -= change;

			yield return new WaitForEndOfFrame();
		}
		render.color += hurt * new Color(0, 1, 1, 0);
	}

	private void BobbleHead()
	{
		bobble = Mathf.Repeat(bobble + Time.deltaTime * Mathf.PI * maskBobbleSpeed, Mathf.PI * 2);
		bobble = Mathf.Repeat(bobble + Time.deltaTime * rb.velocity.magnitude * Mathf.PI * 2 * maskBobbleSpeed, Mathf.PI * 2);
		switch (maskBobble)
		{
			case BobbleTypes.Breathe:
				mask.localScale = new Vector3(Mathf.Sin(bobble) * 0.05f + 1, Mathf.Sin(bobble) * 0.05f + 1, 1);
				break;
			case BobbleTypes.Stretch:
				mask.localScale = new Vector3(Mathf.Clamp(-Mathf.Sin(bobble) * 0.1f, 0, 1) + 1, Mathf.Clamp(Mathf.Sin(bobble) * 0.1f, 0 , 1) + 1, 1);
				break;
			case BobbleTypes.JiggleH:
				mask.localPosition = new Vector3(Mathf.Sin(bobble) * 0.035f, 0);
				break;
			case BobbleTypes.JiggleV:
				mask.localPosition = new Vector3(0, Mathf.Sin(bobble) * 0.035f);
				break;
			default:
				mask.eulerAngles = new Vector3(0, 0, Mathf.Sin(bobble) * 10);
				break;
		}
	}

	// Movement control

	private void SetVelocity(Vector2 velocity)
	{
		rb.velocity = velocity;
	}

	public Vector2 GetSpeed()
	{
		return rb.velocity;
	}

	protected void SetMovement(Vector2 dir)
	{
		moveDir = (dir * 100000).normalized;
	}

	private Vector2 ClampVelocity(Vector2 velocity, float clamp)
	{
		Vector2 absVelocity = new Vector2(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y));
		if (absVelocity.magnitude > clamp)
		{
			float over = absVelocity.magnitude - clamp;
			float multiplier = absVelocity.magnitude / over;
			velocity /= multiplier;
		}
		return velocity;
	}

	// Core

	protected void Awake()
	{
		maxHealth = health;
		rb = GetComponent<Rigidbody2D>();
		animator = transform.GetChild(0).GetComponent<Animator>();
		mask = transform.Find("Mask");
	}

	protected void FixedUpdate()
	{
		if (!rb)
		{
			Vector3 push = new Vector3(moveDir.x, moveDir.y, 0);
			transform.position += speed * Time.fixedDeltaTime * push;
		} else
		{
			Vector2 movement = speed * Time.fixedDeltaTime * 8 * moveDir;
			Vector2 drag = Time.fixedDeltaTime * 8 * rb.velocity;
			if (stun <= 0) rb.velocity = ClampVelocity(rb.velocity + movement - drag, speed);
			else rb.velocity -= drag;
		}
	}

	protected void Update()
	{
		stun = Mathf.Clamp(stun - Time.deltaTime, 0, 1f);
		if (rb.velocity.magnitude >= 0.25f) animator.SetBool("Moving", true);
		else animator.SetBool("Moving", false);

		if (mask != null) BobbleHead();
	}

}
