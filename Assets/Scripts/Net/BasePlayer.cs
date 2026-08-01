using LiteEntitySystem;
using LiteEntitySystem.Extensions;
using UnityEngine;

namespace Zodiac.Net
{
    [EntityFlags(EntityFlags.UpdateOnClient)]
    public class BasePlayer : PawnLogic
    {
        [SyncVarFlags(SyncFlags.Interpolated)] private SyncVar<Vector3> position;
        [SyncVarFlags(SyncFlags.Interpolated)] private SyncVar<FloatAngle> yaw;

        SimpleDebugPlayer view;

        private readonly GameObject prefab;

        private const float SPEED = 6f;

        public BasePlayer(EntityParams entityParams, GameObject prefab) : base(entityParams) {
            this.prefab = prefab;
        }

        protected override void OnConstructed()
        {
            if (!IsClient) return;
            GameObject instance = GameObject.Instantiate(prefab);
            view = instance.GetComponent<SimpleDebugPlayer>();
        }

        protected override void OnDestroy()
        {
            GameObject.Destroy(view.gameObject);
            view = null;
        }

        protected override void Update()
        {
            base.Update();

            if (Controller is not BasePlayerController pc) return;
            
            view.transform.
        }
    }
}