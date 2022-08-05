using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

	public GameObject deathEffect;

	public float health = 4f;

	public static int EnemiesAlive = 0;
	
	// public static int RewardGiven = 0;

	void Start ()
	{
		EnemiesAlive++;
		Debug.Log("start enemy " + EnemiesAlive);
		Instantiate(deathEffect, transform.position, Quaternion.identity);
		deathEffect.SetActive(false);
	}

	void OnCollisionEnter2D (Collision2D colInfo)
	{
		if (colInfo.relativeVelocity.magnitude > health)
		{
			Die();
		}
	}

	void Die ()
	{
		gameObject.SetActive(false);
		deathEffect.SetActive(true);
		EnemiesAlive--;
		// RewardGiven++;
		//Destroy(gameObject);
	}

	public void Respawn () 
	{
		gameObject.SetActive(true);
		//Destroy(deathEffect);
		deathEffect.SetActive(false);
		EnemiesAlive++; 
		

	}
}
