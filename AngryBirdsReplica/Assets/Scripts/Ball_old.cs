using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents; 
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class Ball_old : Agent {

	public Rigidbody2D rb;

	public Rigidbody2D hook;

	public float releaseTime = .15f;

	public float waitForEffectTime = 2.0f;

	public float maxDragDistance = 10f;

	public int numberofEnemies; 

	[SerializeField] private Transform enemyTransform;

	private int numberOfThrows = 3; 

	private int throwNumber = 1; 

	private bool isPressed = false; 

	public override void OnEpisodeBegin() {
		
		// public int EnemiesAlive = Enemy.EnemiesAlive; 
		Debug.Log("OnEpisodeBegin");
		Debug.Log("Enemies Alive = " + Enemy.EnemiesAlive);

		numberofEnemies = Enemy.EnemiesAlive;

		ResetBallPosition();
		Debug.Log("request next decision");
		this.RequestDecision();
	}

	// public override void CollectObservations(VectorSensor sensor) {

	// 	if (needToAct){

	// 		sensor.AddObservation(transform.position);
	// 		sensor.AddObservation(enemyTransform.position);

	// 		// Debug.Log("agent pos = " + transform.position);
	// 		Debug.Log("enemies alive = " + Enemy.EnemiesAlive);
	// 		Debug.Log("enemy pos = " + enemyTransform.position);
	// 	}
	// }

	public override void Heuristic (in ActionBuffers actionsOut) {

		Debug.Log("Heuristic callback");

		ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
		if (isPressed)
				{
					Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

					if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
						rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;
					else
						rb.position = mousePos;

					continuousActions[0] = mousePos[0];
					continuousActions[1] = mousePos[1];

					Debug.Log("continuousActions" + continuousActions);
				}
		}

	void ResetBallPosition(){

		// reset ball position
		Debug.Log("reset ball"); // + rb.position + hook.position);
		rb.position = hook.position;

		GetComponent<SpringJoint2D>().enabled = true;
		this.enabled = true;
	}


	public override void OnActionReceived(ActionBuffers actions) {

		Debug.Log("1");

		// rb.isKinematic = true;  //forces do not affect the agent
		if (throwNumber<=numberOfThrows){
			Vector2 action; 
			// float moveX = actions.ContinuousActions[0];
			// float moveY = actions.ContinuousActions[1];

			float moveX = -7.4f;
			float moveY = 2.0f;

			action = new Vector2(moveX, moveY);

			if (Vector3.Distance(action, hook.position) > maxDragDistance)
				rb.position = hook.position + (action - hook.position).normalized * maxDragDistance;
			else
				rb.position = action;
			Debug.Log("Throw = " + throwNumber + " action = " + action);
			Debug.Log(transform.position);

			throwNumber++;

			// to actually release the ball
			StartCoroutine(Release());

			Debug.Log("2");
			RewardAgent();

			Debug.Log("3");
			// reset the ball in the sling
			ResetBallPosition();

			Debug.Log("4");

		}
		if (throwNumber>numberOfThrows){

			EndEpisode();
			Debug.Log("End Episode");
			}
		else{
			Debug.Log("request next decision");
			this.RequestDecision();
		}
			
	}

		// if (needToAct){
		// 	rb.isKinematic = true;
		// 	Vector2 action; 
		// 	float moveX = actions.ContinuousActions[0];
		// 	float moveY = actions.ContinuousActions[1];
		// 	action = new Vector2(moveX, moveY);

		// 	if (Vector3.Distance(action, hook.position) > maxDragDistance)
		// 		rb.position = hook.position + (action - hook.position).normalized * maxDragDistance;
		// 	else
		// 		rb.position = action;
		// 	Debug.Log("Action = " + action);
		// }

		// rb.isKinematic = false;
		// GetComponent<SpringJoint2D>().enabled = false;
		// RewardAgent();
		// needToAct = false;
	// }

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

	void RewardAgent() {
		int reward = numberofEnemies - Enemy.EnemiesAlive;
		Debug.Log("Give reward = " + reward);
		AddReward(reward); 
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

		StartCoroutine(ManaulRelease());
	}


	IEnumerator ManaulRelease ()
	{
		Debug.Log("man release...");

		// After the action is taken: Release the spring
		yield return new WaitForSeconds(releaseTime);
		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;

	}


	IEnumerator Release ()
	{
		Debug.Log("release...");

		// After the action is taken: Release the spring
		yield return new WaitForSeconds(releaseTime);
		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;

		Debug.Log("wait...");
		// wait for effect, then reset the ball position
		yield return new WaitForSeconds(2f);
		Debug.Log("stop waiting.");
	}
	

		// if (Enemy.EnemiesAlive <= 0) { 
		// 	SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		// }


		// This loads a NEW agent - also with one action. 
		// if (nextBall != null)
		// {
		// 	nextBall.SetActive(true);
		// } else
		// {
		// 	Enemy.EnemiesAlive = 0;
		// 	Debug.Log("No balls left");
			// SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		// }
	// }

}
