using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class NavigationRuntimeSettings
    {
        private const float DefaultAvoidMultiplier = 5f;
        private const float DefaultPreferMultiplier = 0.7f;
        private const float DefaultStartSnapDistance = 200f;
        private const float DefaultDestinationSnapDistance = 50f;
        private const float DefaultArrivalDistance = 10f;
        private const float DefaultTurnedAroundDistance = 30f;
        private const float DefaultRerouteCooldown = 20f;
        private const float DefaultMinHeadingSpeed = 1f;
        private const float DefaultStoppedSpeed = 0.1f;
        private const float DefaultTeleportDistance = 50f;
        private const float DefaultLeaveMargin = 3f;

        [SerializeField] private NavigationFormatter formatter;
        [SerializeField] private GameObject playerMarkerPrefab;
        [SerializeField] private GameObject destinationMarkerPrefab;
        [SerializeField] private GameObject previewPinPrefab;
        [SerializeField] private GameObject defaultMarkerPrefab;
        [SerializeField] private float avoidMultiplier = DefaultAvoidMultiplier;
        [SerializeField] private float preferMultiplier = DefaultPreferMultiplier;
        [SerializeField] private float startSnapDistance = DefaultStartSnapDistance;
        [SerializeField] private float destinationSnapDistance = DefaultDestinationSnapDistance;
        [SerializeField] private float arrivalDistance = DefaultArrivalDistance;
        [SerializeField] private float turnedAroundDistance = DefaultTurnedAroundDistance;
        [SerializeField] private float rerouteCooldown = DefaultRerouteCooldown;
        [SerializeField] private float minHeadingSpeed = DefaultMinHeadingSpeed;
        [SerializeField] private float stoppedSpeed = DefaultStoppedSpeed;
        [SerializeField] private float teleportDistance = DefaultTeleportDistance;
        [SerializeField] private float leaveMargin = DefaultLeaveMargin;

        public NavigationFormatter Formatter { get { return formatter; } }
        public GameObject PlayerMarkerPrefab { get { return playerMarkerPrefab; } }
        public GameObject DestinationMarkerPrefab { get { return destinationMarkerPrefab; } }
        public GameObject PreviewPinPrefab { get { return previewPinPrefab; } }
        public GameObject DefaultMarkerPrefab { get { return defaultMarkerPrefab; } }
        public float AvoidMultiplier { get { return avoidMultiplier; } }
        public float PreferMultiplier { get { return preferMultiplier; } }
        public float StartSnapDistance { get { return startSnapDistance; } }
        public float DestinationSnapDistance { get { return destinationSnapDistance; } }
        public float ArrivalDistance { get { return arrivalDistance; } }
        public float TurnedAroundDistance { get { return turnedAroundDistance; } }
        public float RerouteCooldown { get { return rerouteCooldown; } }
        public float MinHeadingSpeed { get { return minHeadingSpeed; } }
        public float StoppedSpeed { get { return stoppedSpeed; } }
        public float TeleportDistance { get { return teleportDistance; } }
        public float LeaveMargin { get { return leaveMargin; } }

        internal void SetFormatter(NavigationFormatter value)
        {
            formatter = value;
        }

        internal void SetPlayerMarkerPrefab(GameObject value)
        {
            playerMarkerPrefab = value;
        }

        internal void SetDestinationMarkerPrefab(GameObject value)
        {
            destinationMarkerPrefab = value;
        }

        internal void SetPreviewPinPrefab(GameObject value)
        {
            previewPinPrefab = value;
        }

        internal void SetDefaultMarkerPrefab(GameObject value)
        {
            defaultMarkerPrefab = value;
        }

        internal void SetAvoidMultiplier(float value)
        {
            avoidMultiplier = value;
        }

        internal void SetPreferMultiplier(float value)
        {
            preferMultiplier = value;
        }

        internal void SetStartSnapDistance(float value)
        {
            startSnapDistance = value;
        }

        internal void SetDestinationSnapDistance(float value)
        {
            destinationSnapDistance = value;
        }

        internal void SetArrivalDistance(float value)
        {
            arrivalDistance = value;
        }

        internal void SetTurnedAroundDistance(float value)
        {
            turnedAroundDistance = value;
        }

        internal void SetRerouteCooldown(float value)
        {
            rerouteCooldown = value;
        }

        internal void SetMinHeadingSpeed(float value)
        {
            minHeadingSpeed = value;
        }

        internal void SetStoppedSpeed(float value)
        {
            stoppedSpeed = value;
        }

        internal void SetTeleportDistance(float value)
        {
            teleportDistance = value;
        }

        internal void SetLeaveMargin(float value)
        {
            leaveMargin = value;
        }

        internal void ResetTuningToDefaults()
        {
            avoidMultiplier = DefaultAvoidMultiplier;
            preferMultiplier = DefaultPreferMultiplier;
            startSnapDistance = DefaultStartSnapDistance;
            destinationSnapDistance = DefaultDestinationSnapDistance;
            arrivalDistance = DefaultArrivalDistance;
            turnedAroundDistance = DefaultTurnedAroundDistance;
            rerouteCooldown = DefaultRerouteCooldown;
            minHeadingSpeed = DefaultMinHeadingSpeed;
            stoppedSpeed = DefaultStoppedSpeed;
            teleportDistance = DefaultTeleportDistance;
            leaveMargin = DefaultLeaveMargin;
        }
    }
}
