﻿using System;
using System.Diagnostics;
using System.Linq;
using LDtkUnity.Runtime.Data;
using LDtkUnity.Runtime.Data.Level;
using LDtkUnity.Runtime.Providers;
using LDtkUnity.Runtime.Tools;
using LDtkUnity.Runtime.UnityAssets;
using LDtkUnity.Runtime.UnityAssets.Colliders;
using LDtkUnity.Runtime.UnityAssets.Entity;
using LDtkUnity.Runtime.UnityAssets.Settings;
using LDtkUnity.Runtime.UnityAssets.Tileset;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LDtkUnity.Runtime.Builders
{
    public static class LDtkLevelBuilder
    {
        public static event Action<LDtkDataLevel> OnLevelBuilt;

        private static int _layerSortingOrder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnLevelBuilt = null;
            _layerSortingOrder = 0;
        }

        public static void BuildLevel(LDtkDataProject project, LDtkLevelIdentifier levelToBuild, LDtkProjectAssets assets)
        {
            if (assets == null)
            {
                Debug.LogError("LDtk: ProjectAssets object is null; not building level.");
                return;
            }
            if (levelToBuild == null)
            {
                Debug.LogError("LDtk: LDtkLevelIdentifier object is null; not building level.");
                return;
            }

            if (!LDtkUnityTilesetBuilder.ValidateTilemapPrefabRequirements(assets.TilemapPrefab))
            {
                return;
            }
            
            
            bool success = GetProjectLevelByID(project.levels, levelToBuild, out LDtkDataLevel level);
            if (!success) return;
            
            string debugLvlName = $"\"{level.identifier}\"";
            //Debug.Log($"LDtk: Building level: {debugLvlName}");
            Stopwatch levelBuildTimer = Stopwatch.StartNew();

            BuildProcess(project, level, assets);
            
            levelBuildTimer.Stop();
            double ms = levelBuildTimer.ElapsedMilliseconds;
            Debug.Log($"LDtk: Built level {debugLvlName} in {ms}ms ({ms/1000}s)");
            
            OnLevelBuilt?.Invoke(level);
        }
        
        private static bool GetProjectLevelByID(LDtkDataLevel[] levels, LDtkLevelIdentifier levelToBuild, out LDtkDataLevel level)
        {
            level = default;

            if (levelToBuild == null)
            {
                Debug.LogError($"LDtk: LevelToBuild null, not assigned?");
                return false;
            }

            bool IdentifierMatchesLevelToBuild(LDtkDataLevel lvl) => string.Equals(lvl.identifier, levelToBuild);
            
            if (!levels.Any(IdentifierMatchesLevelToBuild))
            {
                Debug.LogError($"LDtk: No level named \"{levelToBuild}\" exists in the LDtk Project");
                return false;
            }

            level = levels.First(IdentifierMatchesLevelToBuild);
            return true;
        }

        private static void BuildProcess(LDtkDataProject project, LDtkDataLevel level, LDtkProjectAssets assets)
        {
            InitStaticTools(project);
            BuildLayerInstances(level, assets);
            DisposeStaticTools();
        }

        private static void InitStaticTools(LDtkDataProject project)
        {
            LDtkProviderTile.Init();     
            LDtkProviderTilesetSprite.Init();
            LDtkProviderEnum.Init();
            LDtkProviderUid.CacheUidData(project);
            LDtkProviderErrorIdentifiers.Init();
        }
        private static void DisposeStaticTools()
        {
            LDtkProviderTile.Dispose();
            LDtkProviderTilesetSprite.Dispose();
            LDtkProviderEnum.Dispose();
            LDtkProviderUid.Dispose(); 
            LDtkProviderErrorIdentifiers.Dispose();
        }

        private static void BuildLayerInstances(LDtkDataLevel level, LDtkProjectAssets assets)
        {
            _layerSortingOrder = 0;
            
            foreach (LDtkDataLayer layer in level.layerInstances)
            {
                BuildLayerInstance(layer, assets);
            }
        }

        private static void BuildLayerInstance(LDtkDataLayer layer, LDtkProjectAssets assets)
        {
            if (layer.IsIntGridLayer) BuildIntGridLayer(layer, assets.CollisionValues, assets.TilemapPrefab);
            if (layer.IsAutoTilesLayer) BuildTilesetLayer(layer, layer.autoLayerTiles, assets.Tilesets, assets.TilemapPrefab);
            if (layer.IsGridTilesLayer) BuildTilesetLayer(layer, layer.gridTiles, assets.Tilesets, assets.TilemapPrefab);
            if (layer.IsEntityInstancesLayer) BuildEntityInstanceLayer(layer, assets.EntityInstances);
        }
        
        private static void BuildIntGridLayer(LDtkDataLayer layer, LDtkIntGridValueAssetCollection intGridValueAssets,
            Grid tilemapPrefab)
        {
            if (IsAssetNull(intGridValueAssets)) return;
            if (IsAssetNull(tilemapPrefab)) return;

            DecrementLayer();
            
            Tilemap tilemap = LDtkUnityTilesetBuilder.BuildUnityTileset(layer.__identifier, tilemapPrefab, _layerSortingOrder);
            if (tilemap == null) return;
            
            LDtkBuilderIntGridValue.BuildIntGridValues(layer, intGridValueAssets, tilemap);
            
            LDtkUnityTilesetBuilder.SetTilesetOpacity(tilemap, layer.__opacity);
        }

        private static void BuildTilesetLayer(LDtkDataLayer layer, LDtkDataTile[] tiles,
            LDtkTilesetAssetCollection tilesetAssets, Grid tilemapPrefab)
        {
            if (IsAssetNull(tilesetAssets)) return;
            if (IsAssetNull(tilemapPrefab)) return;

            
            
            var grouped = tiles.Select(p => p.px.ToVector2Int()).ToLookup(x => x);
            int maxRepetitions = grouped.Max(x => x.Count());

            
            string gameObjectName = layer.__identifier;

            if (layer.IsIntGridLayer)
            {
                gameObjectName += "_AutoLayer";
            }

            //this is for the potential of having multiple rules apply to the same tile. make multiple Unity Tilemaps.
            Tilemap[] tilemaps = new Tilemap[maxRepetitions];
            for (int i = 0; i < maxRepetitions; i++)
            {
                DecrementLayer();
                
                string name = gameObjectName;
                name += $"_{i}";
                Tilemap tilemap = LDtkUnityTilesetBuilder.BuildUnityTileset(name, tilemapPrefab, _layerSortingOrder);
                if (tilemap == null) return;
                
                tilemaps[i] = tilemap;
                
            }
            
            LDtkBuilderTileset.BuildTileset(layer, tiles, tilesetAssets, tilemaps);

            //set each layer's alpha
            foreach (Tilemap tilemap in tilemaps)
            {
                LDtkUnityTilesetBuilder.SetTilesetOpacity(tilemap, layer.__opacity);
            }
        }

        private static void BuildEntityInstanceLayer(LDtkDataLayer layer, LDtkEntityAssetCollection entityAssets)
        {
            if (IsAssetNull(entityAssets)) return;

            DecrementLayer();
            LDtkBuilderEntityInstance.BuildEntityLayerInstances(layer, entityAssets, _layerSortingOrder);
        }


        
        private static bool IsAssetNull<T>(T assets) where T : Object
        {
            if (assets != null) return false;
            
            Debug.LogError($"LDtk: {typeof(T).Name} is null; not building layer. Check the {nameof(LDtkProjectAssets)} that was used to build the level.");
            return true;
        }
        
        public static void DecrementLayer()
        {
            _layerSortingOrder--;
            //Debug.Log(_layerSortingOrder);
        }


    }
}
