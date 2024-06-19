using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Resizetizer.UnitTests.Mocks;

internal class MockTaskItem(string itemSpec, Dictionary<string, string> metadata, bool useLink) : ITaskItem
{
    private readonly IDictionary<string, string> _metadata = metadata;

    public string ItemSpec 
    {
        get => itemSpec;
        set => throw new NotImplementedException(); 
    }

    public ICollection MetadataNames => _metadata.Keys.ToArray();
    public int MetadataCount => MetadataNames.Count;

    public IDictionary CloneCustomMetadata()
    {
        throw new NotImplementedException();
    }

    public void CopyMetadataTo(ITaskItem destinationItem)
    {
        throw new NotImplementedException();
    }

    public string GetMetadata(string metadataName)
    {
        if (_metadata.ContainsKey(metadataName))
        {
            return _metadata[metadataName];
        }

        return metadataName switch
        {
            "Link" => useLink ? ItemSpec : string.Empty,
            "FullPath" => Path.GetFullPath(ItemSpec),
            "DefiningProjectDirectory" => Environment.CurrentDirectory,
            _ => string.Empty
        };
    }

    public void RemoveMetadata(string metadataName)
    {
        throw new NotImplementedException();
    }

    public void SetMetadata(string metadataName, string metadataValue)
    {
        _metadata[metadataName] = metadataValue;
    }

    public static ITaskItem CreateAppIcon(string itemSpec, string foregroundFile, bool useLink = true)
    {
        return new MockTaskItem(itemSpec,
                new Dictionary<string, string>
                {
                    { "ForegroundFile", foregroundFile },
                    { "IsAppIcon", "true" }
                }, useLink);
    }

    public static ITaskItem CreateSplashScreen(string itemSpec, string color = "#FFFFFF", bool useLink = true)
    {
        return new MockTaskItem(itemSpec, new Dictionary<string, string>
        {
            { "Color", color }
        }, useLink);
    }
}
