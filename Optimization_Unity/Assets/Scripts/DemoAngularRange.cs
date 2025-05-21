using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoAngularRange : MonoBehaviour
{
    [SerializeField] private HerdController Herd;

    [Header("Parameter List")]
    [SerializeField] private Parameters[] ParameterSets;

    private void Awake()
    {
        Herd.InitializeHerdController();
        Herd.InitializeHerdAgent();
    }

    private void FixedUpdate()
    {
        List<int> agentIndices = new();
        for (int i = 0; i < Herd.AgentList.Count; i++)
        {
            agentIndices.Add(i);
        }
        Herd.UpdateAgentVisibility();
        Herd.Simulate(agentIndices, ParameterSets[0]);
    }
}
