﻿using System.Collections.Generic;
using LDtkUnity.Runtime.Data.Definition;
using LDtkUnity.Runtime.Data.Level;
using LDtkUnity.Runtime.Providers;
using LDtkUnity.Runtime.Tools;
using LDtkUnity.Runtime.UnityAssets.Tileset;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LDtkUnity.Runtime.Builders
{
    public static class LDtkBuilderTileset
    {
        public static void BuildTileset(LDtkDataLayer layer, LDtkDataTile[] tiles, LDtkTilesetAssetCollection assets, Tilemap[] tilemaps)
        {
            LDtkDefinitionTileset definition = layer.IsAutoTilesLayer
                ? layer.Definition.AutoTilesetDefinition
                : layer.Definition.TileLayerDefinition;
            
            LDtkTilesetAsset asset = assets.GetAssetByIdentifier(definition.identifier);
            if (asset == null) return;
            
            //figure out if we have already built a tile in this position. otherwise, build up to the next tilemap
            Dictionary<Vector2Int, int> builtTileLayering = new Dictionary<Vector2Int, int>();
            
            foreach (LDtkDataTile tileData in tiles)
            {
                Vector2Int px = tileData.px.ToVector2Int();
                int tilemapLayer = GetTilemapLayerToBuildOn(builtTileLayering, px, tilemaps.Length-1);

                
                BuildTile(layer, tileData, asset, tilemaps[tilemapLayer]);
            }
        }

        private static void BuildTile(LDtkDataLayer layer, LDtkDataTile tileData, LDtkTilesetAsset asset, Tilemap tilemap)
        {
            Vector2Int coord = tileData.LayerPixelPosition / layer.__gridSize;
            coord = LDtkToolOriginCoordConverter.ConvertCell(coord, layer.__cHei);

            Sprite tileSprite = GetTileFromTileset(asset.ReferencedAsset, tileData.SourcePixelPosition, layer.__gridSize);

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.colliderType = Tile.ColliderType.None;
            tile.sprite = tileSprite;

            Vector3Int co = new Vector3Int(coord.x, coord.y, 0);


            
            //Tilemap mapToBuildOn = tilemap;
            
            tilemap.SetTile(co, tile);
            SetTileFlips(tilemap, tileData, coord); 
        }

        private static int GetTilemapLayerToBuildOn(Dictionary<Vector2Int, int> builtTileLayering, Vector2Int key, int startingNumber)
        {
            if (builtTileLayering.ContainsKey(key))
            {
                return --builtTileLayering[key];
            }
            
            builtTileLayering.Add(key, startingNumber);
            return startingNumber;
        }

        private static Sprite GetTileFromTileset(Sprite tileset, Vector2Int sourceCathodeRayPos, int pixelsPerUnit)
        {
            Debug.Assert(pixelsPerUnit != 0);
            
            sourceCathodeRayPos = LDtkToolOriginCoordConverter.ConvertPixel(sourceCathodeRayPos, tileset.texture.height, pixelsPerUnit);
            
            Vector2Int tileSize = Vector2Int.one * pixelsPerUnit;
            Rect rect = new Rect(sourceCathodeRayPos, tileSize);
            
            return LDtkProviderTilesetSprite.GetSpriteFromTilesetAndRect(tileset, rect, pixelsPerUnit);
        }

        private static void SetTileFlips(Tilemap tilemap, LDtkDataTile tileData, Vector2Int coord)
        {
            float rotY = tileData.FlipX ? 180 : 0;
            float rotX = tileData.FlipY ? 180 : 0;
            Quaternion rot = Quaternion.Euler(rotX, rotY, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
            tilemap.SetTransformMatrix((Vector3Int) coord, matrix);
        }
    }
}
