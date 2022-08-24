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

	public static int m_numberofEnemies = 2; 

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

	ActionSegment<float> previousAction = ActionSegment<float>.Empty;

	bool isInference = true;
	public bool isTraining = true; 
	bool isPreviousActionSet = false;
	bool levelWon = false;
	bool ballReset = false;


	EnvironmentParameters m_ResetParams;
	Unity.MLAgents.Policies.BehaviorType behaviorType;

	void Start () {
		/* Called on the frame when a script is enabled just before the Update method is called the first time.
		Called exactly once in the lifetime of the script. */

		rb = rb.GetComponent<Rigidbody2D>();
		hook = hook.GetComponent<Rigidbody2D>();

		/* isInference = GetComponent<BehaviorParameters>().BehaviorType == BehaviorType.InferenceOnly;*/

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
		previousAction = actionBuffers.ContinuousActions;
		isPreviousActionSet = true;
		if (isInference){
			MoveAgent(actionBuffers.ContinuousActions);
			StartCoroutine(Release());
		}
	}

	// Called every frame to drag the Agent when not in Inference mode
	void Update(){

		// Debug.Log("Update");
		if (!isInference && isPressed){
				RequestDecision();
				if (isPreviousActionSet){MoveAgent(previousAction);}
		}
		else
			if (ballReset && m_throwsRemaining >= 1){
				Debug.Log("Request Throw");
				this.RequestDecision();
			}
			

		// if enemies are destroyed after some wait
		// float currentReward = ((float)m_numberofEnemies - (float)Enemy.EnemiesAlive)/(float)m_numberofEnemies;
		// if (m_currentReward + float.Epsilon < currentReward){
		// 	Debug.Log("someone is more dead");
		// 	RewardAgent();
		// }

		if (m_currentReward >= 1 || ResartEpisode){
			Debug.Log("END END END");
			EndEpisode();
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
			}

	}


	// does the actual moving of the GameObject
	public void MoveAgent(ActionSegment<float> action){
		// Debug.Log("MoveAgent Step: " + Academy.Instance.StepCount);
		var mousePos = new Vector2(action[0], action[1]);

		if (Vector3.Distance(mousePos, hook.position) > maxDragDistance)
			rb.position = hook.position + (mousePos - hook.position).normalized * maxDragDistance;
		else
			rb.position = mousePos;
	}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
		isTraining = false; // if Heuristic function is called: the agent is not training

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

		Debug.Log("reset ball");

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
	}

	// Releases the ball
	IEnumerator Release () {
		int throwNumber = m_numberofThrows - m_throwsRemaining +1;
		Debug.Log("Throw: " + throwNumber + " = (" + previousAction[0] + ", " + previousAction[1] + ")");
		m_throwsRemaining -= 1;

		// After the action is taken: Release the spring
		yield return new WaitForSeconds(releaseTime);
		GetComponent<SpringJoint2D>().enabled = false;
		this.enabled = false;
		
		// wait for effect, then reset/reward 
		//if (m_waitForEffect>=0f){m_waitForEffect -= Time.fixedDeltaTime;} // count down between actions
		yield return new WaitForSeconds(m_waitForEffect);
		
		// Academy.Instance.EnvironmentStep();  // evolve the env step

		Debug.Log("enemies still alive = "+ Enemy.EnemiesAlive);
		if (Enemy.EnemiesAlive <= 0){
			Debug.Log("LEVEL WON!");
			levelWon = true;
		}
		else {levelWon = false;};

		Debug.Log("throw: " + throwNumber + " of " + m_numberofThrows + ". Remaining = " + m_throwsRemaining);
		if (m_throwsRemaining <= 0 || levelWon){
			Debug.Log("End Episode");
			ResartEpisode = true; // is this needed now the control flow is better?
			// EndEpisode(); // Auto starts another Episode
		}


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
			enemyGO.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), 
														Random.Range(-1.4f, -1.0f), 0);
		}

		terrain = GameObject.FindGameObjectsWithTag("Terrain");
		foreach(GameObject wood in terrain){
			wood.transform.localPosition = new Vector3(Random.Range(0.0f, 10.0f), 
														Random.Range(-1.4f, -1.0f), 0);
		}

    }

}
