using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace GITC.Core
{
    // Central lookup for assets by string key. Keys map to content pipeline asset names in the manifests below.
    // A missing or failed asset never throws: textures fall back to a magenta placeholder, fonts to the default
    // font, and sounds/songs/effects return null (callers null-check). Every miss is logged once per key.
    internal class ContentRegistry
    {
        public ContentManager content { get; set; }
        public GraphicsDevice graphicsDevice { get; set; }
        // 1x1 white texture for rectangles, hitboxes, and the console cursor
        public Texture2D pixel { get; set; }
        public Texture2D placeholder { get; set; }
        public Dictionary<string, Texture2D> textures { get; set; }
        public Dictionary<string, SpriteFont> fonts { get; set; }
        public Dictionary<string, SoundEffect> sounds { get; set; }
        public Dictionary<string, Song> songs { get; set; }
        public Dictionary<string, Effect> effects { get; set; }
        private HashSet<string> warned { get; set; }

        public const string DEFAULTFONT = "default";

        // Manifests: key -> asset name (no extension). Add new assets here.
        public static readonly Dictionary<string, string> textureManifest = new Dictionary<string, string>()
        {
            { "hud", "HUD" },
            { "hud_screen_overlay", "HUD_ScreenOverlay" },
            // Company logos (key = Company.logoKey in companies.json)
            { "logo_fox", "Logo_Swiftail" }
        };
        public static readonly Dictionary<string, string> fontManifest = new Dictionary<string, string>()
        {
            { DEFAULTFONT, "Fonts/File" }
        };
        public static readonly Dictionary<string, string> soundManifest = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> songManifest = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> effectManifest = new Dictionary<string, string>();

        public ContentRegistry(ContentManager content, GraphicsDevice graphicsDevice)
        {
            this.content = content;
            this.graphicsDevice = graphicsDevice;
            textures = new Dictionary<string, Texture2D>();
            fonts = new Dictionary<string, SpriteFont>();
            sounds = new Dictionary<string, SoundEffect>();
            songs = new Dictionary<string, Song>();
            effects = new Dictionary<string, Effect>();
            warned = new HashSet<string>();

            pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Magenta/black checker so a missing texture is obvious on screen
            placeholder = new Texture2D(graphicsDevice, 2, 2);
            placeholder.SetData(new[] { Color.Magenta, Color.Black, Color.Black, Color.Magenta });
        }

        // Loads everything listed in the manifests
        public void LoadAll()
        {
            foreach (KeyValuePair<string, string> kv in textureManifest)
                Load(textures, kv.Key, kv.Value);
            foreach (KeyValuePair<string, string> kv in fontManifest)
                Load(fonts, kv.Key, kv.Value);
            foreach (KeyValuePair<string, string> kv in soundManifest)
                Load(sounds, kv.Key, kv.Value);
            foreach (KeyValuePair<string, string> kv in songManifest)
                Load(songs, kv.Key, kv.Value);
            foreach (KeyValuePair<string, string> kv in effectManifest)
                Load(effects, kv.Key, kv.Value);
        }

        private void Load<T>(Dictionary<string, T> target, string key, string assetName)
        {
            try
            {
                target[key] = content.Load<T>(assetName);
            }
            catch (Exception e)
            {
                Warn(typeof(T).Name + ":" + key, "Could not load " + typeof(T).Name + " '" + key + "' (" + assetName + "): " + e.Message);
            }
        }

        private void Warn(string id, string message)
        {
            if (warned.Add(id))
                System.Diagnostics.Debug.WriteLine("ContentRegistry: " + message);
        }

        public Texture2D GetTexture(string key)
        {
            if (textures.TryGetValue(key, out Texture2D tex))
                return tex;
            Warn("Texture2D:" + key, "Missing Texture2D '" + key + "', using placeholder");
            return placeholder;
        }

        public SpriteFont GetFont(string key)
        {
            if (fonts.TryGetValue(key, out SpriteFont font))
                return font;
            Warn("SpriteFont:" + key, "Missing SpriteFont '" + key + "', using default font");
            return fonts.TryGetValue(DEFAULTFONT, out SpriteFont fallback) ? fallback : null;
        }

        public SoundEffect GetSound(string key)
        {
            if (sounds.TryGetValue(key, out SoundEffect sound))
                return sound;
            Warn("SoundEffect:" + key, "Missing SoundEffect '" + key + "'");
            return null;
        }

        public Song GetSong(string key)
        {
            if (songs.TryGetValue(key, out Song song))
                return song;
            Warn("Song:" + key, "Missing Song '" + key + "'");
            return null;
        }

        public Effect GetEffect(string key)
        {
            if (effects.TryGetValue(key, out Effect effect))
                return effect;
            Warn("Effect:" + key, "Missing Effect '" + key + "'");
            return null;
        }

        // Content-manager assets are unloaded by ContentManager; only the textures created here are disposed
        public void Dispose()
        {
            pixel?.Dispose();
            placeholder?.Dispose();
        }
    }
}
