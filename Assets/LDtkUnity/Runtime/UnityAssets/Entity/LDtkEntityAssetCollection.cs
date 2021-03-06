﻿using LDtkUnity.Runtime.Tools;
using UnityEngine;

namespace LDtkUnity.Runtime.UnityAssets.Entity
{
    [CreateAssetMenu(fileName = nameof(LDtkEntityAssetCollection),
        menuName = LDtkToolScriptableObj.SO_PATH + nameof(LDtkEntityAssetCollection),
        order = LDtkToolScriptableObj.SO_ORDER)]
    public class LDtkEntityAssetCollection : LDtkAssetCollection<LDtkEntityAsset>
    {
    }
}