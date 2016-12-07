﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.ImageEffects;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


public class VRPlayer : MonoBehaviour {
	[SerializeField]
	private GameObject cameraOB;
	public static Expression PlayerState;
	private static BloomAndFlares CameraBloom;

	private Vector3 BeforePlayerHeadRotation;
	private Vector3 AfterPlayerHeadRotation;

	private float HardShakeDurationTime = 0f;
	public float SwayPower = 0.01f;

	private bool BeforeShakeStateisHard = false;
	public static bool isControl = true;
	private IEnumerator[] ControlCoroutines;
	public List<GameObject> interactionItem;

	void Start(){
		DontDestroyOnLoad (transform.parent.gameObject);
		CameraBloom = GetComponent<BloomAndFlares> ();
		StartCoroutine (InputPlayerState ());
		StartCoroutine (Interaction ());
		//Application.targetFrameRate = 300;
	}

	private IEnumerator InputPlayerState(){
		while (true) {
			var Tick = Time.deltaTime;
			AfterPlayerHeadRotation = new Vector3 (cameraOB.transform.localRotation.x, transform.rotation.y,0);
			var HeadShakeSpeedofOneFrame = Mathf.Abs (Vector3.Distance (AfterPlayerHeadRotation,BeforePlayerHeadRotation))*Tick;
			bool AfterShakeStateisHard = HeadShakeSpeedofOneFrame >= 0.0005*Tick*Application.targetFrameRate ? true : false;

			if (AfterShakeStateisHard == true && BeforeShakeStateisHard == true) {
				HardShakeDurationTime += Tick;
			} else {
				HardShakeDurationTime = 0;
			}

			BeforePlayerHeadRotation = AfterPlayerHeadRotation;
			BeforeShakeStateisHard = AfterShakeStateisHard;
			yield return null;
		}
	}

	//VRリモコンのトリガーに反応してインタラクトする必要がある。　PS.読みにくい
	public IEnumerator Interaction(){
		Puppege PuppegeScript = FindObjectOfType<Puppege> ();
		GameObject puppege = PuppegeScript.gameObject;

		while (true) {
			GameObject interact = null;
			float Closest = Mathf.Infinity;
			foreach (GameObject item in interactionItem) {
				var EyeLine = Camera.main.WorldToViewportPoint (item.transform.position);

				if (isLookAt(EyeLine)) {
					float itemDistance = Math.Abs (EyeLine.x - 0.5f) + Math.Abs (EyeLine.y - 0.5f) + EyeLine.z;
					if (itemDistance < Closest) {
						Closest = itemDistance;
						interact = item;
					}
				}
				FlashOutEmissionColor (item);
			}
			yield return null;
			if (interact) {
				FlashInEmissionColor (interact);
				if (Input.GetKeyDown (KeyCode.E)) {
					if (interact == Story.objective) {
						interact.GetComponent<StoryObject> ().StoryCoroutine.MoveNext ();
						Story.NextSequential ().MoveNext ();
					} else {
						interact.GetComponent<Item> ().Interact (gameObject);
					}
				}
			}
			yield return null;
		}
	}

	private bool isLookAt(Vector3 EyeLine){
		return EyeLine.z > -0.1f && EyeLine.z < 3f && EyeLine.x >= 0.3f && EyeLine.y >= 0.3f && EyeLine.x <= 0.7f && EyeLine.y <= 0.7f;
	}

	private void FlashInEmissionColor(GameObject interact){
		if (!interact.GetComponent<Puppege> ()) {
			foreach (Material m in interact.GetComponent<Renderer> ().materials) {
				m.SetColor ("_EmissionColor", new Color (0.5f, 0.5f, 0.5f));
			}
		}
	}

	private void FlashOutEmissionColor(GameObject interact){
		if (!interact.GetComponent<Puppege> ()) {
			foreach (Material m in interact.GetComponent<Renderer> ().materials) {
				m.SetColor ("_EmissionColor", new Color (0f, 0f, 0f));
			}
		}
	}

	public IEnumerator SwayCamera(float time)
	{
		float SwayTime = 0;
		Vector3 NormalPos = transform.localPosition;
		while (SwayTime < time)
		{
			SwayTime += Time.deltaTime;

			Vector3 Amplitude = new Vector3(UnityEngine.Random.Range(-SwayPower, SwayPower),UnityEngine.Random.Range(-SwayPower, SwayPower), 0);
			transform.localPosition = new Vector3(NormalPos.x + Amplitude.x, NormalPos.y + Amplitude.y, transform.localPosition.z);
			yield return new WaitForSeconds(0.01f);
			yield return null;
		}
		transform.localPosition = NormalPos;
		yield return null;
	}

	public void SetControl(){
		isControl = true;
		FirstPersonController playerMove = FindObjectOfType<FirstPersonController> ();
		playerMove.m_WalkSpeed = 5;
		playerMove.m_RunSpeed = 10;
		playerMove.m_JumpSpeed = 10;
		//		for (int count = 0; count < Con
		//		for (int count = 0; count < ControlCoroutines.Length; count++) {
		////			StartCoroutine (ControlCoroutines [count]);
		//		}
	}

	public void ControlOut(){
		isControl = false;
		FirstPersonController playerMove = FindObjectOfType<FirstPersonController> ();
		playerMove.m_WalkSpeed = 0;
		playerMove.m_RunSpeed = 0;
		playerMove.m_JumpSpeed = 0;
		//		for (int count = 0; count < ControlCoroutines.Length; count++) {
		////			StopCoroutine (ControlCoroutines [count]);
		//		}
	}

	public IEnumerator Flash(float FlashPower,float FlashSpeed,string SceneName,Action<bool> FadeOut){
		float StartThreshold = CameraBloom.bloomThreshold;
		float StartIntensity = CameraBloom.bloomIntensity;

		while (CameraBloom.bloomThreshold > 0) {
			FlashIn (FlashPower, FlashSpeed);
			yield return null;
		}
		FadeOut (true);
		while(SceneName != SceneManager.GetActiveScene().name){
			yield return null;
		}

		while (CameraBloom.bloomIntensity > StartIntensity/3) {
			FlashOut(FlashPower, FlashSpeed);
			yield return null;
		}
		yield return null;
	}


	private void FlashIn(float FlashPower, float FlashSpeed){
		CameraBloom.bloomThreshold -= Time.deltaTime * (int)(1 / Time.timeScale) * FlashSpeed;
		CameraBloom.bloomIntensity += Time.deltaTime * FlashPower * (int)(1 / Time.timeScale) * FlashSpeed;
	}

	private void FlashOut(float FlashPower, float FlashSpeed){
		CameraBloom.bloomIntensity += Time.deltaTime * (int)(1 / Time.timeScale) * FlashSpeed;
		CameraBloom.bloomIntensity -= Time.deltaTime * FlashPower * (int)(1 / Time.timeScale) * FlashSpeed;
	}
}
