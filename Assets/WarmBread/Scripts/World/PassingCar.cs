using UnityEngine;

namespace WarmBread
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PassingCar : MonoBehaviour
    {
        [SerializeField] private float speed = 3.5f;
        [SerializeField] private float leftBoundary = -35f;
        [SerializeField] private float rightBoundary = 35f;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void FixedUpdate()
        {
            if (body == null || Time.timeScale <= 0f) return;

            var local = transform.localPosition;
            local.x += speed * Time.fixedDeltaTime;
            if (local.x > rightBoundary) local.x = leftBoundary;

            var worldPosition = transform.parent != null
                ? transform.parent.TransformPoint(local)
                : local;

            body.MovePosition(worldPosition);
        }
    }
}
