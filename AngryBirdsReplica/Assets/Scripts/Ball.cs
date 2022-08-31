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

	public static int m_numberofEnemies = 1; 

	public GameObject[] m_cachedEnemiesGO = new GameObject[m_numberofEnemies];

	public static int EnemiesAlive;

	public GameObject[] terrain;

	public float releaseTime = .1f;

	float m_waitForEffect = 5.0f;

	public float maxDragDistance = 4.0f;

	public static int m_numberofThrows = 5;

	int m_throwsRemaining = m_numberofThrows;

	bool isPressed = false; 

	float m_currentReward = 0; 

	public bool ResartEpisode = true;  // strange behaviour: calls EpisodeBegin too often
 	public bool useVecObs;

	// ActionSegment<float> Action = ActionSegment<float>.Empty;
	public Vector2 Action = Vector2.zero;
	bool isInference = true;
	bool isActionSet = false;
	bool ballReset = false;
	float m_totalRewards = 0;

	EnvironmentParameters m_ResetParams;
	Unity.MLAgents.Policies.BehaviorType behaviorType;

	void Start () {
		/* Called on the frame when a script is enabled just before the Update method is called the first time.
		Called exactly once in the lifetime of the script. */

		// Called multiple times for Inference with Model loaded?

		rb = rb.GetComponent<Rigidbody2D>();
		hook = hook.GetComponent<Rigidbody2D>();

		/*If you want to control the agent manually, change Agent to HEURISTIC ONLY*/
		if (GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.HeuristicOnly)
			isInference = false;

		Debug.Log("isInference = " + isInference);
		Debug.Log("behaviourType = " + behaviorType);

		m_ResetParams = Academy.Instance.EnvironmentParameters;
		m_cachedEnemiesGO = GameObject.FindGameObjectsWithTag("Enemy");
	}

	 public override void OnEpisodeBegin() {
		if (ResartEpisode){
			Debug.Log("Step: " + Academy.Instance.StepCount + " OnEpisodeBegin called");
			SetResetParameters();
			ResartEpisode = false;
			Debug.Log("n Enemies = " + m_numberofEnemies + " n Enemies Alive = " + Enemy.EnemiesAlive);
		}
	}

    public override void CollectObservations(VectorSensor sensor)
	{
		if (useVecObs){
			Debug.Log(">>CollectObservations");
			foreach(GameObject enemyGO in m_cachedEnemiesGO){
				// if enemy is destroy, add zeros to sensor
				if (enemyGO.activeInHierarchy){
					// Debug.Log("OBS: " + enemyGO.name + " " + enemyGO.transform.position);
					sensor.AddObservation(enemyGO.transform.position);
					}
				else { 
					sensor.AddObservation(Vector3.zero);
					//Debug.Log("OBS dead");
				}
    		}
		}
	}


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
		Debug.Log(">>onActionReceived");
		// Debug.Log("RAW Action: " + actionBuffers.ContinuousActions[0] + ",  " + actionBuffers.ContinuousActions[1]);

		isActionSet = true;

		if (!isInference){
			// If Heuristic: mouse position defines movement
			Action[0] = actionBuffers.ContinuousActions[0];
			Action[1] = actionBuffers.ContinuousActions[1];
		}
		else{
			// If Inference or Training Mode: action defines movement relative to hook position
			var ActionScale = 10;
			Action[0] = actionBuffers.ContinuousActions[0]*ActionScale + hook.position[0];
			Action[1] = actionBuffers.ContinuousActions[1]*ActionScale + hook.position[1];
			// Debug.Log("SCALED Action: " + Action[0] + ",  " + Action[1]);

			// // MoveAgent(actionBuffers.ContinuousActions);
			// MoveAgent(Action);
			// StartCoroutine(Release());
		}
	}



	// Called every frame to drag the Agent when not in Inference mode
	void Update(){

		// If Heuristic Mode: rely on Mouse Actions
		if (!isInference && isPressed){
				RequestDecision();
				if (isActionSet){MoveAgent(Action);}  // Release() on MouseUp
		}
		else
			if (ballReset && m_throwsRemaining >= 1){
				Debug.Log("Request Throw");
				this.RequestDecision();
				if (isActionSet){
					MoveAgent(Action);
					StartCoroutine(Release());
					
				}
			}

		Debug.Log("enemies still alive = "+ Enemy.EnemiesAlive);
		// Debug.Log("throw: " + throwNumber + " of " + m_numberofThrows + ". Remaining = " + m_throwsRemaining);

		// Criteria to end episode early
		if (m_throwsRemaining <= 0 || Enemy.EnemiesAlive <= 0){
			Debug.Log("End Episode " + " Sum of Rewards = " + m_totalRewards);
			ResartEpisode = true; 
		}

		if (ResartEpisode || m_currentReward >= 1){
			Debug.Log("END END END Episode");
			EndEpisode();
			// SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			// Debug.Log("HERE?");
			}

		//todo: check the y component of Enemies - if it rolls off the Ground it never stops falling.
	}


	// does the actual moving of the GameObject
	// public void MoveAgent(ActionSegment<float> action){
	public void MoveAgent(Vector2 mousePos){
		if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
			{rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;}
		else
			{rb.position = mousePos;}
	}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
		var continuousActionsOut = actionsOut.ContinuousActions;
		continuousActionsOut[0] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[0];
		continuousActionsOut[1] = Camera.main.ScreenToWorldPoint(Input.mousePosition)[1];
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
		// StopCoroutine(Release());
	}

	public void ResetBall(){

		// Debug.Log("reset ball");

		// reset ball position
		rb.velocity = Vector2.zero;
		rb.angularVelocity = 0f;
		rb.position = hook.position;

		// reattach the spring
		GetComponent<SpringJoint2D>().enabled = true;
		this.enabled = true;

		ballReset=true;
	}

	void RewardAgent() {

		Debug.Log("reward func:: n Enemies = " + m_numberofEnemies + " n Enemies Alive = " + Enemy.EnemiesAlive);
		m_currentReward = ((float)m_numberofEnemies - (float)Enemy.EnemiesAlive)/(float)m_numberofEnemies; 
		Debug.Log("Set reward = " + m_currentReward);
		SetReward(m_currentReward); 

		m_totalRewards += m_currentReward; // Check that we ever receive rewards
	}

    public static class WaitFor
    {
        public static IEnumerator Frames(int frameCount)
        {
 
            while (frameCount > 0)
            {
                frameCount--;
                yield return null;
            }
        }
    }


	// Releases the ball
	IEnumerator Release () {

		// After the action is taken: Release the spring
		yield return StartCoroutine(WaitFor.Frames(1));
		// yield return new WaitForSeconds(releaseTime);
		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;
		
		// wait for effect, then reset/reward 
		yield return StartCoroutine(WaitFor.Frames(20));
		// yield return new WaitForSeconds(m_waitForEffect);
		
		int throwNumber = m_numberofThrows - m_throwsRemaining +1;
		Debug.Log("Throw: " + throwNumber + " = (" + Action[0] + ", " + Action[1] + ")");
		m_throwsRemaining -= 1;

		RewardAgent();
		ResetBall(); 
	}

	public void SetResetParameters()
    {
		m_currentReward = 0;
		m_throwsRemaining = m_numberofThrows; 
		ResetBall();
		SetReward(0);

		foreach (GameObject enemyGO in m_cachedEnemiesGO) {

			if (!enemyGO.activeInHierarchy){
				Debug.Log("RESPAWN = " + enemyGO.name);
				enemyGO.GetComponent<Enemy>().Respawn();
			}
			// enemyGO.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), 
														// Random.Range(-1.4f, -1.0f), 0);
		}

		terrain = GameObject.FindGameObjectsWithTag("Terrain");
		foreach(GameObject wood in terrain){
			wood.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), 
														Random.Range(-1.4f, -1.0f), 0);
		}

    }

}
