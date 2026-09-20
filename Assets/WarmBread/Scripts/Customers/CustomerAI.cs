using UnityEngine;
using UnityEngine.AI;

namespace WarmBread
{
    public sealed class CustomerAI : MonoBehaviour
    {
        public enum State { Arriving, Waiting, Ordering, Paying, Leaving }
        public State CurrentState { get; private set; }
        public CustomerData Data { get; private set; }
        public Order Order { get; private set; }
        public float WaitSeconds { get; private set; }
        public float Patience => Mathf.Clamp01(1 - WaitSeconds / 120);
        private NavMeshAgent agent;
        private QueueManager queue;
        private Transform leftLeg, rightLeg, head;
        private float phase;
        private float lifetime;
        public void Initialize(CustomerData data, Order order, QueueManager owner)
        {
            Data = data; Order = order; queue = owner;
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = data.Child ? 1.65f : 1.15f; agent.radius = .26f; agent.height = 1.8f;
            agent.stoppingDistance = .12f; agent.acceleration = 5; agent.angularSpeed = 220;
            if (NavMesh.SamplePosition(transform.position, out var hit, 3, NavMesh.AllAreas)) agent.Warp(hit.position);
            var coat = WorldArt.Material("Coat " + data.DisplayName, data.Coat);
            var skin = WorldArt.Material("Skin " + data.DisplayName, new Color(.69f, .52f, .4f));
            var dark = WorldArt.Material("Trousers " + data.DisplayName, new Color(.15f, .17f, .18f));
            var body = new GameObject("Body").transform; body.SetParent(transform, false);
            WorldArt.Part("coat", body, new Vector3(0, 1.06f, 0), new Vector3(.54f, .72f, .32f), coat);
            head = WorldArt.Part("head", body, new Vector3(0, 1.65f, 0), new Vector3(.32f, .36f, .3f), skin).transform;
            WorldArt.Part("cap", body, new Vector3(0, 1.84f, -.015f), new Vector3(.37f, .11f, .34f), dark);
            for (int side = -1; side <= 1; side += 2)
            {
                WorldArt.Part("eye", body, new Vector3(side * .075f, 1.69f, .156f), new Vector3(.024f, .024f, .01f), dark);
                WorldArt.Part("sleeve", body, new Vector3(side * .34f, 1.04f, 0), new Vector3(.16f, .63f, .21f), coat);
                WorldArt.Part("hand", body, new Vector3(side * .34f, .7f, 0), new Vector3(.14f, .18f, .16f), skin);
                var leg = new GameObject(side < 0 ? "Left leg pivot" : "Right leg pivot").transform;
                leg.SetParent(body, false); leg.localPosition = new Vector3(side * .145f, .7f, 0);
                WorldArt.Part("trousers", leg, new Vector3(0, -.31f, 0), new Vector3(.21f, .62f, .23f), dark);
                WorldArt.Part("shoe", leg, new Vector3(0, -.64f, .055f), new Vector3(.23f, .13f, .35f), dark);
                if (side < 0) leftLeg = leg; else rightLeg = leg;
            }
            if (data.Child) body.localScale = Vector3.one * .8f;
            CurrentState = State.Arriving;
        }
        public void MoveTo(Vector3 position)
        { if (agent.isOnNavMesh) agent.SetDestination(position); }
        public void SetPaying() => CurrentState = State.Paying;
        public void Leave()
        { CurrentState = State.Leaving; lifetime = 0; MoveTo(new Vector3(18, .1f, 4)); }
        private void Update()
        {
            if (queue == null || queue.Session.Paused || !queue.Session.Running) return;
            lifetime += Time.deltaTime;
            if (CurrentState == State.Leaving)
            { if (lifetime > 25 || transform.position.x > 17) Destroy(gameObject); return; }
            WaitSeconds += Time.deltaTime;
            if (WaitSeconds >= 120) { queue.Abandon(this); return; }
            if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < .3f)
            {
                if (queue.Front == this && CurrentState != State.Paying)
                {
                    if (CurrentState != State.Ordering) { EventBus.Say(Data.Greeting); EventBus.Refresh(); }
                    CurrentState = State.Ordering;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 180, 0), Time.deltaTime * 4);
                }
                else if (CurrentState != State.Paying) CurrentState = State.Waiting;
            }
            phase += Time.deltaTime * 7;
            float stride = agent.velocity.magnitude > .1f ? Mathf.Sin(phase) * 24 : 0;
            leftLeg.localRotation = Quaternion.Euler(stride, 0, 0); rightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
            head.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .7f + GetInstanceID()) * 6, 0);
        }
    }
}
