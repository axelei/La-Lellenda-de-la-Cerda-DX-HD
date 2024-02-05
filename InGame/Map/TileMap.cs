using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;

namespace ProjectZ.InGame.Map
{
    public class TileMap
    {
        public Texture2D SprTileset;
        public Texture2D SprTilesetBlur;
        public Texture2D SprFogWar;

        private int _timeOffset;

        public string TilesetPath;
        public int[,,] ArrayTileMap;

        public int TileSize;
        public int TileCountX;
        public int TileCountY;

        public bool BlurLayer = false;

        public void SetTileset(Texture2D sprTileset, int tileSize = 16)
        {
            SprTileset = sprTileset;
            SprTilesetBlur = Resources.SprBlurTileset;
            SprFogWar = Resources.SprFogWar;
            
            _timeOffset = Game1.RandomNumber.Next(0, 5000);

            TileSize = tileSize;

            // calculate how many tiles are horizontally and vertically
            TileCountX = SprTileset.Width / TileSize;
            TileCountY = SprTileset.Height / TileSize;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (ArrayTileMap == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, MapManager.Camera.Scale >= 1 ?
                SamplerState.PointWrap : SamplerState.AnisotropicWrap, null, null, null, MapManager.Camera.TransformMatrix);

            for (var i = 0; i < ArrayTileMap.GetLength(2) - (BlurLayer ? 1 : 0); i++)
                DrawTileLayer(spriteBatch, SprTileset, i);

            spriteBatch.End();
        }

        // TODO_End: this could be optimized like in MonoGame.Extended
        public void DrawTileLayer(SpriteBatch spriteBatch, Texture2D tileset, int layer, int padding = 0)
        {
            var halfWidth = Game1.RenderWidth / 2;
            var halfHeight = Game1.RenderHeight / 2;

            var tileSize = Values.TileSize;

            var camera = MapManager.Camera;
            var startX = Math.Max(0, (int)((camera.X - halfWidth) / (camera.Scale * tileSize)) - padding);
            var startY = Math.Max(0, (int)((camera.Y - halfHeight) / (camera.Scale * tileSize)) - padding);
            var endX = Math.Min(ArrayTileMap.GetLength(0), (int)((camera.X + halfWidth) / (camera.Scale * tileSize)) + 1 + padding);
            var endY = Math.Min(ArrayTileMap.GetLength(1), (int)((camera.Y + halfHeight) / (camera.Scale * tileSize)) + 1 + padding);

            for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                {
                    if (ArrayTileMap[x, y, layer] >= 0)
                        spriteBatch.Draw(tileset,
                            new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                            new Rectangle((ArrayTileMap[x, y, layer] % (tileset.Width / TileSize)) * TileSize,
                                ArrayTileMap[x, y, layer] / (tileset.Width / TileSize) * TileSize, TileSize, TileSize),
                            Color.White);
                }
        }

        // this should probably be at a different location
        public void DrawBlurLayer(SpriteBatch spriteBatch)
        {
            if (ArrayTileMap == null)
                return;

            //SprTilesetBlur.Dispose();
            //Resources.LoadTexture(out SprTilesetBlur, "D:\\Dev\\ProjectZ\\ProjectZ\\bin\\Data\\Maps\\Tilesets\\blur tileset.png");

            DrawTileLayer(spriteBatch, SprTilesetBlur, ArrayTileMap.GetLength(2) - 1, 1);
        }

        
        private void DrawUnexploredCoverDungeon(SpriteBatch spriteBatch, Texture2D tileset, int layer, int padding = 0)
        {
            var halfWidth = Game1.RenderWidth / 2;
            var halfHeight = Game1.RenderHeight / 2;

            var tileSize = Values.TileSize;

            var camera = MapManager.Camera;
            var startX = Math.Max(0, (int)((camera.X - halfWidth) / (camera.Scale * tileSize)) - padding);
            var startY = Math.Max(0, (int)((camera.Y - halfHeight) / (camera.Scale * tileSize)) - padding);
            var endX = Math.Min(ArrayTileMap.GetLength(0), (int)((camera.X + halfWidth) / (camera.Scale * tileSize)) + 1 + padding);
            var endY = Math.Min(ArrayTileMap.GetLength(1), (int)((camera.Y + halfHeight) / (camera.Scale * tileSize)) + 1 + padding);

            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var destinationRectangle = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    var sourceRectangle = new Rectangle(
                        (ArrayTileMap[x, y, layer] % (tileset.Width / TileSize)) * TileSize,
                        ArrayTileMap[x, y, layer] / (tileset.Width / TileSize) * TileSize, TileSize, TileSize);
                    if (!Game1.GameManager.IsTileInExploredZone(x, y))
                    {
                        spriteBatch.Draw(tileset,
                            destinationRectangle,
                            sourceRectangle,
                            Color.Black);
                    }
                    else if (!Game1.GameManager.IsTileInCurrentPlayerZone(x, y) && ArrayTileMap[x, y, layer] >= 0)
                    {
                        spriteBatch.Draw(tileset,
                            destinationRectangle,
                            sourceRectangle,
                            Color.Black * 0.33f);
                    }
                }
            }
        }

        private void DrawUnexploredCoverOverworld(SpriteBatch spriteBatch, int zoneX, int zoneY)
        {
            bool isVisible = Game1.GameManager.MapVisibility[zoneX, zoneY];
            float opacity = 1.0f;
            if (isVisible)
            {
                opacity = Game1.GameManager.MapVisibilityOverworldTimer[zoneX, zoneY] / GameManager.TileTtl;
                Game1.GameManager.MapVisibilityOverworldTimer[zoneX, zoneY] -= Game1.DeltaTime;

                if (opacity < 0.001)
                {
                    return;
                }
            }
            
            var position = new Vector2(zoneX * Values.FieldWidth, zoneY * Values.FieldHeight - TileSize);
            for (int i = -2; i < 2; ++i)
            {
                var offset0 = new Vector2(MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 1000)) * i * 8, MathF.Cos((float)((Game1.TotalGameTime + _timeOffset) / 3000)) * i * 8);
                var offset1 = new Vector2(MathF.Cos((float)((Game1.TotalGameTime + _timeOffset) / 1750)) * i * 8, MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 4100)) * i * 8);
                var color = Color.White * opacity;
                spriteBatch.Draw(SprFogWar, position + offset0, null, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                spriteBatch.Draw(SprFogWar, position + offset1, null, color, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
            }
        }

        public void DrawUnexploredCover(SpriteBatch spriteBatch)
        {
            if (ArrayTileMap == null)
            {
                return;
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, null, MapManager.Camera.Scale >= 1 ?
                SamplerState.PointWrap : SamplerState.AnisotropicWrap, null, null, null, MapManager.Camera.TransformMatrix);

            if (Game1.GameManager.MapManager.CurrentMap.IsOverworld)
            {
                for (var zoneX = 0; zoneX < 16; ++zoneX)
                {
                    for (var zoneY = 0; zoneY < 16; ++zoneY)
                    { 
                        DrawUnexploredCoverOverworld(spriteBatch, zoneX, zoneY);
                    }
                }
            }
            else if (Game1.GameManager.MapManager.CurrentMap.DungeonMode)
            {
                DrawUnexploredCoverDungeon(spriteBatch, SprTileset, 0);
            }
            
            spriteBatch.End();
        }
    }
}
