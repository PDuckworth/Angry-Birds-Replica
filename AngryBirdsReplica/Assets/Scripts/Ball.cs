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
	public float maxDragDistance = 2f;

	public GameObject nextBall;

	private bool isPressed = false;

	public int NumberofEnemies = 0; 

	[SerializeField] private Transform enemyTransform;

	public override void OnEpisodeBegin() {
		
		// public int EnemiesAlive = Enemy.EnemiesAlive; 
		Debug.Log("OnEpisodeBegin");
		Debug.Log("Enemies Alive = " + Enemy.EnemiesAlive);

		NumberofEnemies = Enemy.EnemiesAlive;

	}

	public override void CollectObservations(VectorSensor sensor) {
		sensor.AddObservation(transform.position);
		sensor.AddObservation(enemyTransform.position);

		// Debug.Log("agent pos = " + transform.position);
		Debug.Log("enemies alive = " + Enemy.EnemiesAlive);
		Debug.Log("enemy pos = " + enemyTransform.position);
	}

	public override void Heuristic (in ActionBuffers actionsOut) {
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
			}
		}

	// public override void OnActionReceived(ActionBuffers actions) {

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
	// 	GetComponent<SpringJoint2D>().enabled = false;
		
	// 	RewardAgent();
	// }

	// void Update ()
	// {
	// 	if (isPressed)
	// 	{
	// 		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

	// 		if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
	// 			rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;
	// 		else
	// 			rb.position = mousePos;
	// 	}
	// }

	void RewardAgent() {
		int reward = NumberofEnemies - Enemy.EnemiesAlive;
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

		StartCoroutine(Release());
	}

	IEnumerator Release ()
	{
		yield return new WaitForSeconds(releaseTime);

		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;

		yield return new WaitForSeconds(2f);

		if (nextBall != null)
		{
			nextBall.SetActive(true);
		} else
		{
			Enemy.EnemiesAlive = 0;
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	
	}

}
