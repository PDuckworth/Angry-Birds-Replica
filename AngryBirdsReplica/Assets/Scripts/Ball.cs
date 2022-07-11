using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents; 
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class Ball : Agent {

	public Rigidbody2D rb;

	public Rigidbody2D hook;

	public float releaseTime = .15f;

	public float waitForEffectTime = 3.0f;

	public float maxDragDistance = 2f;

	private int numberOfThrows = 3; 

	private int throwNumber = 0; 

	private bool isPressed = false; 

	public int collectReward = 0;

	private int numberofEnemies = 3; 

	public static int EnemiesAlive;
	
	// public overide OnEpisodeBegin() {

	// 	// Debug.Log("OnEpisodeBegin");
	// 	// ResetBall();

	// 	// int numberofEnemies = Enemy.EnemiesAlive;
	// 	// Debug.Log("Enemies Alive = " + numberofEnemies);

	// 	// Debug.Log("request next decision");
	// 	// this.RequestDecision();
	// }

	void RestartEpisode() { 
		Debug.Log("Restart Episode");
		ResetBall();
		SetReward(0);
		numberofEnemies = 3; 
		throwNumber = 1;
	}


	void Update ()
	{
		if (isPressed)
		{
			Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
				rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;
			else
				rb.position = mousePos;	
		}

	}

	void OnMouseDown ()
	{
		isPressed = true;
		rb.isKinematic = true;
	}

	void OnMouseUp ()
	{
		isPressed = false;
		rb.isKinematic = false;

		StartCoroutine(Release());
		StopCoroutine(Release());
	}


	void ResetBall(){

		// reset ball position
		rb.velocity = Vector2.zero;
		rb.angularVelocity = 0f;
		rb.position = hook.position;

		// reattach the spring
		GetComponent<SpringJoint2D>().enabled = true;
		this.enabled = true;
	}

	void RewardAgent() {
		Debug.Log("reward");
		Debug.Log("NumberofEnemies = " + numberofEnemies);
		Debug.Log("Enemies Alive = " + Enemy.EnemiesAlive);

		int reward =  numberofEnemies - Enemy.EnemiesAlive; 
		Debug.Log("Set reward = " + reward);
		SetReward(reward); 
	}

	IEnumerator Release ()
	{
		Debug.Log("throw...");

		// After the action is taken: Release the spring
		yield return new WaitForSeconds(releaseTime);
		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;
		
		// wait for effect, then reset the ball position
		yield return new WaitForSeconds(waitForEffectTime);
		
		throwNumber+=1;
		RewardAgent();
		ResetBall();
		
		if (throwNumber>=numberOfThrows){
			Debug.Log("End Episode");
			EndEpisode();
			RestartEpisode();
			// Enemy.EnemiesAlive = 0;
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}


		// if (nextBall != null)
		// {
		// 	nextBall.SetActive(true);
		// } else
		// {
		// 	Enemy.EnemiesAlive = 0;
		// 	SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		// }
	}
	

		// if (Enemy.EnemiesAlive <= 0) { 
		// 	SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		// }


	
	// }

}
