﻿using System.Linq;
using LDtkUnity.Runtime.Data;
using LDtkUnity.Runtime.Data.Level;
using LDtkUnity.Runtime.Tools;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace LDtkUnity.Tests.Editor
{
    public class TileTest
    {
        private const string MOCK_TILE = "LDtkMockTiles.json";
        
        [Test]
        public void GetCorrectTileBits()
        {
            TextAsset mockTest = TestJsonLoader.LoadJson(MOCK_TILE);
            LDtkDataTile[] tiles = JsonConvert.DeserializeObject<LDtkDataTile[]>(mockTest.text);

            foreach (LDtkDataTile tile in tiles)
            {
                Debug.Log($"Tile: {tile.FlipX}, {tile.FlipY}");
            }
            
            Assert.IsTrue(tiles[0].FlipX == false && tiles[0].FlipY == false);
            Assert.IsTrue(tiles[1].FlipX == true && tiles[1].FlipY == false);
            Assert.IsTrue(tiles[2].FlipX == false && tiles[2].FlipY == true);
            Assert.IsTrue(tiles[3].FlipX == true && tiles[3].FlipY == true);
        }
    }
}