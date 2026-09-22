using UnityEngine;

namespace Gley.NavigationSystem
{
    internal class VehicleMotion
    {
        private const float DefaultTeleportDistance = 50f;
        private const float DefaultStoppedSpeed = 0.1f;
        private const float DefaultMinHeadingSpeed = 1f;

        private Vector3 lastTruePosition;

        public Vector3 NoseHeading { get; private set; }
        public Vector3 MovementHeading { get; private set; }
        public float TeleportDistance { get; set; }
        public float StoppedSpeed { get; set; }
        public float MinHeadingSpeed { get; set; }
        public float Speed { get; private set; }
        public bool HasMovedOnce { get; private set; }
        public bool Teleported { get; private set; }
        public bool IsStopped { get; private set; }
        public bool IsReversing { get; private set; }

        public VehicleMotion()
        {
            TeleportDistance = DefaultTeleportDistance;
            StoppedSpeed = DefaultStoppedSpeed;
            MinHeadingSpeed = DefaultMinHeadingSpeed;
        }

        public void UpdateVehicleMotionLogic(Vector3 truePos, Vector3 trueNose, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Teleported = false;

            float displacementX = truePos.x - lastTruePosition.x;
            float displacementZ = truePos.z - lastTruePosition.z;
            float displacement = Mathf.Sqrt(displacementX * displacementX + displacementZ * displacementZ);

            if (displacement > TeleportDistance)
            {
                Reset(truePos, trueNose);
                Teleported = true;
                return;
            }

            Speed = displacement / deltaTime;
            IsStopped = Speed < StoppedSpeed;

            NoseHeading = FlattenHeading(trueNose, NoseHeading);

            if (Speed >= MinHeadingSpeed)
            {
                MovementHeading = new Vector3(displacementX, 0f, displacementZ).normalized;
                HasMovedOnce = true;
            }
            else if (!HasMovedOnce)
            {
                MovementHeading = NoseHeading;
            }

            IsReversing = Vector3.Dot(MovementHeading, NoseHeading) < 0f;

            lastTruePosition = truePos;
        }

        public void Reset(Vector3 truePos, Vector3 trueNose)
        {
            lastTruePosition = truePos;
            HasMovedOnce = false;
            Teleported = false;
            Speed = 0f;
            IsStopped = true;
            NoseHeading = FlattenHeading(trueNose, NoseHeading);
            MovementHeading = NoseHeading;
            IsReversing = false;
        }

        private Vector3 FlattenHeading(Vector3 source, Vector3 fallback)
        {
            Vector3 flattened = new Vector3(source.x, 0f, source.z);
            if (flattened.sqrMagnitude < 0.0001f)
            {
                return fallback;
            }
            return flattened.normalized;
        }
    }
}
