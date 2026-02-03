using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mesen.Interop;

namespace Mesen.Utilities
{
    /// <summary>
    /// Helper class for loading embedded ROMs directly from memory or resources.
    /// Use this for creating single-game embedded emulator builds.
    /// </summary>
    public static class EmbeddedRomHelper
    {
        // Dictionary to store embedded ROMs (name -> data)
        private static readonly Dictionary<string, byte[]> _embeddedRoms = new();

        /// <summary>
        /// Register an embedded ROM from a byte array.
        /// Call this during application startup.
        /// </summary>
        /// <param name="name">Display name for the ROM</param>
        /// <param name="romData">ROM data as byte array</param>
        public static void RegisterRom(string name, byte[] romData)
        {
            _embeddedRoms[name] = romData;
        }

        /// <summary>
        /// Register an embedded ROM from an embedded resource.
        /// </summary>
        /// <param name="name">Display name for the ROM</param>
        /// <param name="resourceName">Embedded resource name (e.g., "MyGame.nes")</param>
        public static void RegisterRomFromResource(string name, string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                _embeddedRoms[name] = ms.ToArray();
            }
        }

        /// <summary>
        /// Register an embedded ROM from the EmbeddedRoms folder.
        /// </summary>
        /// <param name="name">Display name for the ROM</param>
        /// <param name="fileName">File name in EmbeddedRoms folder</param>
        public static void RegisterRomFromFile(string name, string fileName)
        {
            string romsFolder = Path.Combine(AppContext.BaseDirectory, "EmbeddedRoms");
            string filePath = Path.Combine(romsFolder, fileName);
            if (File.Exists(filePath))
            {
                _embeddedRoms[name] = File.ReadAllBytes(filePath);
            }
        }

        /// <summary>
        /// Get list of all registered embedded ROMs.
        /// </summary>
        public static IEnumerable<string> GetRegisteredRoms()
        {
            return _embeddedRoms.Keys;
        }

        /// <summary>
        /// Check if any embedded ROMs are registered.
        /// </summary>
        public static bool HasEmbeddedRoms => _embeddedRoms.Count > 0;

        /// <summary>
        /// Load an embedded ROM by name.
        /// </summary>
        /// <param name="name">Name of the registered ROM</param>
        /// <returns>True if loaded successfully</returns>
        public static bool LoadEmbeddedRom(string name)
        {
            if (_embeddedRoms.TryGetValue(name, out byte[]? romData))
            {
                return EmuApi.LoadRomFromMemory(romData, (uint)romData.Length, name);
            }
            return false;
        }

        /// <summary>
        /// Load ROM directly from byte array (without registering).
        /// </summary>
        /// <param name="romData">ROM data</param>
        /// <param name="romName">Optional ROM name for display</param>
        /// <returns>True if loaded successfully</returns>
        public static bool LoadRomFromBytes(byte[] romData, string romName = "embedded.rom")
        {
            return EmuApi.LoadRomFromMemory(romData, (uint)romData.Length, romName);
        }

        /// <summary>
        /// Load the first (or only) registered embedded ROM.
        /// Useful for single-game builds.
        /// </summary>
        /// <returns>True if loaded successfully</returns>
        public static bool LoadDefaultRom()
        {
            foreach (var rom in _embeddedRoms)
            {
                return EmuApi.LoadRomFromMemory(rom.Value, (uint)rom.Value.Length, rom.Key);
            }
            return false;
        }
    }
}
