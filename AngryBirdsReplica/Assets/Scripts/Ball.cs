using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents; 
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class Ball : Agent {

	public Rigidbody2D rb;

	public Rigidbody2D hook;

	public Rigidbody2D enemy1;

	public Rigidbody2D enemy2;

	public float releaseTime = .15f;

	public float waitForEffectTime = 4.0f;

	public float maxDragDistance = 3.0f;

	private int numberOfThrows = 3; 

	private int throwNumber; 

	private bool isPressed = false; 

	public int numberofEnemies; 

	public static int EnemiesAlive;

	public bool episodeNeedsStarting = true;  // strange behaviour: calls EpisodeBegin too often

 	public bool useVecObs;

	ActionSegment<float> previousAction = ActionSegment<float>.Empty;

	public bool isInference;
	public bool isTraining = true; 

	void Start () {
		rb = rb.GetComponent<Rigidbody2D>();
		hook = hook.GetComponent<Rigidbody2D>();

		isInference = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;

		// Can randomise these in the future
		enemy1 = enemy1.GetComponent<Rigidbody2D>();
		enemy2 = enemy2.GetComponent<Rigidbody2D>();

		Debug.Log("ball "+rb.position);
		Debug.Log("hook "+hook.position);

		Debug.Log("enemy1 " +enemy1.position);
		Debug.Log("enemy2 " +enemy2.position);

		Debug.Log("isInference = " + isInference);
	}


	 public override void OnEpisodeBegin() {
		if (episodeNeedsStarting){

			ResetBall();
			throwNumber = 0;
			SetReward(0);

			numberofEnemies = Enemy.EnemiesAlive;
			Debug.Log("n Enemies = " + numberofEnemies + " n Enemies Alive = " + Enemy.EnemiesAlive);

			Debug.Log(Academy.Instance.StepCount + " OnEpisodeBegin called");

			//initialise this?
			// ActionSegment<float> previousAction;// Empty;
			episodeNeedsStarting = false;
		}
	}

    public override void CollectObservations(VectorSensor sensor)
	{
		// %todo: When Enemies die, their position doesnt exist
		// check if they are alive?  
		if (useVecObs){
			sensor.AddObservation(enemy1.transform.position);
			sensor.AddObservation(enemy2.transform.position);

			// Debug.Log("collectObs = " + sensor);

			if (isInference){
				this.RequestDecision();
			}
		}
	}


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
		// Debug.Log("action1 rec " + actionBuffers.ContinuousActions[0]);
		// Debug.Log("action2 rec " + actionBuffers.ContinuousActions[1]);

		previousAction = actionBuffers.ContinuousActions;

		if (isInference){
			MoveAgent(actionBuffers.ContinuousActions);
			StartCoroutine(Release());
		}
	}


	void Update(){
		// This runs each Academy.step to allow us to drag the Agent
		// It does not request excess decisions when in Inference mode

		if (!isInference){
			if (isPressed){
				this.RequestDecision();

				// if (previousAction == ActionSegment<float>.Empty){
				// 	Debug.Log("weha?T" + previousAction);
				// }
				// else{
				MoveAgent(previousAction);
				// }
			}
		}
	}

	public void MoveAgent(ActionSegment<float> action){

			var mousePos = new Vector2(action[0], action[1]);

			if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
				rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;
			else
				rb.position = mousePos;

	}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
		// Debug.Log("Heuristic: " + actionsOut.ContinuousActions);

		// if Heuristic function is called: the agent is not training
		isTraining = false; 

		var continuousActionsOut = actionsOut.ContinuousActions;
		continuousActionsOut[0] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[0];
		continuousActionsOut[1] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[1];
	}

	void OnMouseDown ()
	{
		Debug.Log("down");
		isPressed = true;
		rb.isKinematic = true;
	}

	void OnMouseUp ()
	{
		Debug.Log("up");
		isPressed = false;
		rb.isKinematic = false;

		StartCoroutine(Release());
		// StopCoroutine(Release());
	}

	void ResetBall(){

		Debug.Log("reset ball");

		// reset ball position
		rb.velocity = Vector2.zero;
		rb.angularVelocity = 0f;
		rb.position = hook.position;

		// reattach the spring
		GetComponent<SpringJoint2D>().enabled = true;
		this.enabled = true;
	}

	void RewardAgent() {

		Debug.Log("reward func:: n Enemies = " + numberofEnemies + " n Enemies Alive = " + Enemy.EnemiesAlive);
		var killed = (numberofEnemies - Enemy.EnemiesAlive)/numberofEnemies; 
		
		Debug.Log("Set reward = " + killed);
		SetReward(killed); 
	}


	IEnumerator Release () {
		Debug.Log("Action = (" + previousAction[0] + ", " + previousAction[1] + ")");

		// After the action is taken: Release the spring
		yield return new WaitForSeconds(releaseTime);
		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;
		
		// wait for effect, then reset the ball position
		yield return new WaitForSeconds(waitForEffectTime);
		
		throwNumber++;

		Debug.Log("enemies still alive = "+ Enemy.EnemiesAlive);
		if (Enemy.EnemiesAlive <= 0){Debug.Log("LEVEL WON!");}

		Debug.Log("throw numbers = "+ throwNumber + " of " + numberOfThrows);
		if (throwNumber==numberOfThrows){
			RewardAgent();
			Debug.Log("End Episode");
			episodeNeedsStarting = true; // is this needed now the control flow is better?
			// Enemy.EnemiesAlive = 0;   // is this needed if the whole scene restarts? 
			EndEpisode(); // Auto starts another Episode
		}
		else{ 
			ResetBall(); 
			RewardAgent();
		}
		
	}

}
