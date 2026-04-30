using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LmpClient.Extensions
{
    public static class ConfigNodeSerializer
    {
        static ConfigNodeSerializer()
        {
            var configNodeType = typeof(ConfigNode);

            var writeNodeMethodInfo = configNodeType.GetMethod(
                "WriteNode",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (writeNodeMethodInfo != null)
            {
                WriteNodeThunk = (WriteNodeDelegate)Delegate.CreateDelegate(
                    typeof(WriteNodeDelegate),
                    null,
                    writeNodeMethodInfo);
            }

            var preFormatConfigMethodInfo = configNodeType.GetMethod(
                "PreFormatConfig",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (preFormatConfigMethodInfo != null)
            {
                PreFormatConfigThunk = (PreFormatConfigDelegate)Delegate.CreateDelegate(
                    typeof(PreFormatConfigDelegate),
                    null,
                    preFormatConfigMethodInfo);
            }

            var recurseFormatMethodInfo = configNodeType.GetMethod(
                "RecurseFormat",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(List<string[]>) },
                null);

            if (recurseFormatMethodInfo != null)
            {
                RecurseFormatThunk = (RecurseFormatDelegate)Delegate.CreateDelegate(
                    typeof(RecurseFormatDelegate),
                    null,
                    recurseFormatMethodInfo);
            }
        }

        private static WriteNodeDelegate WriteNodeThunk { get; }
        private static PreFormatConfigDelegate PreFormatConfigThunk { get; }
        private static RecurseFormatDelegate RecurseFormatThunk { get; }

        private static bool IsReady =>
            WriteNodeThunk != null &&
            PreFormatConfigThunk != null &&
            RecurseFormatThunk != null;

        public static byte[] Serialize(this ConfigNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            EnsureInitialized();

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                WriteNodeThunk(node, writer);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static void SerializeToArray(this ConfigNode node, byte[] data, out int numBytes)
        {
            numBytes = 0;

            try
            {
                if (node == null)
                    throw new ArgumentNullException(nameof(node));

                if (data == null || data.Length == 0)
                    throw new ArgumentException("Output buffer is null or empty.", nameof(data));

                EnsureInitialized();

                using (var stream = new MemoryStream(data))
                using (var writer = new StreamWriter(stream))
                {
                    WriteNodeThunk(node, writer);
                    writer.Flush();
                    numBytes = (int)stream.Position;
                }
            }
            catch (Exception e)
            {
                LunaLog.LogError($"Error serializing vessel! Details {e}");
                numBytes = 0;
            }
        }

        public static ConfigNode DeserializeToConfigNode(this byte[] data, int numBytes)
        {
            if (data == null || numBytes <= 0 || numBytes > data.Length)
                return null;

            EnsureInitialized();

            using (var stream = new MemoryStream(data, 0, numBytes))
            using (var reader = new StreamReader(stream))
            {
                var lines = new List<string>();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                        lines.Add(line);
                }

                var cfg = PreFormatConfigThunk(lines.ToArray());
                return RecurseFormatThunk(cfg);
            }
        }

        private static void EnsureInitialized()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "ConfigNodeSerializer failed to initialize. KSP ConfigNode private methods were not found.");
            }
        }

        private delegate void WriteNodeDelegate(ConfigNode configNode, StreamWriter writer);
        private delegate List<string[]> PreFormatConfigDelegate(string[] cfgData);
        private delegate ConfigNode RecurseFormatDelegate(List<string[]> cfg);
    }
}