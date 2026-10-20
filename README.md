Telerik RadChatListView 13+ and 12 has different interfaces. To switch from newest version (13+) to older (12) just define/undefine TELERIK14 constant in csproj and set required version:

<PropertyGroup>
	<DefineConstants>$(DefineConstants);TELERIK14</DefineConstants>
</PropertyGroup>
<ItemGroup>
	<!--<PackageReference Include="Telerik.UI.for.Maui" Version="12.0.0" />-->
	<PackageReference Include="Telerik.UI.for.Maui" Version="15.0.0" />
</ItemGroup>
