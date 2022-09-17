using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Rival.Samples
{
    public struct CameraObstructionHitsCollector : ICollector<ColliderCastHit>
    {
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction => 1f;
        public int NumHits { get; private set; }

        public ColliderCastHit ClosestHit;

        private PhysicsWorld _physicsWorld;
        private float _closestHitFraction;
        private float3 _cameraDirection;
        private DynamicBuffer<IgnoredEntityBufferElement> _ignoredEntitiesBuffer;

        public CameraObstructionHitsCollector(in PhysicsWorld physicsWorld, DynamicBuffer<IgnoredEntityBufferElement> ignoredEntitiesBuffer, float3 cameraDirection)
        {
            NumHits = 0;
            ClosestHit = default;

            _closestHitFraction = float.MaxValue;
            _physicsWorld = physicsWorld;
            _cameraDirection = cameraDirection;
            _ignoredEntitiesBuffer = ignoredEntitiesBuffer;
        }

        public bool AddHit(ColliderCastHit hit)
        {
            if (math.dot(hit.SurfaceNormal, _cameraDirection) < 0f || !PhysicsUtilities.IsCollidable(hit.Material))
            {
                return false;
            }

            if (hit.RigidBodyIndex >= 0 && PhysicsUtilities.IsBodyDynamic(in _physicsWorld, hit.RigidBodyIndex))
            {
                return false;
            }

            for (int i = 0; i < _ignoredEntitiesBuffer.Length; i++)
            {
                if (_ignoredEntitiesBuffer[i].Entity == hit.Entity)
                {
                    return false;
                }
            }

            // Process valid hit
            if (hit.Fraction < _closestHitFraction)
            {
                _closestHitFraction = hit.Fraction;
                ClosestHit = hit;
            }
            NumHits++;

            return true;
        }
    }
}