using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Rival.Samples.OnlineFPS
{
    public class NameplateBehaviour : MonoBehaviour
    {
        public Text NameplateText;
        public Transform PivotTransform;

        private Entity _followedEntity;
        private World _followedWorld;
        private Camera _camera;
        private Entity _previousOwningPlayer;

        private void Start()
        {
            _camera = Camera.main;
        }

        public void Setup(Entity entity, World world, float verticalOffset)
        {
            _followedEntity = entity;
            _followedWorld = world;

            Vector3 pivotLocalPos = PivotTransform.localPosition;
            pivotLocalPos.y = verticalOffset;
            PivotTransform.localPosition = pivotLocalPos;
        }

        void LateUpdate()
        {
            if (_followedEntity == Entity.Null || !_followedWorld.EntityManager.HasComponent<LocalToWorld>(_followedEntity))
            {
                Destroy(this.gameObject);
            }
            else if (_camera && _followedWorld != null)
            {
                Vector3 faceDirection = (_camera.transform.position - PivotTransform.position).normalized;
                PivotTransform.forward = faceDirection;

                transform.position = _followedWorld.EntityManager.GetComponentData<LocalToWorld>(_followedEntity).Position;

                // Auto detect name changes
                Entity newOwningPlayer = default;
                if (_followedWorld.EntityManager.HasComponent<OwningPlayer>(_followedEntity))
                {
                    newOwningPlayer = _followedWorld.EntityManager.GetComponentData<OwningPlayer>(_followedEntity).PlayerEntity;
                }
                if (newOwningPlayer != _previousOwningPlayer)
                {
                    OnOwnerChanged(newOwningPlayer);
                }
                _previousOwningPlayer = newOwningPlayer;
            }
        }

        private void OnOwnerChanged(Entity newOwningPlayer)
        {
            if(newOwningPlayer == Entity.Null)
            {
                PivotTransform.gameObject.SetActive(false);
            }
            else
            {
                if (_followedWorld.EntityManager.HasComponent<OnlineFPSPlayer>(newOwningPlayer))
                {
                    PivotTransform.gameObject.SetActive(true);

                    NameplateText.text = _followedWorld.EntityManager.GetComponentData<OnlineFPSPlayer>(newOwningPlayer).PlayerName.ToString();
                }
            }
        }
    }
}