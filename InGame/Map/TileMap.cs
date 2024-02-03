using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

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

        private bool ttlInitialized = false;
        private double[,] _zoneTtl;
        private const double ttl = 1000;

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

        private void initTtlMap()
        {
            if (ArrayTileMap == null)
                return;
            
            var mapSizeX = ArrayTileMap.GetLength(0);
            var mapSizeY = ArrayTileMap.GetLength(1);

            _zoneTtl = new double[mapSizeX, mapSizeY];
            
            for (int x = 0; x < mapSizeX; x++)
            for (int y = 0; y < mapSizeY; y++)
                if (Game1.GameManager.IsTileInExploredZone(x + 1, y + 1))
                {
                    _zoneTtl[x,y] = 0;
                }
                else
                {
                    _zoneTtl[x,y] = ttl;
                }

            ttlInitialized = true;
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

        private void DrawTileUnexploredLayer(SpriteBatch spriteBatch, Texture2D tileset, int layer, int padding = 0)
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
                    if (Game1.GameManager.MapManager.CurrentMap.DungeonMode)
                    {
                        DungeonFog(x, y);
                    } else if (Game1.GameManager.MapManager.CurrentMap.IsOverworld)
                    {
                        OverworldFog(x, y);
                    }
                    
                }
            }

            void OverworldFog(int x, int y)
            {
                if (x % (Values.FieldWidth / TileSize) != 0 || y % (Values.FieldHeight / TileSize) != 0)
                {
                    return;
                }

                float opacity = 1.0f;
                if (Game1.GameManager.IsTileInExploredZone(x + 1, y + 1))
                {
                    if (!ttlInitialized)
                    {
                        initTtlMap();
                        if (!ttlInitialized)
                        {
                            return;
                        }
                    }
                    opacity = (float) (_zoneTtl[x, y] / ttl);
                    _zoneTtl[x, y] -= Game1.DeltaTime;
                    if (opacity < 0.001)
                    {
                        return;
                    }
                }

                var position = new Vector2(x * (tileSize + 1), y * (tileSize + 1));
                var offset0 = new Vector2(MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 2000)) * 8, MathF.Cos((float)((Game1.TotalGameTime + _timeOffset) / 6000)) * 8);
                var offset1 = new Vector2(MathF.Cos((float)((Game1.TotalGameTime + _timeOffset) / 3250)) * 8, MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 7500)) * 8);
                var color = Color.White * opacity;
                spriteBatch.Draw(SprFogWar, position + offset0, null, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                spriteBatch.Draw(SprFogWar, position + offset1, null, color, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
            }

            void DungeonFog(int x, int y)
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
                        new Color(32, 32, 32, 128));
                }
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

            for (var i = 0; i < ArrayTileMap.GetLength(2) - (BlurLayer ? 1 : 0); i++)
            {
                DrawTileUnexploredLayer(spriteBatch, SprTileset, i, 128);
            }

            spriteBatch.End();
        }
    }
}
