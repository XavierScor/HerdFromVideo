using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parameters : MonoBehaviour
{
    [Header("Agent Sensitivity")]
    [Range(0f, 1000f)]
    [SerializeField] private float Sensitivity = 1.0f;

    [Header("Avoidance Force")]
    [Range(0, 1.0f)]
    [SerializeField] private float AvoidanceWeight = 0.25f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float AvoidanceRadialRange = 0.7f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float AvoidanceAngularRange = 1.0f;

    [Header("Cohesion Force")]
    [Range(0, 1.0f)]
    [SerializeField] private float CohesionWeight = 0.15f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float CohesionRadialRange = 1.0f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float CohesionAngularRange = 0.5f;

    [Header("Alignment Force")]
    [Range(0, 1.0f)]
    [SerializeField] private float AlignmentWeight = 0.15f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float AlignmentRadialRange = 1.0f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float AlignmentAngularRange = 0.5f;

    [Header("Obstacle-Related Force")]
    [Range(0, 1.0f)]
    [SerializeField] private float ObstacleAvoidanceWeight = 0.15f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float ObstacleRadialRange = 1.0f;
    [Range(1e-3f, 1.0f)]
    [SerializeField] private float ObstacleAngularRange = 0.5f;

    [Header("Debug Option")]
    [SerializeField] private bool IsPrintWhenUpdate = false;

    public float GetSensitivity()
    {
        return Sensitivity;
    }

    public void SetSensitivity(float sensitivity)
    {
        Sensitivity = sensitivity;
    }

    public float[] GetParameterArray()
    {
        float[] parameters = {
            AvoidanceWeight,            AvoidanceRadialRange,   AvoidanceAngularRange,
            CohesionWeight,             CohesionRadialRange,    CohesionAngularRange,
            AlignmentWeight,            AlignmentRadialRange,   AlignmentAngularRange,
            ObstacleAvoidanceWeight,    ObstacleRadialRange,    ObstacleAngularRange
        };
        return parameters;
    }

    public void SetParameterArray(float[] parameterArray)
    {
        AvoidanceWeight = parameterArray[0];
        AvoidanceRadialRange = parameterArray[1];
        AvoidanceAngularRange = parameterArray[2];

        CohesionWeight = parameterArray[3];
        CohesionRadialRange = parameterArray[4];
        CohesionAngularRange = parameterArray[5];

        AlignmentWeight = parameterArray[6];
        AlignmentRadialRange = parameterArray[7];
        AlignmentAngularRange = parameterArray[8];

        ObstacleAvoidanceWeight = parameterArray[9];
        ObstacleRadialRange = parameterArray[10];
        ObstacleAngularRange = parameterArray[11];
    }

    public void UpdateParameters(float[] stepSize, float[] gradients)
    {
        float gradientNorm = 0;
        for (int i = 0; i < gradients.Length; i++)
        {
            gradientNorm += gradients[i] * gradients[i];
        }
        gradientNorm = Mathf.Sqrt(gradientNorm);

        if (gradientNorm > 1e-3)
        {
            AvoidanceWeight             += stepSize[0] * gradients[0] / gradientNorm;
            AvoidanceRadialRange        += stepSize[1] * gradients[1] / gradientNorm;
            AvoidanceAngularRange       += stepSize[2] * gradients[2] / gradientNorm;

            CohesionWeight              += stepSize[3] * gradients[3] / gradientNorm;
            CohesionRadialRange         += stepSize[4] * gradients[4] / gradientNorm;
            CohesionAngularRange        += stepSize[5] * gradients[5] / gradientNorm;

            AlignmentWeight             += stepSize[6] * gradients[6] / gradientNorm;
            AlignmentRadialRange        += stepSize[7] * gradients[7] / gradientNorm;
            AlignmentAngularRange       += stepSize[8] * gradients[8] / gradientNorm;

            ObstacleAvoidanceWeight     += stepSize[9] * gradients[9] / gradientNorm;
            ObstacleRadialRange         += stepSize[10] * gradients[10] / gradientNorm;
            ObstacleAngularRange        += stepSize[11] * gradients[11] / gradientNorm;
        }
        //AvoidanceWeight             += stepSize[0] * gradients[0] / gradientNorm;
        //AvoidanceRadialRange        += stepSize[1] * gradients[1] / gradientNorm;
        //AvoidanceAngularRange       += stepSize[2] * gradients[2] / gradientNorm;

        //CohesionWeight              += stepSize[3] * gradients[3] / gradientNorm;
        //CohesionRadialRange         += stepSize[4] * gradients[4] / gradientNorm;
        //CohesionAngularRange        += stepSize[5] * gradients[5] / gradientNorm;

        //AlignmentWeight             += stepSize[6] * gradients[6] / gradientNorm;
        //AlignmentRadialRange        += stepSize[7] * gradients[7] / gradientNorm;
        //AlignmentAngularRange       += stepSize[8] * gradients[8] / gradientNorm;

        //ObstacleAvoidanceWeight     += stepSize[9] * gradients[9] / gradientNorm;
        //ObstacleRadialRange         += stepSize[10] * gradients[10] / gradientNorm;
        //ObstacleAngularRange        += stepSize[11] * gradients[11] / gradientNorm;

        if (IsPrintWhenUpdate)
        {
            float[] parameters = GetParameterArray();
            string debugInfo = "";
            for (int i = 0; i < parameters.Length; i++)
                debugInfo += parameters[i] + " ";
            Debug.Log(debugInfo);
        }
    }
}
