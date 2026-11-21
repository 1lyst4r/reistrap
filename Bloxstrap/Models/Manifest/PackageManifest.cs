/*
 * Roblox Studio Mod Manager (ProjectSrc/Utility/PackageManifest.cs)
 * MIT License
 * Copyright (c) 2015-present MaximumADHD
*/

namespace Bloxstrap.Models.Manifest
{
    public class PackageManifest : List<Package>
    {
        public PackageManifest(string data)
        {
            using var reader = new StringReader(data);
            string? version = reader.ReadLine();

            if (version != "v0")
                throw new NotSupportedException($"Unexpected package manifest version: {version} (expected v0!)");

            while (reader.ReadLine() is string fileName)
            {
                if (String.IsNullOrWhiteSpace(fileName))
                {
                    if (String.IsNullOrWhiteSpace(reader.ReadToEnd()))
                        break;
                    throw new InvalidDataException("Package name missing from manifest");
                }

                string? signature = reader.ReadLine();
                string? rawPackedSize = reader.ReadLine();
                string? rawSize = reader.ReadLine();
                if (fileName == "RobloxPlayerLauncher.exe")
                    continue;

                if (String.IsNullOrWhiteSpace(signature)
                    || !long.TryParse(rawPackedSize, NumberStyles.None, CultureInfo.InvariantCulture, out long packedSize)
                    || !long.TryParse(rawSize, NumberStyles.None, CultureInfo.InvariantCulture, out long size))
                    throw new InvalidDataException($"Incomplete or invalid package manifest entry: {fileName}");

                Add(new Package
                {
                    Name = fileName,
                    Signature = signature,
                    PackedSize = packedSize,
                    Size = size
                });
            }
        }
    }
}
