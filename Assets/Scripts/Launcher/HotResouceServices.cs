// Author : xuejie.guo
using System;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.IO;
#endif
using System.Security.Cryptography;
using System.Text;
using YooAsset;

namespace MythMerge
{
    /// <summary>为 Launcher 和直接初始化资源模块的入口提供相同的加密文件系统配置。</summary>
    public static class HotResourceServices
    {
        public static InitializePackageOptions CreateHostOptions(IRemoteService remoteService, string secret)
        {
            if (remoteService == null) throw new ArgumentNullException(nameof(remoteService));
            var decryptor = new GameDecryptionServices(secret);
#if UNITY_WEBGL && !UNITY_EDITOR
            var builtin = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(true);
            var remote = FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(remoteService, true);
            ConfigureDecryptor(builtin, decryptor);
            ConfigureDecryptor(remote, decryptor);
            return new WebPlayModeOptions
            {
                WebServerFileSystemParameters = builtin,
                WebNetworkFileSystemParameters = remote,
                // 内存解密期间同时存在密文、明文及引擎数据，Web 先串行加载以控制峰值。
                BundleLoadingMaxConcurrency = 1
            };
#else
            var builtin = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            var cache = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService);
            ConfigureDecryptor(builtin, decryptor);
            ConfigureDecryptor(cache, decryptor);
            return new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = builtin,
                CacheFileSystemParameters = cache
            };
#endif
        }

        public static InitializePackageOptions CreateOfflineOptions(string secret)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException("Web/mini-game resources require WebPlayModeOptions and a platform download backend.");
#else
            var builtin = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            ConfigureDecryptor(builtin, new GameDecryptionServices(secret));
            return new OfflinePlayModeOptions { BuiltinFileSystemParameters = builtin };
#endif
        }

        private static void ConfigureDecryptor(FileSystemParameters parameters, IBundleDecryptor decryptor)
        {
            parameters.AddParameter(EFileSystemParameter.AssetBundleDecryptor, decryptor);
            parameters.AddParameter(EFileSystemParameter.RawBundleDecryptor, decryptor);
            parameters.AddParameter(EFileSystemParameter.ArchiveBundleDecryptor, decryptor);
        }
    }

    /// <summary>
    /// YooAsset 3.x 内存解密器，兼容 TM5 的 AES-CFB128/PKCS7 密文。
    /// Web/小游戏直接使用 FileData，不依赖系统文件路径或流加载。
    /// </summary>
    public sealed class GameDecryptionServices : IBundleMemoryDecryptor
    {
        // TM5 的 Key 和 128-bit IV 使用同一组字节，因此只支持 16 个 UTF-8 字节。
        public const int KeyByteCount = 16;
        private readonly string _secret;

        public GameDecryptionServices() : this(LauncherSettings.Instance != null
            ? LauncherSettings.Instance._AesKey : null)
        {
        }

        public GameDecryptionServices(string secret)
        {
            _secret = secret;
        }

        public byte[] GetDecryptedData(BundleDecryptArgs args)
        {
            if (args.FileData != null)
                return Decrypt(args.FileData, _secret);

#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException(
                "Encrypted Web bundles require FileData. Configure WebPlayModeOptions and a compatible download backend.");
#else
            if (string.IsNullOrEmpty(args.FilePath))
                throw new ArgumentException("Encrypted bundle has neither FileData nor FilePath.");

            return Decrypt(File.ReadAllBytes(args.FilePath), _secret);
#endif
        }

        /// <summary>打包端与运行时共享算法，不改变已有 TM5 密文格式。</summary>
        public static byte[] Encrypt(byte[] data, string secret)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Transform(data, secret, true);
        }

        public static byte[] Decrypt(byte[] data, string secret)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0 || data.Length % KeyByteCount != 0)
                throw new CryptographicException("Encrypted bundle data is empty or truncated.");

            return Transform(data, secret, false);
        }

        private static byte[] Transform(byte[] data, string secret, bool encrypt)
        {
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("Set LauncherSettings._AesKey before building or loading encrypted bundles.");

            byte[] key = Encoding.UTF8.GetBytes(secret);
            if (key.Length != KeyByteCount)
            {
                Array.Clear(key, 0, key.Length);
                throw new ArgumentException("TM5 resource encryption requires exactly 16 UTF-8 key bytes.", nameof(secret));
            }

            Array.Reverse(key);
            try
            {
                // 保留旧版托管实现；显式固定反馈长度，避免运行时默认值变化。
                using (var aes = new RijndaelManaged())
                {
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CFB;
                    aes.FeedbackSize = 128;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var transform = encrypt
                        ? aes.CreateEncryptor(key, key)
                        : aes.CreateDecryptor(key, key))
                    {
                        // 避免旧 MemoryStream.CopyTo/ToArray 的额外完整拷贝与流生命周期。
                        return transform.TransformFinalBlock(data, 0, data.Length);
                    }
                }
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }
    }
}
