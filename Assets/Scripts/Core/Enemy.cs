using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Zodiac.Core;
using Zodiac.Util;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private HealthController target;
    [SerializeField, Min(0)] private float cooldown = 0.2f;
    [SerializeField, Min(0)] private float attackDist = 4f;
    [SerializeField, Min(0)] private float windup = 0.5f;
    [SerializeField, Min(0)] private float damage = 5f;
    [SerializeField] private Color color = Color.black;

    private NavMeshAgent agent;
    private StateMachine<Enemy> ai;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        ai = new(this, new SeekingState());
    }

    void Update() => ai.Update();

    public void Destroy() => GameObject.Destroy(this.gameObject);

    class SeekingState : State<Enemy>
    {
        private float lastUpdated;

        public override bool Update(Enemy enemy, out State<Enemy> next)
        {
            if (Vector3.Distance(enemy.target.transform.position, enemy.transform.position) < enemy.attackDist)
            {
                next = new AttackingState(enemy.windup);
                return true;
            }

            if (Time.time - lastUpdated > enemy.cooldown)
            {
                enemy.agent.SetDestination(enemy.target.transform.position);
                lastUpdated = Time.time;
            }

            if (enemy.agent.hasPath)
            {
                _ = enemy.agent.path.corners.Aggregate((p1, p2) =>
                {
                    Debug.DrawLine(p1, p2, enemy.color);
                    return p2;
                });
            }

            next = null;
            return false;
        }
    }

    class AttackingState : State<Enemy>
    {
        private readonly float waitTime;

        public AttackingState(float windup) => waitTime = Time.time + windup;

        public override bool Update(Enemy enemy, out State<Enemy> next)
        {
            if (Time.time < waitTime)
            {
                next = null;
                return false;
            }

            next = new SeekingState();
            return true;
        }

        public override void OnExit(Enemy enemy)
        {
            if (Vector3.Distance(enemy.target.transform.position, enemy.transform.position) > enemy.attackDist) return;
            enemy.target.Damage(enemy.damage);
        }
    }
}