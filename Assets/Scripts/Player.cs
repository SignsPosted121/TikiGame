using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Entity
{

	private Vector2 forward = Vector2.up;

	private CameraScript cam;

	private float dashCooldown = 0f;
	private float dashUse = 2f;

	private float stamina = 10f;
	private float maxStamina = 10f;
	private float staminaCooldown = 0f;

	private float healCooldown = 0f;

	public override void Damage(int damage, Vector2 push)
	{
		base.Damage(damage, push);
		if (damage > 0)
		{
			healCooldown = 7f;
			CameraScript.Shake(damage * 10);
		}
	}

	public void Move(InputAction.CallbackContext ctx)
	{
		SetMovement(ctx.ReadValue<Vector2>());
	}

	public void Dash(InputAction.CallbackContext ctx)
	{
		if (ctx.performed && GetSpeed().magnitude > 0 && dashCooldown <= 0 && stamina > 0)
		{
			dashCooldown = 0.5f;
			Push(GetSpeed().normalized, 30);
			UseStamina(dashUse);
		}
	}

	public void Primary(InputAction.CallbackContext ctx)
	{
		if (weapon != null) weapon.Primary(ctx);
		if (ctx.performed) CameraScript.Shake(0.5f);
	}

	public void Secondary(InputAction.CallbackContext ctx)
	{
		if (weapon != null) weapon.Secondary(ctx);
	}

	public void ChangeCameraZoom(InputAction.CallbackContext ctx)
	{
		if (ctx.performed) cam.ChangeCameraZoom(ctx.ReadValue<float>());
	}

	public void MousePos(InputAction.CallbackContext ctx)
	{
		if (weapon != null && Camera.main)
		{
			Vector3 worldPos = Camera.main.ScreenToWorldPoint(ctx.ReadValue<Vector2>());
			Vector2 dir = (worldPos - transform.position).normalized;
			forward = (dir * 1000).normalized;
			weapon.Aim(forward);
			cam.SetAim(dir);
		}
	}

	public override Vector2 GetForward()
	{
		return forward;
	}

	public void UseStamina(float damage)
	{
		stamina = Mathf.Clamp(stamina - damage, 0, maxStamina);
		if (damage > 0) staminaCooldown = 2f;
	}

	public float GetStamina()
	{
		return stamina;
	}

	public float GetMaxStamina()
	{
		return maxStamina;
	}

	private new void Awake()
	{
		base.Awake();
		cam = Camera.main.GetComponent<CameraScript>();
	}

	private new void Update()
	{
		base.Update();
		dashCooldown = Mathf.Clamp(dashCooldown - Time.deltaTime, 0, 4f);
		if (staminaCooldown <= 0) UseStamina(-5 * Time.deltaTime);
		if (healCooldown <= 0 && GetHealth() < GetMaxHealth())
		{
			Damage(-1);
			healCooldown = 1.5f;
		}
		staminaCooldown -= Time.deltaTime;
		healCooldown -= Time.deltaTime;
	}

}
