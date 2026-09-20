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

        private const float MaxWaitSeconds = 120f;

        public State CurrentState { get; private set; }
        public CustomerData Data { get; private set; }
        public Order Order { get; private set; }
        public float WaitSeconds { get; private set; }
        public float Patience => Mathf.Clamp01(1f - WaitSeconds / MaxWaitSeconds);

        private NavMeshAgent agent;
        private QueueManager queue;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform head;
        private float phase;
        private float lifetime;
        private bool abandonRequested;

        public void Initialize(CustomerData data, Order order, QueueManager owner)
        {
            Data = data;
            Order = order;
            queue = owner;

            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = data != null && data.Child ? 1.65f : 1.15f;
            agent.radius = .26f;
            agent.height = 1.8f;
            agent.stoppingDistance = .12f;
            agent.acceleration = 5f;
            agent.angularSpeed = 220f;

            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            var displayName = data != null ? data.DisplayName : "Покупатель";
            var coatColor = data != null ? data.Coat : new Color(.32f, .34f, .35f);
            var coat = WorldArt.Material("Coat " + displayName, coatColor);
            var skin = WorldArt.Material("Skin " + displayName, new Color(.69f, .52f, .4f));
            var dark = WorldArt.Material("Trousers " + displayName, new Color(.15f, .17f, .18f));

            var body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            WorldArt.Part("coat", body, new Vector3(0f, 1.06f, 0f), new Vector3(.54f, .72f, .32f), coat);
            head = WorldArt.Part("head", body, new Vector3(0f, 1.65f, 0f), new Vector3(.32f, .36f, .3f), skin).transform;
            WorldArt.Part("cap", body, new Vector3(0f, 1.84f, -.015f), new Vector3(.37f, .11f, .34f), dark);

            for (var side = -1; side <= 1; side += 2)
            {
                WorldArt.Part("eye", body, new Vector3(side * .075f, 1.69f, .156f), new Vector3(.024f, .024f, .01f), dark);
                WorldArt.Part("sleeve", body, new Vector3(side * .34f, 1.04f, 0f), new Vector3(.16f, .63f, .21f), coat);
                WorldArt.Part("hand", body, new Vector3(side * .34f, .7f, 0f), new Vector3(.14f, .18f, .16f), skin);

                var leg = new GameObject(side < 0 ? "Left leg pivot" : "Right leg pivot").transform;
                leg.SetParent(body, false);
                leg.localPosition = new Vector3(side * .145f, .7f, 0f);
                WorldArt.Part("trousers", leg, new Vector3(0f, -.31f, 0f), new Vector3(.21f, .62f, .23f), dark);
                WorldArt.Part("shoe", leg, new Vector3(0f, -.64f, .055f), new Vector3(.23f, .13f, .35f), dark);

                if (side < 0) leftLeg = leg;
                else rightLeg = leg;
            }

            if (data != null && data.Child) body.localScale = Vector3.one * .8f;
            CurrentState = State.Arriving;
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
                return;
            }

            if (CurrentState != State.Paying)
            {
                WaitSeconds += Time.deltaTime;
                if (WaitSeconds >= MaxWaitSeconds && !abandonRequested)
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

            phase += Time.deltaTime * 7f;
            var velocity = agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
            var stride = velocity > .1f ? Mathf.Sin(phase) * 24f : 0f;

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(stride, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-stride, 0f, 0f);
            if (head != null)
            {
                head.localRotation = Quaternion.Euler(
                    0f,
                    Mathf.Sin(Time.time * .7f + GetInstanceID()) * 6f,
                    0f);
            }
        }
    }
}
