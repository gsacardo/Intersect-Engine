﻿/*
    Intersect Game Engine (Server)
    Copyright (C) 2015  JC Snider, Joe Bridges
    
    Website: http://ascensiongamedev.com
    Contact Email: admin@ascensiongamedev.com 

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
using Intersect_Editor.Forms;
using Intersect_Editor.Properties;
using SFML.Graphics;
using SFML.Window;
using Color = SFML.Graphics.Color;
using Image = SFML.Graphics.Image;

namespace Intersect_Editor.Classes
{
    public static class Graphics
    {
        //Main render area - map window
        private static RenderWindow _renderwindow;
        //Tileset render widow
        private static RenderWindow _tilesetwindow;

        //Game Graphics
        static bool _tilesetsLoaded;
        static Texture[] _tilesetTex;
        public static string[] ItemNames;
        public static Texture[] ItemTextures;
        public static string[] EntityFileNames;
        public static Texture[] EntityTextures;
        public static string[] SpellFileNames;
        public static Texture[] SpellTextures;
        public static string[] AnimationFileNames;
        public static Texture[] AnimationTextures;
        public static List<string> ImageFileNames;
        public static Texture[] ImageTextures;
        public static List<string> FogFileNames;
        public static Texture[] FogTextures;

        //Face Textures
        public static List<string> FaceFileNames;
        public static Texture[] FaceTextures;

        //Basic Editor Textures
        private static Texture _transH;
        private static Texture _transV;
        //Texture for attributes
        private static Texture _attributesTex;
        //Texture for events
        private static Texture _eventTex;
        //Single tile texture for light placement
        private static Texture _lightTex;
        //Texture for NPC Spawns
        private static Texture _spawnTex;

        //Light Stuff
        public static byte CurrentBrightness = 100;
        public static bool HideDarkness = false;
        public static bool LightsChanged = true;
        public static RenderTexture DarkCacheTexture;

        //Overlay Stuff
        public static Color OverlayColor = Color.Transparent;
        public static bool HideOverlay = false;
        public static RenderTexture OverlayTexture = new RenderTexture(Constants.MapWidth * 32 * 3, Constants.MapHeight * 32 * 3);

        //Fog Stuff
        public static bool HideFog = false;
        private static long _fogUpdateTime = Environment.TickCount;
        private static float _fogCurrentX = 0;
        private static float _fogCurrentY = 0;

        //Setup and Loading
        public static void InitSfml(FrmMain myForm)
        {
            try
            {
                Stream s = new MemoryStream();

                _renderwindow = new RenderWindow(myForm.picMap.Handle); // creates our SFML RenderWindow on our surface control
                _tilesetwindow = new RenderWindow(myForm.picTileset.Handle);
                Resources.transV.Save(s, ImageFormat.Png);
                _transV = new Texture(s);
                s.Dispose();
                s = new MemoryStream();
                Resources.transH.Save(s, ImageFormat.Png);
                _transH = new Texture(s);
                s.Dispose();
                s = new MemoryStream();
                Resources.attributes.Save(s, ImageFormat.Png);
                _attributesTex = new Texture(s);
                s.Dispose();
                s = new MemoryStream();
                Resources.jcl.Save(s, ImageFormat.Png);
                _lightTex = new Texture(s);
                s.Dispose();
                s = new MemoryStream();
                Resources.MapSpawn.Save(s, ImageFormat.Png);
                _spawnTex = new Texture(s);
                s.Dispose();
                s = new MemoryStream();
                Resources.jce.Save(s, ImageFormat.Png);
                _eventTex = new Texture(s);
                s.Dispose();
                LoadGraphics(myForm);
                Audio.LoadAudio();

            }
            catch (Exception)
            {
                // ignored
            }
        }
        private static void LoadGraphics(FrmMain myForm)
        {
            LoadTilesets(myForm);
            LoadItems();
            LoadEntities();
            LoadSpells();
            LoadAnimations();
            LoadFaces();
            LoadImages();
            LoadFogs();
        }
        private static void LoadTilesets(FrmMain myForm)
        {
            if (!Directory.Exists("Resources/Tilesets")) { Directory.CreateDirectory("Resources/Tilesets"); }
            var tilesets = Directory.GetFiles("Resources/Tilesets", "*.png");
            var tilesetsUpdated = false;
            if (tilesets.Length > 0)
            {
                for (var i = 0; i < tilesets.Length; i++)
                {
                    tilesets[i] = tilesets[i].Replace("Resources/Tilesets\\", "");
                    if (Globals.Tilesets != null)
                    {
                        if (Globals.Tilesets.Length > 0)
                        {
                            for (var x = 0; x < Globals.Tilesets.Length; x++)
                            {
                                if (Globals.Tilesets[x] == tilesets[i])
                                {
                                    break;
                                }
                                if (x != Globals.Tilesets.Length - 1) continue;
                                var newTilesets = new string[Globals.Tilesets.Length + 1];
                                Globals.Tilesets.CopyTo(newTilesets, 0);
                                newTilesets[Globals.Tilesets.Length] = tilesets[i];
                                Globals.Tilesets = newTilesets;
                                tilesetsUpdated = true;
                            }
                        }
                        else
                        {
                            var newTilesets = new string[1];
                            newTilesets[0] = tilesets[i];
                            Globals.Tilesets = newTilesets;
                            tilesetsUpdated = true;
                        }
                    }
                    else
                    {
                        var newTilesets = new string[1];
                        newTilesets[0] = tilesets[i];
                        Globals.Tilesets = newTilesets;
                        tilesetsUpdated = true;
                    }
                }

                if (tilesetsUpdated)
                {
                    PacketSender.SendTilesets();
                }

                myForm.cmbTilesets.Items.Clear();
                foreach (var t in Globals.Tilesets)
                {
                    if (File.Exists("Resources/Tilesets/" + t))
                    {
                        myForm.cmbTilesets.Items.Add(t);
                    }
                    else
                    {
                        myForm.cmbTilesets.Items.Add(t + " - [MISSING]");
                    }
                }
                myForm.cmbTilesets.SelectedIndex = 0;
                Globals.CurrentTileset = 0;

                _tilesetTex = new Texture[Globals.Tilesets.Length];
                for (var i = 0; i < Globals.Tilesets.Length; i++)
                {
                    if (File.Exists("Resources/Tilesets/" + Globals.Tilesets[i]))
                    {
                        _tilesetTex[i] = new Texture(new Image("Resources/Tilesets/" + Globals.Tilesets[i]));
                    }
                }
                _tilesetsLoaded = true;
                InitTileset(0, myForm);
            }
        }
        private static void LoadItems()
        {
            if (!Directory.Exists("Resources/Items")) { Directory.CreateDirectory("Resources/Items"); }
            var items = Directory.GetFiles("Resources/Items", "*.png");
            ItemNames = new string[items.Length];
            ItemTextures = new Texture[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                ItemNames[i] = items[i].Replace("Resources/Items\\","");
                ItemTextures[i] = new Texture(new Image("Resources/Items/" + ItemNames[i]));
            }
        }
        private static void LoadEntities()
        {
            if (!Directory.Exists("Resources/Entities")) { Directory.CreateDirectory("Resources/Entities"); }
            var chars = Directory.GetFiles("Resources/Entities", "*.png");
            EntityFileNames = new string[chars.Length];
            EntityTextures = new Texture[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                EntityFileNames[i] = chars[i].Replace("Resources/Entities\\", "");
                EntityTextures[i] = new Texture(new Image("Resources/Entities/" + EntityFileNames[i]));
            }
        }
        private static void LoadSpells()
        {
            if (!Directory.Exists("Resources/Spells")) { Directory.CreateDirectory("Resources/Spells"); }
            var spells = Directory.GetFiles("Resources/Spells", "*.png");
            SpellFileNames = new string[spells.Length];
            SpellTextures = new Texture[spells.Length];
            for (int i = 0; i < spells.Length; i++)
            {
                SpellFileNames[i] = spells[i].Replace("Resources/Spells\\", "");
                SpellTextures[i] = new Texture(new Image("Resources/Spells/" + SpellFileNames[i]));
            }
        }
        private static void LoadAnimations()
        {
            if (!Directory.Exists("Resources/Animations")) { Directory.CreateDirectory("Resources/Animations"); }
            var animations = Directory.GetFiles("Resources/Animations", "*.png");
            AnimationFileNames = new string[animations.Length];
            AnimationTextures = new Texture[animations.Length];
            for (int i = 0; i < animations.Length; i++)
            {
                AnimationFileNames[i] = animations[i].Replace("Resources/Animations\\", "");
                AnimationTextures[i] = new Texture(new Image("Resources/Animations/" + AnimationFileNames[i]));
            }
        }
        private static void LoadFaces()
        {
            if (!Directory.Exists("Resources/Faces")) { Directory.CreateDirectory("Resources/Faces"); }
            var faces = Directory.GetFiles("Resources/Faces", "*.png");
            FaceFileNames = new List<string>();
            FaceTextures = new Texture[faces.Length];
            for (int i = 0; i < faces.Length; i++)
            {
                FaceFileNames.Add(faces[i].Replace("Resources/Faces\\", ""));
                FaceTextures[i] = new Texture(new Image("Resources/Faces/" + FaceFileNames[i]));
            }
        }
        private static void LoadImages()
        {
            if (!Directory.Exists("Resources/Images")) { Directory.CreateDirectory("Resources/Images"); }
            var images = Directory.GetFiles("Resources/Images", "*.png");
            ImageFileNames = new List<string>();
            ImageTextures = new Texture[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                ImageFileNames.Add(images[i].Replace("Resources/Images\\", ""));
                ImageTextures[i] = new Texture(new Image("Resources/Images/" + ImageFileNames[i]));
            }
        }
        private static void LoadFogs()
        {
            if (!Directory.Exists("Resources/Fogs")) { Directory.CreateDirectory("Resources/Fogs"); }
            var fogs = Directory.GetFiles("Resources/Fogs", "*.png");
            FogFileNames = new List<string>();
            FogTextures = new Texture[fogs.Length];
            for (int i = 0; i < fogs.Length; i++)
            {
                FogFileNames.Add(fogs[i].Replace("Resources/Fogs\\", ""));
                FogTextures[i] = new Texture(new Image("Resources/Fogs/" + FogFileNames[i]));
            }
        }

        //Rendering
        public static void Render()
        {
            _renderwindow.DispatchEvents(); // handle SFML events - NOTE this is still required when SFML is hosted in another window
            _renderwindow.Clear(Color.Black); // clear our SFML RenderWindow
            _tilesetwindow.DispatchEvents();
            _tilesetwindow.Clear(Color.Black);
            DrawTileset();

            //Draw Current Map
            DrawTransparentBorders();
            DrawMap();
            DrawMapUp();
            DrawMapDown();
            DrawMapLeft();
            DrawMapRight();
            DrawMapBorders();
            if (!HideFog) { DrawFog(); }
            if (!HideOverlay) { DrawMapOverlay(); }
            if (!HideDarkness || Globals.CurrentLayer == Constants.LayerCount + 1) { DrawDarkness(); }
            _renderwindow.Display(); // display what SFML has drawn to the screen
            _tilesetwindow.Display();
        }
        private static void DrawTransparentBorders()
        {
            var tmpSprite = new Sprite(_transH);
            _renderwindow.Draw(tmpSprite);
            tmpSprite.Position = new Vector2f(0, 1024 - 32);
            _renderwindow.Draw(tmpSprite);
            tmpSprite = new Sprite(_transV) {Position = new Vector2f(0, 0)};
            _renderwindow.Draw(tmpSprite);
            tmpSprite.Position = new Vector2f(1024 - 32, 0);
            _renderwindow.Draw(tmpSprite);
        }
        private static void DrawMap()
        {
            Sprite tmpSprite;
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (tmpMap == null) { return; }
            for (var x = 0; x < Constants.MapWidth; x++)
            {
                for (var y = 0; y < Constants.MapHeight; y++)
                {
                    for (var z = 0; z < Constants.LayerCount; z++)
                    {
                        if (tmpMap.Layers[z].Tiles[x, y].TilesetIndex <= -1) continue;
                        if (_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex] == null) continue;
                        switch (tmpMap.Autotiles.Autotile[x, y].Layer[z].RenderState)
                        {
                            case Constants.RenderStateNormal:
                                tmpSprite = new Sprite(_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex])
                                {
                                    TextureRect =
                                        new IntRect(tmpMap.Layers[z].Tiles[x, y].X*32, tmpMap.Layers[z].Tiles[x, y].Y*32,
                                            32, 32),
                                    Position = new Vector2f(x*32 + 32, y*32 + 32)
                                };
                                _renderwindow.Draw(tmpSprite);
                                break;
                            case Constants.RenderStateAutotile:
                                DrawAutoTile(z, x * 32 + 32, y * 32 + 32, 1, x, y, tmpMap);
                                DrawAutoTile(z, x * 32 + 16 + 32, y * 32 + 32, 2, x, y, tmpMap);
                                DrawAutoTile(z, x * 32 + 32, y * 32 + 16 + 32, 3, x, y, tmpMap);
                                DrawAutoTile(z, x * 32 + 16 + 32, y * 32 + 16 + 32, 4, x, y, tmpMap);
                                break;
                        }
                    }
                }

            }
            RectangleShape tileSelectionRect;

            // Draw Npc Spawns from Map Editor
            if (Globals.ViewingMapProperties == true)
            {
                for (int i = 0; i < Globals.GameMaps[Globals.CurrentMap].Spawns.Count; i++)
                {
                    if (Globals.GameMaps[Globals.CurrentMap].Spawns[i].X >= 0 && Globals.GameMaps[Globals.CurrentMap].Spawns[i].Y >= 0)
                    {
                        tmpSprite = new Sprite(_spawnTex);
                        tmpSprite.TextureRect = new IntRect(0, 0, 32, 32);
                        tmpSprite.Position = new Vector2f(Globals.GameMaps[Globals.CurrentMap].Spawns[i].X * 32 + 32, Globals.GameMaps[Globals.CurrentMap].Spawns[i].Y * 32 + 32);
                        _renderwindow.Draw(tmpSprite);
                    }
                }
            }

            switch (Globals.CurrentLayer)
            {
                case Constants.LayerCount:
                    //Draw attributes
                    for (int x = 0; x < Constants.MapWidth; x++)
                    {
                        for (int y = 0; y < Constants.MapHeight; y++)
                        {
                            if (tmpMap.Attributes[x, y].value > 0)
                            {
                                tmpSprite = new Sprite(_attributesTex);
                                tmpSprite.TextureRect = new IntRect(0, (tmpMap.Attributes[x, y].value - 1) * 32, 32, 32);
                                tmpSprite.Position = new Vector2f(x * 32 + 32, y * 32 + 32);
                                _renderwindow.Draw(tmpSprite);
                            }
                        }

                    }
                    tileSelectionRect = new RectangleShape(new Vector2f(32, 32))
                    {
                        Position = new Vector2f(Globals.CurTileX*32 + 32, Globals.CurTileY*32 + 32),
                        FillColor = Color.White
                    };
                    tileSelectionRect.FillColor = Color.Transparent;
                    tileSelectionRect.OutlineColor = Color.White;
                    tileSelectionRect.OutlineThickness = 1;
                    _renderwindow.Draw(tileSelectionRect);
                    break;
                case Constants.LayerCount + 1:

                    break;
                case Constants.LayerCount + 2:
                    //Draw Blocks
                    for (var x = 0; x < Constants.MapWidth; x++)
                    {
                        for (var y = 0; y < Constants.MapHeight; y++)
                        {
                            if (Globals.GameMaps[Globals.CurrentMap].FindEventAt(x, y) == null) continue;
                            tmpSprite = new Sprite(_eventTex)
                            {
                                TextureRect = new IntRect(0, 0, 32, 32),
                                Position = new Vector2f(x*32 + 32, y*32 + 32)
                            };
                            _renderwindow.Draw(tmpSprite);
                        }

                    }
                    tileSelectionRect = new RectangleShape(new Vector2f(32, 32))
                    {
                        Position = new Vector2f(Globals.CurTileX*32 + 32, Globals.CurTileY*32 + 32),
                        FillColor = Color.White
                    };
                    tileSelectionRect.FillColor = Color.Transparent;
                    tileSelectionRect.OutlineColor = Color.White;
                    tileSelectionRect.OutlineThickness = 1;
                    _renderwindow.Draw(tileSelectionRect);
                    break;
                default:
                    tileSelectionRect = new RectangleShape(new Vector2f(32, 32));
                    if (Globals.Autotilemode == 0) { tileSelectionRect = new RectangleShape(new Vector2f(32 + (Globals.CurSelW * 32), 32 + (Globals.CurSelH * 32))); }
                    tileSelectionRect.Position = new Vector2f(Globals.CurTileX * 32 + 32, Globals.CurTileY * 32 + 32);
                    tileSelectionRect.FillColor = Color.White;
                    tileSelectionRect.FillColor = Color.Transparent;
                    tileSelectionRect.OutlineColor = Color.White;
                    tileSelectionRect.OutlineThickness = 1;
                    _renderwindow.Draw(tileSelectionRect);
                    break;
            }


        }
        private static void DrawAutoTile(int layerNum, int destX, int destY, int quarterNum, int x, int y, MapStruct map)
        {
            int yOffset = 0, xOffset = 0;

            // calculate the offset
            switch (map.Layers[layerNum].Tiles[x, y].Autotile)
            {
                case Constants.AutotileWaterfall:
                    yOffset = (Globals.WaterfallFrame - 1) * 32;
                    break;
                case Constants.AutotileAnim:
                    xOffset = Globals.AutotileFrame * 64;
                    break;
                case Constants.AutotileCliff:
                    yOffset = -32;
                    break;
            }

            // Draw the quarter
            var tmpSprite = new Sprite(_tilesetTex[map.Layers[layerNum].Tiles[x, y].TilesetIndex])
            {
                TextureRect =
                    new IntRect((int) map.Autotiles.Autotile[x, y].Layer[layerNum].SrcX[quarterNum] + xOffset,
                        (int) map.Autotiles.Autotile[x, y].Layer[layerNum].SrcY[quarterNum] + yOffset, 16, 16),
                Position = new Vector2f(destX, destY)
            };
            _renderwindow.Draw(tmpSprite);
        }
        private static void DrawMapUp()
        {
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (tmpMap == null) { return; }
            if (tmpMap.Up <= -1) return;
            tmpMap = Globals.GameMaps[tmpMap.Up];
            if (tmpMap == null) { return; }
            for (var x = 0; x < Constants.MapWidth; x++)
            {
                for (var y = Constants.MapHeight - 1; y < Constants.MapHeight; y++)
                {
                    for (var z = 0; z < Constants.LayerCount; z++)
                    {
                        if (tmpMap.Layers[z].Tiles[x, y].TilesetIndex <= -1) continue;
                        if (_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex] == null) continue;
                        if (tmpMap.Autotiles.Autotile[x, y].Layer[z].RenderState != Constants.RenderStateNormal)
                        {
                            if (tmpMap.Autotiles.Autotile[x, y].Layer[z].RenderState != Constants.RenderStateAutotile)
                                continue;
                            DrawAutoTile(z, x*32 + 32, (y - Constants.MapHeight + 1)*32, 1, x, y, tmpMap);
                            DrawAutoTile(z, x*32 + 16 + 32, (y - Constants.MapHeight + 1)*32, 2, x, y, tmpMap);
                            DrawAutoTile(z, x*32 + 32, (y - Constants.MapHeight + 1)*32 + 16, 3, x, y, tmpMap);
                            DrawAutoTile(z, x*32 + 16 + 32, (y - Constants.MapHeight + 1)*32 + 16, 4, x, y, tmpMap);
                        }
                        else
                        {
                            var tmpSprite = new Sprite(_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex])
                            {
                                TextureRect = new IntRect(tmpMap.Layers[z].Tiles[x, y].X*32,
                                    tmpMap.Layers[z].Tiles[x, y].Y*32, 32, 32),
                                Position = new Vector2f(x*32 + 32, (y - Constants.MapHeight + 1)*32)
                            };
                            _renderwindow.Draw(tmpSprite);
                        }
                    }
                }

            }
        }
        private static void DrawMapDown()
        {
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (tmpMap == null) { return; }
            if (tmpMap.Down <= -1) return;
            tmpMap = Globals.GameMaps[tmpMap.Down];
            if (tmpMap == null) { return; }
            for (var x = 0; x < Constants.MapWidth; x++)
            {
                for (var y = 0; y < 1; y++)
                {
                    for (var z = 0; z < Constants.LayerCount; z++)
                    {
                        if (tmpMap.Layers[z].Tiles[x, y].TilesetIndex <= -1) continue;
                        if (_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex] == null) continue;
                        switch (tmpMap.Autotiles.Autotile[x, y].Layer[z].RenderState)
                        {
                            case Constants.RenderStateNormal:
                                var tmpSprite = new Sprite(_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex])
                                {
                                    TextureRect =
                                        new IntRect(tmpMap.Layers[z].Tiles[x, y].X*32, tmpMap.Layers[z].Tiles[x, y].Y*32,
                                            32, 32),
                                    Position = new Vector2f(x*32 + 32, 32 + Constants.MapHeight*32)
                                };
                                _renderwindow.Draw(tmpSprite);
                                break;
                            case Constants.RenderStateAutotile:
                                DrawAutoTile(z, x * 32 + 32, 32 + Constants.MapHeight * 32, 1, x, y, tmpMap);
                                DrawAutoTile(z, x * 32 + 16 + 32, 32 + Constants.MapHeight * 32, 2, x, y, tmpMap);
                                DrawAutoTile(z, x * 32 + 32, 32 + Constants.MapHeight * 32 + 16, 3, x, y, tmpMap);
                                DrawAutoTile(z, x * 32 + 16 + 32, 32 + Constants.MapHeight * 32 + 16, 4, x, y, tmpMap);
                                break;
                        }
                    }
                }

            }
        }
        private static void DrawMapLeft()
        {
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (tmpMap == null) { return; }
            if (tmpMap.Left <= -1) return;
            tmpMap = Globals.GameMaps[tmpMap.Left];
            if (tmpMap == null) { return; }
            for (var x = Constants.MapWidth - 1; x < Constants.MapWidth; x++)
            {
                for (var y = 0; y < Constants.MapHeight; y++)
                {
                    for (var z = 0; z < Constants.LayerCount; z++)
                    {
                        if (tmpMap.Layers[z].Tiles[x, y].TilesetIndex <= -1) continue;
                        if (_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex] == null) continue;
                        switch (tmpMap.Autotiles.Autotile[x, y].Layer[z].RenderState)
                        {
                            case Constants.RenderStateNormal:
                                var tmpSprite = new Sprite(_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex])
                                {
                                    TextureRect =
                                        new IntRect(tmpMap.Layers[z].Tiles[x, y].X*32, tmpMap.Layers[z].Tiles[x, y].Y*32,
                                            32, 32),
                                    Position = new Vector2f(0, y*32 + 32)
                                };
                                _renderwindow.Draw(tmpSprite);
                                break;
                            case Constants.RenderStateAutotile:
                                DrawAutoTile(z, 0, y * 32 + 32, 1, x, y, tmpMap);
                                DrawAutoTile(z, 0 + 16, y * 32 + 32, 2, x, y, tmpMap);
                                DrawAutoTile(z, 0, y * 32 + 16 + 32, 3, x, y, tmpMap);
                                DrawAutoTile(z, 0 + 16, y * 32 + 16 + 32, 4, x, y, tmpMap);
                                break;
                        }
                    }
                }

            }
        }
        private static void DrawMapRight()
        {
            var tmpMap = Globals.GameMaps[Globals.CurrentMap];
            if (tmpMap == null) { return; }
            if (tmpMap.Right <= -1) return;
            tmpMap = Globals.GameMaps[tmpMap.Right];
            if (tmpMap == null) { return; }
            for (var x = 0; x < 1; x++)
            {
                for (var y = 0; y < Constants.MapHeight; y++)
                {
                    for (var z = 0; z < Constants.LayerCount; z++)
                    {
                        if (tmpMap.Layers[z].Tiles[x, y].TilesetIndex <= -1) continue;
                        if (_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex] == null) continue;
                        switch (tmpMap.Autotiles.Autotile[x, y].Layer[z].RenderState)
                        {
                            case Constants.RenderStateNormal:
                                var tmpSprite = new Sprite(_tilesetTex[tmpMap.Layers[z].Tiles[x, y].TilesetIndex])
                                {
                                    TextureRect =
                                        new IntRect(tmpMap.Layers[z].Tiles[x, y].X*32, tmpMap.Layers[z].Tiles[x, y].Y*32,
                                            32, 32),
                                    Position = new Vector2f(32 + Constants.MapWidth*32, 32 + y*32)
                                };
                                _renderwindow.Draw(tmpSprite);
                                break;
                            case Constants.RenderStateAutotile:
                                DrawAutoTile(z, 32 + Constants.MapWidth * 32, 32 + y * 32, 1, x, y, tmpMap);
                                DrawAutoTile(z, 32 + Constants.MapWidth * 32 + 16, 32 + y * 32, 2, x, y, tmpMap);
                                DrawAutoTile(z, 32 + Constants.MapWidth * 32, 32 + y * 32 + 16, 3, x, y, tmpMap);
                                DrawAutoTile(z, 32 + Constants.MapWidth * 32 + 16, 32 + y * 32 + 16, 4, x, y, tmpMap);
                                break;
                        }
                    }
                }

            }
        }
        private static void DrawTileset()
        {
            //Draw Current Tileset
            if (Globals.CurrentTileset <= -1) return;
            var tmpSprite = new Sprite(_tilesetTex[Globals.CurrentTileset]);
            _tilesetwindow.Draw(tmpSprite);
            var selX = Globals.CurSelX;
            var selY = Globals.CurSelY;
            var selW = Globals.CurSelW;
            var selH = Globals.CurSelH;
            if (selW < 0)
            {
                selX -= Math.Abs(selW);
                selW = Math.Abs(selW);
            }
            if (selH < 0)
            {
                selY -= Math.Abs(selH);
                selH = Math.Abs(selH);
            }
            var tileSelectionRect = new RectangleShape(new Vector2f(32 + (selW*32), 32 + (selH*32)))
            {
                Position = new Vector2f(selX*32, selY*32),
                FillColor = Color.White
            };
            tileSelectionRect.FillColor = Color.Transparent;
            tileSelectionRect.OutlineColor = Color.White;
            tileSelectionRect.OutlineThickness = 1;
            _tilesetwindow.Draw(tileSelectionRect);
        }
        private static void DrawMapBorders()
        {
            var mapBorderLine = new RectangleShape(new Vector2f(1024, 1))
            {
                Position = new Vector2f(0, 32),
                FillColor = Color.White
            };
            _renderwindow.Draw(mapBorderLine);
            mapBorderLine.Position = new Vector2f(0, 1024 - 32);
            _renderwindow.Draw(mapBorderLine);
            mapBorderLine.Size = new Vector2f(1, 1024);
            mapBorderLine.Position = new Vector2f(32, 0);
            _renderwindow.Draw(mapBorderLine);
            mapBorderLine.Position = new Vector2f(1024 - 32, 0);
            _renderwindow.Draw(mapBorderLine);
            mapBorderLine.Dispose();
        }
        private static void DrawMapOverlay()
        {
            if (OverlayColor.A != Globals.GameMaps[Globals.CurrentMap].AHue || OverlayColor.R != Globals.GameMaps[Globals.CurrentMap].RHue || OverlayColor.G != Globals.GameMaps[Globals.CurrentMap].GHue || OverlayColor.B != Globals.GameMaps[Globals.CurrentMap].BHue)
            {
                OverlayColor = new Color((byte)Globals.GameMaps[Globals.CurrentMap].RHue, (byte)Globals.GameMaps[Globals.CurrentMap].GHue, (byte)Globals.GameMaps[Globals.CurrentMap].BHue, (byte)( Globals.GameMaps[Globals.CurrentMap].AHue));
                OverlayTexture.Clear(OverlayColor);
                OverlayTexture.Display();
            }
            var overlaySprite = new Sprite(OverlayTexture.Texture) { Position = new Vector2f(-32 * 30, -32 * 30) };
            _renderwindow.Draw(overlaySprite);
            overlaySprite.Dispose();
        }
        private static void DrawDarkness()
        {
            if (LightsChanged || CurrentBrightness != Globals.GameMaps[Globals.CurrentMap].Brightness) { InitLighting(); }
            var darkSprite = new Sprite(DarkCacheTexture.Texture) {Position = new Vector2f(-32*30, -32*30)};
            _renderwindow.Draw(darkSprite, new RenderStates(BlendMode.Multiply));
            darkSprite.Dispose();
            if (Globals.CurrentLayer != Constants.LayerCount + 1) return;
            for (var x = 0; x < Constants.MapWidth; x++)
            {
                for (var y = 0; y < Constants.MapHeight; y++)
                {
                    if (Globals.GameMaps[Globals.CurrentMap].FindLightAt(x, y) == null) continue;
                    var tmpSprite = new Sprite(_lightTex)
                    {
                        TextureRect = new IntRect(0, 0, 32, 32),
                        Position = new Vector2f(x*32 + 32, y*32 + 32)
                    };
                    _renderwindow.Draw(tmpSprite);
                }

            }
            Shape tileSelectionRect = new RectangleShape(new Vector2f(32, 32));
            tileSelectionRect.Position = new Vector2f(Globals.CurTileX * 32 + 32, Globals.CurTileY * 32 + 32);
            tileSelectionRect.FillColor = Color.White;
            tileSelectionRect.FillColor = Color.Transparent;
            tileSelectionRect.OutlineColor = Color.White;
            tileSelectionRect.OutlineThickness = 1;
            _renderwindow.Draw(tileSelectionRect);
        }

        //Fogs
        private static void DrawFog()
        {
            float ecTime = Environment.TickCount - _fogUpdateTime;
            _fogUpdateTime = Environment.TickCount;
            if (Globals.GameMaps[Globals.CurrentMap].Fog.Length > 0)
            {
                if (FogFileNames.IndexOf(Globals.GameMaps[Globals.CurrentMap].Fog) > -1)
                {
                    int fogIndex = FogFileNames.IndexOf(Globals.GameMaps[Globals.CurrentMap].Fog);
                    int xCount = (int)(_renderwindow.Size.X / FogTextures[fogIndex].Size.X) + 1;
                    int yCount = (int)(_renderwindow.Size.Y / FogTextures[fogIndex].Size.Y) + 1;

                    _fogCurrentX += (ecTime / 1000f) * Globals.GameMaps[Globals.CurrentMap].FogXSpeed * 2;
                    _fogCurrentY += (ecTime / 1000f) * Globals.GameMaps[Globals.CurrentMap].FogYSpeed * 2;

                    if (_fogCurrentX < FogTextures[fogIndex].Size.X) { _fogCurrentX += FogTextures[fogIndex].Size.X; }
                    if (_fogCurrentX > FogTextures[fogIndex].Size.X) { _fogCurrentX -= FogTextures[fogIndex].Size.X; }
                    if (_fogCurrentY < FogTextures[fogIndex].Size.Y) { _fogCurrentY += FogTextures[fogIndex].Size.Y; }
                    if (_fogCurrentY > FogTextures[fogIndex].Size.Y) { _fogCurrentY -= FogTextures[fogIndex].Size.Y; }

                    for (int x = -1; x < xCount; x++)
                    {
                        for (int y = -1; y < yCount; y++)
                        {
                            var fogSprite = new Sprite(FogTextures[fogIndex]) { Position = new Vector2f(x * FogTextures[fogIndex].Size.X + _fogCurrentX, y * FogTextures[fogIndex].Size.Y + _fogCurrentY) };
                            fogSprite.Color = new Color(255, 255, 255, (byte)Globals.GameMaps[Globals.CurrentMap].FogTransaprency);
                            _renderwindow.Draw(fogSprite);
                        }
                    }
                }
            }
        }

        //Lighting
        private static void InitLighting()
        {
            //If we don't have a light texture, make a base/blank one.
            if (DarkCacheTexture == null)
            {
                DarkCacheTexture = new RenderTexture(Constants.MapWidth * 32 * 3, Constants.MapHeight * 32 * 3);
            }
            CurrentBrightness = (byte)(((float)Globals.GameMaps[Globals.CurrentMap].Brightness / 100f) * 255f);
            DarkCacheTexture.Clear(new Color(CurrentBrightness, CurrentBrightness, CurrentBrightness, 255));

            foreach (var t in Globals.GameMaps[Globals.CurrentMap].Lights)
            {
                double w = CalcLightWidth(t.Range);
                var x = (30 * 32) + (t.TileX * 32 + t.OffsetX) - (int)w / 2 + 32 + 16;
                var y = 30 * 32 + (t.TileY * 32 + t.OffsetY) - (int)w / 2 + 32 + 16;
                AddLight(x, y, (int)w, t.Intensity,  t);
            }
            DarkCacheTexture.Display();
            LightsChanged = false;
            CurrentBrightness = (byte)Globals.GameMaps[Globals.CurrentMap].Brightness;
        }
        private static int CalcLightWidth(int range)
        {
            //Formula that is ~equilivant to Unity spotlight widths, this is so future Unity lighting is possible.
            int[] xVals = { 0, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180 };
            int[] yVals = { 1, 8, 18, 34, 50, 72, 92, 114, 135, 162, 196, 230, 268, 320, 394, 500, 658, 976, 1234, 1600 };
            int w;
            var x = 0;
            while (range >= xVals[x])
            {
                x++;
            }
            if (x > yVals.Length)
            {
                w = yVals[yVals.Length - 1];
            }
            else
            {
                w = yVals[x - 1];
                w += (int)((range - xVals[x - 1]) / ((float)xVals[x] - xVals[x - 1])) * (yVals[x] - yVals[x - 1]);
            }
            return w;
        }
        private static void AddLight(int x1, int y1, int size, double intensity, Light light)
        {
            Bitmap tmpLight;
            //If not cached, create a radial gradent for the light.
            if (light.Graphic == null)
            {
                tmpLight = new Bitmap(size, size);
                var g = System.Drawing.Graphics.FromImage(tmpLight);
                var pth = new GraphicsPath();
                pth.AddEllipse(0, 0, size - 1, size - 1);
                var pgb = new PathGradientBrush(pth)
                {
                    CenterColor = System.Drawing.Color.FromArgb((int)(255 * intensity), 255, 255, 255),
                    SurroundColors = new[] { System.Drawing.Color.Transparent },
                    FocusScales = new PointF(0.8f, 0.8f)
                };
                g.FillPath(pgb, pth);
                g.Dispose();
                light.Graphic = tmpLight;
            }
            else
            {
                tmpLight = light.Graphic;
            }

            var tmpSprite = new Sprite(TexFromBitmap(tmpLight)) { Position = new Vector2f(x1, y1) };
            DarkCacheTexture.Draw(tmpSprite, new RenderStates(BlendMode.Add));
        }
        private static Texture TexFromBitmap(Bitmap bmp)
        {
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return new Texture(ms);
        }

        //Extra
        public static void InitTileset(int index,FrmMain myForm)
        {
            if (!_tilesetsLoaded) { return; }
            Globals.CurrentTileset = index;
            Globals.CurSelX = 0;
            Globals.CurSelY = 0;
            myForm.picTileset.Width = (int)_tilesetTex[index].Size.X;
            myForm.picTileset.Height = (int)_tilesetTex[index].Size.Y;
            _tilesetwindow.SetView(new View(new Vector2f(myForm.picTileset.Width / 2f, myForm.picTileset.Height / 2f), new Vector2f(myForm.picTileset.Width, myForm.picTileset.Height)));
        }
    }
}
