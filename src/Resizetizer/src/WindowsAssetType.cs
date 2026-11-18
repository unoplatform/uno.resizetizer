namespace Uno.Resizetizer
{
	/// <summary>
	/// The kind of Windows App SDK image a <see cref="DpiPath"/> produces, which decides its padding.
	/// </summary>
	internal enum WindowsAssetType
	{
		None,
		PlatedIcon,
		UnplatedIcon,
		StoreLogo,
		SmallTile,
		Tile,
	}
}
