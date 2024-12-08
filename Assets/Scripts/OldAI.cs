using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldAI : Entity
{

    public Entity target;

    private bool favorLeft = true;
    private bool reacting = false;
    private bool attack = false; // Do or do not rush the enemy
    private bool attacking = false; // Actively swinging or not

    private bool DEBUG = false;

    [SerializeField, Range(0, 1f)] private float rushChance = 0.2f;
    [SerializeField, Range(0, 0.5f)] private float dodgeChance = 0.4f;
    [SerializeField, Range(0, 0.5f)] private float counterChance = 0.2f;
    [SerializeField, Range(1, 20)] private float distanceWanted = 2f;
    [SerializeField, Range(0, 1.5f)] private float attackDistance = 1.5f;
    [Range(1, 20)] private float startDistance;

    // General methods

    public override void Damage(int damage, Vector2 push)
    {
        base.Damage(damage, push);
        attack = false;
    }

    public override Vector2 GetForward()
    {
        if (GetSpeed().magnitude > 0)
        {
            return (GetSpeed() * 1000).normalized;
        }
        return Vector2.up;
    }

    // Methods for combat with AI

    private float attackTimer = 0;
    private void CalcRandomRushOrbit()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer > 2)
        {
            attackTimer -= 1f;
            if (Random.Range(0, 1f) <= rushChance)
            {
                attack = true;
            }
            distanceWanted = startDistance * Random.Range(0.8f, 1.5f);
        }
    }

    private IEnumerator Attack()
    {
        if (attacking)
        {
            yield break;
        }
        attacking = true;
        weapon.Primary();
        attack = false;
        yield return new WaitForSeconds(weapon.GetAttackSpeed());
        attacking = false;
    }

    private IEnumerator React()
    {
        if (reacting || attacking)
        {
            yield break;
        }
        reacting = true;
        float chance = Random.Range(0, 1f);
        if (chance <= counterChance)
        {
            Vector2 dir = ((target.transform.position - transform.position).normalized * 1000).normalized;
            Stun(0.4f);
            Push(dir, 25);
            StartCoroutine(Attack());
            if (DEBUG)
            {
                Debug.Log("Countering Target: " + target.transform.ToString());
            } // DEBUG
        }
        else if (chance <= dodgeChance + counterChance)
        {
            Vector2 dir = Vector2.Perpendicular(((target.transform.position - transform.position).normalized * 1000).normalized);
            if (!favorLeft)
            {
                dir = -dir;
            }
            Stun(1f);
            Push(dir, 20);
            if (DEBUG)
            {
                Debug.Log("Dodging Target: " + target.transform.ToString());
            } // DEBUG
        }
        else
        {
            if (DEBUG)
            {
                Debug.Log("Defending From Target: " + target.transform.ToString());
            } // DEBUG
        }
        yield return new WaitForSeconds(1);
        reacting = false;
    }

    // Weight methods

    private RaycastHit2D CheckForObstrcution(Vector2 dir, float range)
    {
        Vector2 center = transform.position;
        RaycastHit2D closest = new RaycastHit2D();
        RaycastHit2D[] hits = Physics2D.CircleCastAll(center, 0.25f, dir, range);
        if (hits.Length > 0)
        {
            closest = hits[0];
            foreach (RaycastHit2D hit in hits)
            {
                if ((closest.point - center).magnitude <= (hit.point - center).magnitude)
                {
                    closest = hit;
                }
            }
        }
        return closest;
    }

    private float WeighObstruction(Vector2 dir)
    {
        float weight = 1f;
        RaycastHit2D hit = CheckForObstrcution(dir, 2f);
        if (hit)
        {
            if (hit.transform != target.transform)
            {
                float distanceWeight = (hit.distance - 1) / 2;
                weight *= distanceWeight;
            }
        }
        return weight;
    }

    private Vector2 GetTargetDirection()
    {
        return (target.transform.position - transform.position).normalized;
    }

    // Movment Core Systems

    private float WeighDirection(Vector3 dir)
    {
        Vector2 targetDir = GetTargetDirection();
        float targetDistance = (target.transform.position - transform.position).magnitude;

        float directionDot = Vector2.Dot(dir, targetDir);
        float obstruction = WeighObstruction(dir);
        if (obstruction <= 0.2f)
        {
            return 0.1f;
        }

        float weight = (directionDot + 0.5f) / 1.5f;
        if (targetDistance < distanceWanted * 2)
        {
            if (targetDistance <= attackDistance && !attacking)
            {
                StartCoroutine(Attack());
            }
            if (Mathf.Abs(directionDot) >= 0.15f)
            {
                weight *= targetDistance / (distanceWanted * 2) * 2 - 1;
            }
            else
            {
                Vector2 favor = Vector2.Perpendicular(targetDir);
                if (!favorLeft)
                {
                    favor *= -1;
                }
                if (Random.Range(0, 1000) == 0)
                {
                    favorLeft = !favorLeft;
                }
                float favorDotWeight = Vector2.Dot(favor, dir);
                weight *= favorDotWeight;
            }
            if (targetDistance < distanceWanted)
            {
                if (targetDistance < attackDistance && !reacting)
                {
                    StartCoroutine(React());
                }
                if (directionDot < 0.1f)
                {
                    weight = -distanceWanted / targetDistance * directionDot;
                }
            }
            if (attack)
            {
                weight = (directionDot + 0.5f) / 1.5f;
            }
        }

        if (!attack)
        {
            float visionDot = Vector2.Dot(target.GetForward(), dir);
            //weight *= (visionDot + 19) / 20;
        }

        return weight * obstruction;
    }

    private Vector3 WeighCombatMoves()
    {
        int totalDirs = 20;
        float rotation = Mathf.Atan2(GetForward().y, GetForward().x);
        List<Vector2> dirs = new List<Vector2>();
        GetComponent<Collider2D>().enabled = false;

        for (int i = 0; i < totalDirs; i++)
        {
            Vector2 newDir = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));
            rotation += 360 / totalDirs * Mathf.Deg2Rad;
            float weight = WeighDirection(newDir);
            if (weight > 0.01f)
            {
                newDir *= weight;
                dirs.Add(newDir);
            }
            else
            {
                if (DEBUG)
                {
                    Debug.DrawLine(transform.position, transform.position - (Vector3)newDir * weight, Color.red, 0.01f);
                } // DEBUG
            }
        }

        GetComponent<Collider2D>().enabled = true;
        Vector2 weightedDir = Vector2.zero;

        foreach (Vector2 dir in dirs)
        {
            if (dir.magnitude > weightedDir.magnitude)
            {
                weightedDir = dir;
            }
        }
        if (DEBUG)
        {
            foreach (Vector2 dir in dirs)
            {
                if (dir != weightedDir)
                {
                    Debug.DrawLine(transform.position, transform.position + (Vector3)dir / weightedDir.magnitude, Color.green, 0.01f);
                }
                else
                {
                    Debug.DrawLine(transform.position, transform.position + (Vector3)dir / weightedDir.magnitude, Color.blue, 0.01f);
                }
            }
        } // DEBUG

        weightedDir = (weightedDir * 100).normalized;
        if (weapon != null)
        {
            weapon.Aim(GetTargetDirection());
        }
        return weightedDir;
    }

    private Vector3 GetMoveDirection()
    {
        Vector2 newDir = Vector2.zero;
        if (target)
        {
            CalcRandomRushOrbit();
            newDir = WeighCombatMoves();
        }
        newDir = Vector2.Lerp(newDir, GetForward(), 0.2f);
        return newDir;
    }

    // Core

    private new void Awake()
    {
        base.Awake();
        if (Random.Range(0, 2) == 0)
        {
            favorLeft = false;
        }
        attackDistance *= Random.Range(0.9f, 1.1f);
        startDistance = distanceWanted;
    }

    private new void Update()
    {
        base.Update();
        Vector3 moveDir = GetMoveDirection();
        SetMovement(moveDir);
    }
}
