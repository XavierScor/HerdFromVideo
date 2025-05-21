using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoAlignmentChange : MonoBehaviour
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
        List<int> leftAgentIndices = new();
        List<int> rightAgentIndices = new();
        for (int i = 0; i < Herd.AgentList.Count; i++)
        {
            if (Herd.AgentList[i].transform.position.x < Herd.transform.position.x)
                leftAgentIndices.Add(i);
            else
                rightAgentIndices.Add(i);
        }
        Herd.UpdateAgentVisibility();
        Herd.Simulate(leftAgentIndices, ParameterSets[0]);
        Herd.Simulate(rightAgentIndices, ParameterSets[1]);
    }
}
