using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{

	[SerializeField] private Transform target;
	[SerializeField] private Camera cam;

	private Vector3 aim;
	private float farSight;
	private float turn = 0;

	public Vector3 offset = new Vector3 (0, 0, -10);
	public float movementSpeed = 0.2f;
	public float viewSize = 3;

	public static void Shake(float intensity)
	{
		Camera.main.GetComponent<CameraScript>().ShakeCam(intensity);
	}

	public void ShakeCam(float intensity) //10 is like taking damage
	{
		turn += intensity * (Random.Range(0, 2) * 2 - 1) / 2;
	}

	public void SetFarsight(float range)
	{
		farSight = range;
	}

	public void SetAim(Vector3 aim)
	{
		this.aim = aim;
	}

	public void SetCameraZoom(float zoom)
	{
		viewSize = Mathf.Clamp(zoom, 2, 8);
	}

	public void ChangeCameraZoom(float dir)
	{
		SetCameraZoom(GetCurrentCameraZoom() + dir * Time.deltaTime);
	}

	private float GetCurrentCameraZoom()
	{
		return viewSize;
	}

	private void Awake()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
	}

	private void FixedUpdate()
	{
		cam.transform.eulerAngles = new Vector3(0, 0, turn);
		turn = Mathf.Lerp(turn, 0, 0.2f);
		if (target)
		{
			Vector3 targetPos = target.position + offset + aim * farSight;
			cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, movementSpeed);
			cam.orthographicSize = viewSize;
		}
	}

}
