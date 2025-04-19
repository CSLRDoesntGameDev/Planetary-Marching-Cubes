using Godot;
using static Godot.GD;
using System;
using System.Linq;
using System.Text;

public partial class PlanetChunk : MeshInstance3D
{
	[Export] public int ChunkSize = 32;
	[Export] public float ChunkScale = 2;
	[Export] public int Resolution = 1;
    [Export] public Vector3 WorldOffset;
	[Export] public ProcPlanet Planet;
	SurfaceTool MeshTool = new SurfaceTool();
    [Export] public float HeightThreshold = 0.15f;
	Gradient SteepnessGradient;


    public override void _Ready()
    {
    }
    public void UpdateChunk()
	{
        if (!IsInstanceValid(Planet)) 
        {
            PrintErr("Invalid Planet Instance!");
            return;
        }
		SteepnessGradient = Load<Gradient>("res://MarchingCubes/SteepnessGradient.tres");

		MeshTool = new SurfaceTool();
		MeshTool.Begin(Mesh.PrimitiveType.Triangles);
		for (int x = 0; x < ChunkSize; x++) {
            for (int y = 0; y < ChunkSize; y++) {
                for (int z = 0; z < ChunkSize; z++) {
                    Vector3 vec = new Vector3(x, y, z);
					sbyte[] CubeCorners = new sbyte[8];
					for (int i = 0; i < 8; i++) {
						Vector3I corner = new Vector3I(x, y, z) + (Vector3I)WorldOffset + MarchingTable.Corners[i];
						
						CubeCorners[i] = Planet.PlanetData[corner.X, corner.Y, corner.Z];
					}
					March(vec, GetConfigIndex(CubeCorners), CubeCorners);
                }
            }
        }
		CallDeferred("UpdateGeometry");
	}

	public void March(Vector3 position, int ConfigIndex, sbyte[] cubeCorners)
	{
		if (ConfigIndex == 0 || ConfigIndex == 255) return;

		int edgeIndex = 0;
		for (int t = 0; t < 5; t++) 
		{
			Vector3[] verts = new Vector3[3];
			for (int v = 0; v < 3; v++)
			{
				int triTableValue = MarchingTable.Triangles[ConfigIndex, edgeIndex];
				if (triTableValue == -1) return;

				float vert1Sample = cubeCorners[MarchingTable.Edges[triTableValue, 0]] / 256f;
				float vert2Sample = cubeCorners[MarchingTable.Edges[triTableValue, 1]] / 256f;
				
				float difference = vert2Sample - vert1Sample;
				if (difference == 0) {
					difference = 1f;
				}
				else {
					difference = (HeightThreshold - vert1Sample) / difference;
					difference = Mathf.Clamp(difference, 0f, 1f);
				}

				Vector3 vert1 = position + MarchingTable.Corners[MarchingTable.Edges[triTableValue, 0]];
				Vector3 vert2 = position + MarchingTable.Corners[MarchingTable.Edges[triTableValue, 1]];
				
				Vector3 vertex = vert1 + (vert2 - vert1) * difference;

				verts[v] = vertex;
				edgeIndex++;
			}	

			Vector3 Norm = (verts[1] - verts[0]).Cross(verts[2] - verts[0]).Normalized();
			MeshTool.SetNormal(Norm);
			Vector3 dir = new Vector3(Planet.Radius/2f, Planet.Radius/2f, Planet.Radius/2f).DirectionTo(verts[0]);
			float steepness = 1f - (90f - Mathf.RadToDeg(Mathf.Acos(Norm.Dot( dir ))));
			Color sampled = SteepnessGradient.Sample(steepness);
			MeshTool.SetColor(sampled);
			MeshTool.AddVertex(verts[0] * ChunkScale);
			MeshTool.AddVertex(verts[1] * ChunkScale);
			MeshTool.AddVertex(verts[2] * ChunkScale);
			verts = new Vector3[0];
		}
	}
	
	public int GetConfigIndex(sbyte[] CubeCorners)
	{
		int ConfigIndex = 0;
		for (int i = 0; i < 8; i++)
		{
			if ( ((CubeCorners[i]) / 256f) > HeightThreshold ) ConfigIndex |= 1 << i;
			
		}
		return ConfigIndex;
	}

	public void UpdateGeometry() {
		Mesh = MeshTool.Commit();
		MeshTool.Clear();
		MaterialOverride = Load<StandardMaterial3D>("res://MarchingCubes/NoCullMaterial.tres");
		foreach (Node node in GetChildren()) node.QueueFree();
		CreateTrimeshCollision();
	}
}
