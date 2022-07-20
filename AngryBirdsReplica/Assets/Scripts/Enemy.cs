using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

	public GameObject deathEffect;

	public float health = 4f;

	public static int EnemiesAlive = 0;
	
	public static int RewardGiven = 0;

	void Start ()
	{
		EnemiesAlive++;
		Debug.Log("start enemy " + EnemiesAlive);
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
		Instantiate(deathEffect, transform.position, Quaternion.identity);

		EnemiesAlive--;

		RewardGiven++;
		if (EnemiesAlive <= 0)
			Debug.Log("LEVEL WON!");

		Destroy(gameObject);
	}

}
