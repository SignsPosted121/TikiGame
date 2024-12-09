using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : Entity
{

    private static int totalAttacking = 0;
    private static int totalAllowed = 1;

    public Entity target;

    private enum Activity { IDLE, ATTACKING, CHASING, DODGING };
    private Activity currentState = 0;
    private bool attacking = false; // Actively swinging or not
    [SerializeField] private float rushCooldown = 0;

    private readonly bool DEBUG = false;
    private const float RADIAN = Mathf.PI * 2;

    [SerializeField] private float attackDistance = 1.5f;
    [Range(0, 1), SerializeField] private float reactionTime = 0.2f;
    [Tooltip("X is in seconds of their attack mode rate (anywhere from X/1.5 to X*1.5)"), SerializeField, Range(0, 60)] private float rushTime = 7;
    [Tooltip("How many directions to calculate every movement calculation, every frame"), SerializeField, Range(4, 30)] private int totalDirectionProbes = 18;
    [Tooltip("Maintain a circle of this radius around the enemy"), SerializeField, Range(1, 10)] private float keepDistanceBetween = 3;
    [SerializeField, Range(0, 4)] private float objectAvoidance = 1.5f;
    [SerializeField, Range(1, 10)] private float maxWanderDistance = 4;
    [Tooltip("Percentage every stat can be mutated on spawn"), SerializeField, Range(0, 0.5f)] private float randomMutationChance = 0.1f;
    private Vector2 startPos;

    // General methods

    public override void Damage(int damage, Vector2 push)
    {
        if (currentState == Activity.ATTACKING) ChangeState(Activity.CHASING);
        base.Damage(damage, push);
    }

    protected override void Kill()
    {
        ChangeState(Activity.IDLE);
        base.Kill();
    }

    public override Vector2 GetForward()
    {
        if (GetSpeed().magnitude > 0) return (GetSpeed() * 1000).normalized;
        return Vector2.zero;
    }

    private Vector2 ConvertRadianToVector(float rad)
    {
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    private Vector2 DirectionOfTarget(Transform target)
    {
        Vector2 direction = (target.position - transform.position).normalized;
        return direction;
    }

    private void ChangeState(Activity newState)
	{
        if (newState == currentState) return;

        switch(currentState)
		{
            case Activity.ATTACKING:
                totalAttacking--;
                break;
            default:
                break;
		}

        switch (newState)
		{
            case Activity.ATTACKING:
                if (totalAttacking < totalAllowed)
                {
                    ResetRushCooldown();
                    totalAttacking++;
                }
                else return;
                break;
            default:
                break;
		}

        currentState = newState;
	}

    private void ResetRushCooldown()
	{
        rushCooldown = Random.Range(rushTime / 1.5f, rushTime * 1.5f);
    }

    // Methods for combat with AI

    private IEnumerator Attack()
    {
        if (attacking)
        {
            yield break;
        }
        float pause = reactionTime * Random.Range(0.9f, 1.1f);
        Stun(pause);

        yield return new WaitForSeconds(pause);

        attacking = true;
        ChangeState(Activity.CHASING);
        weapon.Primary();

        yield return new WaitForSeconds(weapon.GetAttackSpeed());
        attacking = false;
    }

    private void RandomRushAttack(bool doAttack)
    {
        if ((target.transform.position - transform.position).magnitude > keepDistanceBetween + 1f) return;

        if (!doAttack && rushCooldown <= 0) doAttack = true;

        if (doAttack) ChangeState(Activity.ATTACKING);
    }

    private void CheckForAttack(Transform target)
    {
        if (weapon) weapon.Aim(DirectionOfTarget(target));

        if ((transform.position - target.position).magnitude <= attackDistance && !attacking) StartCoroutine(Attack());
    }

    // Movment Core Systems

    private float CalculateObjectAvoidance(Vector2 direction)
    {
        float clearage = 1;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, transform.localScale.magnitude, direction, objectAvoidance);
        if (hit.transform != null && hit.transform != transform && hit.transform != target.transform)
        {
            Vector2 hitPoint = hit.point;
            Vector2 hitDir = (hitPoint - (Vector2)transform.position).normalized;
            //Debug.Log(hitPoint + ", " + (Vector2)transform.position);
            if (Vector2.Dot(hitDir, direction) > -0.5f) clearage = Mathf.Clamp01((hitPoint - (Vector2)transform.position).magnitude - objectAvoidance);
        }
        return clearage;
    }

    private float CalculateTargetSteer(Transform target, Vector2 direction)
    {
        Vector2 targetDir = DirectionOfTarget(target);

        float distance = (target.position - transform.position).magnitude;
        float facing = (Vector2.Dot(targetDir, direction) + 1) / 2;
        float extra = Mathf.Clamp((distance / (keepDistanceBetween * 2) * facing) * 2 - 1, -1, 1);

        return facing * extra * CalculateObjectAvoidance(direction);
    }

    private Vector2 WeighDirection(Vector2 direction)
    {
        float totalMultiplier = CalculateTargetSteer(target.transform, direction) * 0.9f;
        Vector2 forward = GetForward();

        if (forward.magnitude > 0)
        {
            float momentumMultiplier = (Vector2.Dot(forward, direction) + 1) / 20;
            totalMultiplier += momentumMultiplier;
        }

        if (currentState == Activity.ATTACKING) totalMultiplier = (Vector2.Dot(DirectionOfTarget(target.transform), direction) + 1) / 2;

        return direction * totalMultiplier;
    }

    private Vector2 GetCombatMovement() // called from GetMovement whenever there is a target
    {
        if (currentState == Activity.CHASING) if (!attacking)
                RandomRushAttack(false);

        List<Vector2> directions = new List<Vector2>();
        float frontRad = Mathf.Atan2(GetForward().y, GetForward().x);

        if (target != null)
        {
            Vector2 targetDir = DirectionOfTarget(target.transform);
            frontRad = Mathf.Atan2(targetDir.y, targetDir.x);
        }
        for (float i = 0; i <= RADIAN; i += RADIAN / totalDirectionProbes)
        {
            Vector2 currentDir = ConvertRadianToVector(i + frontRad);
            directions.Add(WeighDirection(currentDir));
        }

        Vector2 heaviestDir = Vector2.zero;

        if (GetForward().magnitude == 0) heaviestDir = directions[Random.Range(0, directions.Count)];

        else foreach (Vector2 currentDir in directions)
            {
                if (currentDir.magnitude > heaviestDir.magnitude) heaviestDir = currentDir;

                else if (currentDir.magnitude == heaviestDir.magnitude)
                    if (Random.Range(0, 2) == 0) heaviestDir = currentDir;
            }

        if (DEBUG)
        {
            foreach (Vector2 currentDir in directions)
            {
                if (Mathf.Abs(Vector2.Dot(currentDir, heaviestDir)) <= Mathf.Epsilon) continue;

                Color lineColor = Color.green;
                if (currentDir.magnitude <= 0.5f) lineColor = Color.red;

                Debug.DrawLine(transform.position, transform.position + (Vector3)currentDir, lineColor);
            }

            Debug.DrawLine(transform.position, transform.position + (Vector3)heaviestDir, Color.blue);
            Debug.Log($"Moving: {heaviestDir}, Distance from {target.transform.name}: {(target.transform.position - transform.position).magnitude}, Avoidance: {CalculateObjectAvoidance(heaviestDir)}, State: {currentState}");
        } // DEBUGGING
        return heaviestDir.normalized;
    }

    private Vector2 GetMovement() // Called from update, gets a movement direction
    {
        Vector2 direction = Vector2.zero;
        if (target != null)
        {
            if (currentState == Activity.IDLE) ChangeState(Activity.CHASING);
            direction = GetCombatMovement();
        }
        else ChangeState(Activity.IDLE);
        return direction;
    }

    // Core

    private float GetMutationPercent(float chance, float multiplier)
    {
        if (chance == 0) return 1;
        return 1 + Random.Range(-chance, chance) * multiplier;
    }

    private float GetMutationPercent(float multiplier)
    {
        return GetMutationPercent(randomMutationChance, multiplier);
    }

    private float GetMutationPercent()
    {
        return GetMutationPercent(1);
    }

    protected void Mutate()
    {
        reactionTime *= GetMutationPercent(1.2f);
        rushTime *= GetMutationPercent(0.5f);
        keepDistanceBetween += GetMutationPercent(randomMutationChance * 2, 3) - 1;
    }

    protected new void Awake()
	{
        base.Awake();
        startPos = transform.position;
        Mutate();

        ResetRushCooldown();
    }

    protected new void Update()
    {
        base.Update();
        rushCooldown -= Time.deltaTime;
        SetMovement(GetMovement());
        if (target) CheckForAttack(target.transform);
    }
}
