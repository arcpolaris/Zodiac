using LiteEntitySystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zodiac.Util;

namespace Zodiac.Net
{
    [Serializable]
    public enum GameEntities
    {
        Player,
        PlayerController,
        Enemy
    }

    [CreateAssetMenu(fileName = "EntityAtlas", menuName = "Zodiac/Entity Atlas")]
    public class GameTypes : ScriptableObject
    {
        public EntityTypesMap<GameEntities> Build() => new EntityTypesMap<GameEntities>()
            .Register(GameEntities.Player, e => new BasePlayer(e, prefabs[GameEntities.Player]))
            .Register(GameEntities.PlayerController, static e => new BasePlayerController(e));

        [SerializeField] private Dictionary<GameEntities, GameObject> prefabs = new();
    }
}