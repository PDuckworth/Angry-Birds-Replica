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

	public Rigidbody2D enemy1;

	public Rigidbody2D enemy2;

	public float releaseTime = .15f;

	public float waitForEffectTime = 4.0f;

	public float maxDragDistance = 3.0f;

	private int numberOfThrows = 3; 

	private int throwNumber; 

	private bool isPressed = false; 

	public int collectReward = 0;

	public int numberofEnemies; 

	public static int EnemiesAlive;

	public bool episodeNeedsStarting = true;  // strange behaviour: calls EpisodeBegin too often

	public bool actionNeeded = false;  // strange behaviour: calls EpisodeBegin too often

 	public bool useVecObs;

    void Start () {
		rb = rb.GetComponent<Rigidbody2D>();
		hook = hook.GetComponent<Rigidbody2D>();

		// Can randomise these in the future
		enemy1 = enemy1.GetComponent<Rigidbody2D>();
		enemy2 = enemy2.GetComponent<Rigidbody2D>();

		Debug.Log("ball "+rb.position);
		Debug.Log("hook "+hook.position);

		Debug.Log("enemy1 " +enemy1.position);
		Debug.Log("enemy2 " +enemy2.position);
	} 

	 public override void OnEpisodeBegin() {
		if (episodeNeedsStarting){

			ResetBall();
			throwNumber = 0;
			SetReward(0);
			Enemy.EnemiesAlive = 0;
			int numberofEnemies = 2;
			Debug.Log("n Enemies = " + numberofEnemies + " n Enemies Alive = " + Enemy.EnemiesAlive);

			Debug.Log(Academy.Instance.StepCount + " OnEpisodeBegin called");			

			Debug.Log("request next decision");
			this.RequestDecision();
			episodeNeedsStarting = false;
		}
	}

    public override void CollectObservations(VectorSensor sensor)
	{
		if (useVecObs){
			sensor.AddObservation(enemy1.transform.position);
			sensor.AddObservation(enemy2.transform.position);

			Debug.Log("collectObs = " + sensor);

			actionNeeded = true;
			this.RequestDecision();
		}
	}

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
		Debug.Log("action1 rec " + actionBuffers.ContinuousActions[0]);
		Debug.Log("action2 rec " + actionBuffers.ContinuousActions[1]);

		if (actionNeeded){
			MoveAgent(actionBuffers.ContinuousActions);
			actionNeeded = false;
		}
	}

	public void MoveAgent(ActionSegment<float> action){

			var mousePos = new Vector2(action[0], action[1]);

			Debug.Log("mouse pos = " + mousePos);
			if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
				rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;
			else
				rb.position = mousePos;

			StartCoroutine(Release());
			StopCoroutine(Release());
	}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
		if (isPressed){

			Debug.Log("Heuristic: " + actionsOut.ContinuousActions);
			
			var continuousActionsOut = actionsOut.ContinuousActions;
			continuousActionsOut[0] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[0];
			continuousActionsOut[1] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[1];

			MoveAgent(continuousActionsOut);
		}

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

		// StartCoroutine(Release());
		// StopCoroutine(Release());
	}

	void ResetBall(){

		Debug.Log("reset ball");

		// reset ball position
		rb.velocity = Vector2.zero;
		rb.angularVelocity = 0f;
		rb.position = hook.position;

		Debug.Log("throw n: "+ throwNumber);

		// reattach the spring
		GetComponent<SpringJoint2D>().enabled = true;
		this.enabled = true;

		actionNeeded = true;
	}

	void RewardAgent() {

		float killed = 0;
		if (numberofEnemies != 0){
			killed = ((float)numberofEnemies - (float)Enemy.EnemiesAlive)/(float)numberofEnemies; 
		}
		
		Debug.Log("Set reward = " + killed);
		SetReward(killed); 
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
		
		throwNumber++;
		RewardAgent();

		if (Enemy.EnemiesAlive <= 0)
			Debug.Log("LEVEL WON!");

		ResetBall();
		
		if (throwNumber==numberOfThrows){
			Debug.Log("End Episode");
			episodeNeedsStarting = true;
			EndEpisode();
			// SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}

		// if (nextBall != null)
		// {
		// 	nextBall.SetActive(true);
		// } else
		// {
		// 	Enemy.EnemiesAlive = 0;
		// 	SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		// }
	
	

		// if (Enemy.EnemiesAlive <= 0) { 
		// 	SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		// }


	
	// }

}
