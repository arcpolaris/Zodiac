using LiteEntitySystem;
using System.Numerics;

namespace Zodiac.Net
{
    public class BasePlayerController : HumanControllerLogic<BasePlayerController.Input>
    {
        public struct Input {
            public Vector3 move;
        }
        public BasePlayerController(EntityParams entityParams) : base(entityParams) {

        }
    }
}