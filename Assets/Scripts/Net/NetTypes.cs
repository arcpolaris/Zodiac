using LiteEntitySystem;
using UnityEngine;

namespace Zodiac.Net
{
    public static class NetTypes
    {
        private static bool _done;

        public static void EnsureRegistered(){
            if (_done) return;
            _done = true;

            EntityManager.RegisterFieldType<Vector2>(Vector2.Lerp);
            EntityManager.RegisterFieldType<Vector3>(Vector3.Lerp);
            EntityManager.RegisterFieldType<Vector4>(Vector4.Lerp);
            EntityManager.RegisterFieldType<Quaternion>(Quaternion.Slerp);
            EntityManager.RegisterFieldType<Color>(Color.Lerp);

            // LiteEntitySystem.Extensions.FloatAngle
        }
    }
}
