using UnityEngine;

namespace Zodiac
{
    public class SimpleDebugPlayer : MonoBehaviour
    {
        void Start()
        {
            print("simple debug player created");
        }

        void OnDestroy()
        {
            print("simple debug player destroyed");
        }
    }
}
