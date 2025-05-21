using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HerdAgent : MonoBehaviour
{
    private Rigidbody _rigidbody;
    //private Animator _animator;
    private Collider _collider;
    private GameObject _terrain;

    private const string IS_WALKING = "isWalking";

    private Vector3 _terrainDampingBuffer = new();
    private Vector3 _directionDampingBuffer = new();

    [SerializeField] private bool IsUserControlled = false;

    [SerializeField] private float Thrust = 20f;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float ForceMemoryDecay = 1.0f;
    [SerializeField] private float SpeedToAlign = 0.2f;
    [SerializeField] private float TerrainAlignTime = 0.1f;
    [SerializeField] private float DirectionAlignTime = 0.1f;

    [SerializeField] private GameObject Arrow;

    public bool IsShowVisibleNeighbors = false;
    public bool IsShowVisibleObstacles = false;
    public bool IsShowForceByNeighbors = false;

    private Vector3 _memorizedForce;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        //_animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<CapsuleCollider>();

        _rigidbody.maxLinearVelocity = 3.0f;
        //_rigidbody.maxAngularVelocity = 0.001f;
    }

    public void SetTerrain(GameObject terrain)
    {
        _terrain = terrain;
        Physics.IgnoreCollision(_collider, _terrain.GetComponent<Collider>());
    }

    public void SetMaxLinearSpeed(float maxSpeed)
    {
        _rigidbody.maxLinearVelocity = maxSpeed;
    }

    public void SetDrag(float drag)
    {
        _rigidbody.drag = drag;
    }

    public void SetSpeed(float speed)
    {
        _rigidbody.velocity = speed * _rigidbody.velocity.normalized;
    }

    public Vector3 GetVelocity()
    {
        return _rigidbody.velocity;
    }

    // NOTE: This function is only used when force applied on agent is from the virtual environment.
    //       When user is controlling the agent, this function will not make effect.
    public void MoveByForce(Vector3 force)
    {
        if (!IsUserControlled)
        {
            Vector3 currentForce = ForceMemoryDecay * force + (1.0f - ForceMemoryDecay) * _memorizedForce;
            _rigidbody.AddForce(currentForce * Thrust);
            _memorizedForce = currentForce;
        }
        AgentUpdate();
    }

    public void AgentUpdate()
    {
        Vector3 currentVelocity = _rigidbody.velocity;
        float currentSpeed = currentVelocity.magnitude;

        //// === Update animator according to current status ===
        //if (currentSpeed > 0 && _rigidbody.maxLinearVelocity > 0)
        //{
        //    _animator.SetBool(IS_WALKING, true);
        //    _animator.speed = currentSpeed / _rigidbody.maxLinearVelocity;
        //}
        //else
        //{
        //    _animator.SetBool(IS_WALKING, false);
        //}

        // === Agent movement controlled by user ===
        // NOTE: Backward force will make agent point backward.
        //       When agent changes direction, direction of backward force will change too.
        //       This will lead to infinite turing of agent.
        //       Therefore vertical direction is 01 clamped.
        if (IsUserControlled)
        {
            Vector3 inputVector = new(Input.GetAxis("Horizontal"), 0.0f, Mathf.Clamp01(Input.GetAxis("Vertical")));
            Vector3 forceLocalSpace = inputVector.normalized;
            Vector3 forceWorldSpace = transform.TransformDirection(forceLocalSpace);
            _rigidbody.AddForce(forceWorldSpace * Thrust);
        }

        // === Terrain alignment and snapping === 
        Vector3 alignedUp = transform.up;
        Vector3 alignedForward = transform.forward;
        Vector3 alignedPos = transform.position;

        // === Align and snap to the terrain surface ===
        if (_terrain == null)
        {
            Debug.LogError("Terrain is not set before updating agents!");
        }
        else
        {
            TerrainData terrainData = _terrain.GetComponent<Terrain>().terrainData;
            Vector3 terrainSize = terrainData.size;
            float agentTerrainX = transform.position.x - _terrain.transform.position.x;
            float agentTerrainZ = transform.position.z - _terrain.transform.position.z;
            if (0.0f < agentTerrainX && agentTerrainX < terrainSize.x &&
                0.0f < agentTerrainZ && agentTerrainZ < terrainSize.z)
            {
                alignedUp = Vector3.SmoothDamp(
                    transform.up,
                    terrainData.GetInterpolatedNormal(agentTerrainX / terrainSize.x, agentTerrainZ / terrainSize.z),
                    ref _terrainDampingBuffer,
                    TerrainAlignTime,
                    Mathf.Infinity,
                    Time.fixedDeltaTime);

                alignedPos.y = terrainData.GetInterpolatedHeight(agentTerrainX / terrainSize.x, agentTerrainZ / terrainSize.z);
            }
        }

        // === Update orientation based on current status ===
        if (currentSpeed > SpeedToAlign)
        {
            Vector3 velocityComponentAlongUp = Vector3.Dot(alignedUp, currentVelocity) * alignedUp.normalized;
            Vector3 forwardDirection = (currentVelocity - velocityComponentAlongUp).normalized;

            alignedForward = Vector3.SmoothDamp(
                transform.forward,
                forwardDirection,
                ref _directionDampingBuffer,
                DirectionAlignTime,
                Mathf.Infinity,
                Time.fixedDeltaTime);
        }

        transform.SetPositionAndRotation(alignedPos, Quaternion.LookRotation(alignedForward, alignedUp));
    }
}
