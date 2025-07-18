using Nebukam.Common;
using Nebukam.ORCA; 
using System.Collections.Generic;
using Unity.Mathematics; 
using UnityEngine;

public class ORCASimulationManager : MonoBehaviour
{
    public static ORCASimulationManager Instance { get; private set; }

    // The core ORCA object from the library that will run the simulation
    private ORCABundle<Agent> orcaBundle;

    // A dictionary to map our MonoBehaviour EnemyAgents to their data-oriented counterparts
    // This is the bridge between the GameObject world and the simulation world.
    private Dictionary<EnemyAgent, Agent> agentMap = new Dictionary<EnemyAgent, Agent>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Initialize the ORCA bundle
        orcaBundle = new ORCABundle<Agent>();

        // For a 2D game, simulate on the XY plane. 
        // For a 3D game with movement on the ground,use AxisPair.XZ.
        orcaBundle.plane = AxisPair.XY;
    }

    void Update()
    {
        

        // This is the core update loop, taken directly from the library's documentation.
        // It ensures the job from the previous frame is complete before we access its data.
        if (orcaBundle.orca.TryComplete())
        {
            // The simulation step is finished. The Agent data (position, velocity) 
            // in the bundle is now safely updated with the results from the job.

            // Now that the previous frame's work is done, we can schedule the next 
            // simulation step, which will run in the background.
            orcaBundle.orca.Schedule(Time.deltaTime);
        }
        else
        {
            // This 'else' block is a failsafe. If the job from the previous frame is somehow
            // still running, we don't try to access its data. We just keep it scheduled.
            // This prevents the simulation from stalling on a very laggy frame.
            orcaBundle.orca.Schedule(Time.deltaTime);
        }
    }

    // Called by an EnemyAgent (usually in Start()) to join the simulation.
    /// <param name="enemy">The MonoBehaviour agent joining.
    // The data-oriented Agent struct that represents this enemy in the simulation.
    public Agent RegisterAgent(EnemyAgent enemy)
    {
        // Prevent duplicate registrations
        if (agentMap.ContainsKey(enemy))
        {
            return agentMap[enemy];
        }

        // Add a new agent to the ORCA simulation at its current world position.
        // We cast the Vector3 transform.position to a float3 for the library.
        Agent orcaAgent = orcaBundle.agents.Add((float3)enemy.transform.position);

        // Store the mapping so we can find this agent later.
        agentMap.Add(enemy, orcaAgent);

        Debug.Log($"Agent {enemy.name} registered with ORCA simulation.");
        return orcaAgent;
    }

    // Called by an EnemyAgent (usually in OnDestroy()) to leave the simulation.
    /// <param name="enemy">The MonoBehaviour agent leaving.</param>
    public void UnregisterAgent(EnemyAgent enemy)
    {
        if (agentMap.TryGetValue(enemy, out Agent orcaAgent))
        {
            // Remove the agent from the simulation and our map
            orcaBundle.agents.Remove(orcaAgent);
            agentMap.Remove(enemy);
            Debug.Log($"Agent {enemy.name} unregistered from ORCA simulation.");
        }
    }

    /// This is the critical method for providing INPUT to the simulation.
    /// An EnemyAgent calls this every frame to tell the simulation where it WANTS to go.
    /// <param name="agent">The agent struct to modify.</param>
    /// <param name="velocity">The desired velocity for this frame.</param>
    public void SetAgentPreferredVelocity(Agent agent, float3 velocity)
    {
        // This directly sets the 'prefVelocity' which the ORCA algorithm will use
        // as the goal for its calculation.
        agent.prefVelocity = velocity;
    }

    private void OnDestroy()
    {
        // This is a critical cleanup step.
        // We must complete any running job and dispose of the native collections
        // used by the bundle to prevent memory leaks and errors in the editor.
        orcaBundle?.orca.Complete();
        orcaBundle?.Dispose();
    }
}