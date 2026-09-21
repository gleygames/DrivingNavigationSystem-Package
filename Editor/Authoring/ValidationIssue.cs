using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public enum ValidationIssueKind
    {
        NearMiss,
        Duplicate,
        Island,
        OneWayTrap,
        OutsideMap,
        GroundMiss
    }

    public class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public ValidationIssueKind Kind { get; }
        public Vector3 Position { get; }
        public string Message { get; }
        public int RoadId { get; }
        public int IntersectionId { get; }

        public ValidationIssue(ValidationSeverity severity, ValidationIssueKind kind, int roadId, int intersectionId, Vector3 position, string message)
        {
            Severity = severity;
            Kind = kind;
            RoadId = roadId;
            IntersectionId = intersectionId;
            Position = position;
            Message = message;
        }
    }
}
