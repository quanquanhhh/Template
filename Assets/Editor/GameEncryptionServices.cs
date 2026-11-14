// Author : xuejie.guo
using System;
using System.IO;
using YooAsset;

namespace MythMerge.Editor
{
    /// <summary>在 YooAsset 构建窗口选择此加密器，与 GameDecryptionServices 配套使用。</summary>
    public sealed class GameEncryptionServices : IBundleEncryptor
    {
        public BundleEncryptResult Encrypt(BundleEncryptArgs args)
        {
            var settings = LauncherSettings.Instance;
            if (settings == null)
                throw new InvalidOperationException("LauncherSettings asset is missing.");

            byte[] encrypted = GameDecryptionServices.Encrypt(
                File.ReadAllBytes(args.FilePath), settings._AesKey);
            return new BundleEncryptResult(true, encrypted);
        }
    }
}
