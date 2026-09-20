using UnityEngine;
using UnityEngine.AI;

namespace WarmBread
{
    public sealed class CustomerAI : MonoBehaviour
    {
        public enum State
        {
            Arriving,
            Waiting,
            Ordering,
            Paying,
            Leaving
        }

        public State CurrentState { get; private set; }
        public CustomerData Data { get; private set; }
        public Order Order { get; private set; }
        public float WaitSeconds { get; private set; }
        public float Patience => Mathf.Clamp01(1f - WaitSeconds / Mathf.Max(1f, maxWaitSeconds));

        private NavMeshAgent agent;
        private QueueManager queue;
        private CustomerVisualRig rig;
        private float maxWaitSeconds = 120f;
        private float phase;
        private float idlePhase;
        private float lifetime;
        private bool abandonRequested;

        public void Initialize(CustomerData data, Order order, QueueManager owner)
        {
            Data = data;
            Order = order;
            queue = owner;

            var child = data != null && data.Child;
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = child ? 1.65f : 1.15f;
            agent.radius = child ? .22f : .27f;
            agent.height = child ? 1.45f : 1.82f;
            agent.stoppingDistance = .12f;
            agent.acceleration = child ? 7f : 5f;
            agent.angularSpeed = 240f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            var session = owner != null ? owner.Session : null;
            var weatherPenalty = session != null && session.CurrentPlan != null &&
                                 (session.CurrentPlan.Weather == WeatherKind.Rain ||
                                  session.CurrentPlan.Weather == WeatherKind.Wind)
                ? -12f
                : 0f;
            var reputationBonus = session != null ? (session.Reputation - 50) * .35f : 0f;
            maxWaitSeconds = Mathf.Clamp(120f + reputationBonus + weatherPenalty + (child ? 10f : 0f), 75f, 165f);

            rig = ModelFactory.BuildCustomer(transform, data);
            CurrentState = State.Arriving;
            idlePhase = Mathf.Abs(GetInstanceID() % 1000) * .01f;
        }

        public void MoveTo(Vector3 position)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(position);
            }
        }

        public void SetPaying()
        {
            if (CurrentState != State.Leaving) CurrentState = State.Paying;
        }

        public void Leave()
        {
            CurrentState = State.Leaving;
            lifetime = 0f;
            abandonRequested = true;
            MoveTo(new Vector3(18f, .1f, 4f));
        }

        private void Update()
        {
            if (queue == null || queue.Session == null || queue.Session.Paused || !queue.Session.Running) return;

            lifetime += Time.deltaTime;
            if (CurrentState == State.Leaving)
            {
                if (lifetime > 25f || transform.position.x > 17f) Destroy(gameObject);
                Animate();
                return;
            }

            if (CurrentState != State.Paying)
            {
                WaitSeconds += Time.deltaTime;
                if (WaitSeconds >= maxWaitSeconds && !abandonRequested)
                {
                    abandonRequested = true;
                    queue.Abandon(this);
                    return;
                }
            }

            if (agent != null &&
                agent.enabled &&
                agent.isOnNavMesh &&
                !agent.pathPending &&
                agent.remainingDistance < .3f)
            {
                if (queue.Front == this && CurrentState != State.Paying)
                {
                    if (CurrentState != State.Ordering)
                    {
                        EventBus.Say(Data != null ? Data.Greeting : "Здравствуйте.");
                        EventBus.Refresh();
                    }

                    CurrentState = State.Ordering;
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.Euler(0f, 180f, 0f),
                        Time.deltaTime * 4f);
                }
                else if (CurrentState != State.Paying)
                {
                    CurrentState = State.Waiting;
                }
            }

            Animate();
        }

        private void Animate()
        {
            if (rig == null) return;

            var speed = agent != null && agent.enabled && agent.isOnNavMesh
                ? agent.velocity.magnitude
                : 0f;
            phase += Time.deltaTime * Mathf.Lerp(4.5f, 8f, Mathf.Clamp01(speed / 1.5f));
            var stride = speed > .08f ? Mathf.Sin(phase) * 26f : 0f;
            var armStride = speed > .08f ? -Mathf.Sin(phase) * 18f : 0f;

            if (rig.LeftLeg != null)
            {
                rig.LeftLeg.localRotation = Quaternion.Lerp(
                    rig.LeftLeg.localRotation,
                    Quaternion.Euler(stride, 0f, 0f),
                    Time.deltaTime * 12f);
            }

            if (rig.RightLeg != null)
            {
                rig.RightLeg.localRotation = Quaternion.Lerp(
                    rig.RightLeg.localRotation,
                    Quaternion.Euler(-stride, 0f, 0f),
                    Time.deltaTime * 12f);
            }

            var payingPose = CurrentState == State.Paying ? -32f : 0f;
            var orderingGesture = CurrentState == State.Ordering
                ? Mathf.Sin(Time.time * 1.9f + idlePhase) * 7f
                : 0f;

            if (rig.LeftArm != null)
            {
                rig.LeftArm.localRotation = Quaternion.Lerp(
                    rig.LeftArm.localRotation,
                    Quaternion.Euler(armStride + payingPose, 0f, CurrentState == State.Paying ? -10f : 0f),
                    Time.deltaTime * 8f);
            }

            if (rig.RightArm != null)
            {
                rig.RightArm.localRotation = Quaternion.Lerp(
                    rig.RightArm.localRotation,
                    Quaternion.Euler(-armStride + payingPose + orderingGesture, 0f, CurrentState == State.Paying ? 10f : 0f),
                    Time.deltaTime * 8f);
            }

            if (rig.Head != null)
            {
                var look = Mathf.Sin(Time.time * .72f + idlePhase) * 7f;
                var nod = CurrentState == State.Ordering
                    ? Mathf.Sin(Time.time * 1.35f + idlePhase) * 2.5f
                    : 0f;
                rig.Head.localRotation = Quaternion.Lerp(
                    rig.Head.localRotation,
                    Quaternion.Euler(nod, look, 0f),
                    Time.deltaTime * 4f);
            }

            if (rig.Root != null)
            {
                var breathe = Mathf.Sin(Time.time * 1.8f + idlePhase) * .006f;
                rig.Root.localPosition = new Vector3(0f, breathe, 0f);
            }
        }
    }
}
