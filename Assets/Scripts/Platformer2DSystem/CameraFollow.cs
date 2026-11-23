using UnityEngine;

namespace Platformer2DSystem.Example
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float speed = 5f;

        [Header("Vertical")]
        [SerializeField] private bool followY = true;
        [SerializeField] private float verticalSpeed = 3f;
        [SerializeField] private float verticalOffset = 2f;

        [Header("Límits")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minPosition = new(-1000f, -1000f);
        [SerializeField] private Vector2 maxPosition = new(1000f, 1000f);

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minPosition = min;
            maxPosition = max;
            useBounds = true;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 position = transform.position;
            position.x = Maths.Damp(position.x, target.position.x, speed, Time.deltaTime);
            if (followY)
                position.y = Maths.Damp(position.y, target.position.y + verticalOffset, verticalSpeed, Time.deltaTime);

            if (useBounds)
            {
                position.x = Mathf.Clamp(position.x, minPosition.x, maxPosition.x);
                position.y = Mathf.Clamp(position.y, minPosition.y, maxPosition.y);
            }

            transform.position = position;
        }
    }
}
