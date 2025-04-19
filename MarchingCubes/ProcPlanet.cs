using Godot;
using Godot.Collections;

using System;
using System.Threading;
using System.Threading.Tasks;
using static Godot.GD;

public partial class ProcPlanet : Node3D
{
    public sbyte[,,] PlanetData;
    [Export] public int Radius = 128;
    [Export] public int ChunkSize = 32;
	[Export] public float ChunkScale = 2;
    [Export] public FastNoiseLite noise;
	[Export] public float NoiseScale = 2f;
	[Export] public float SpinSpeed = 1f;


    public override void _Ready() 
    {
        CreatePlanetData();
        CreateChunks();
    }

    public override void _Process(double delta)
    {
        Rotate(GlobalBasis.Y.Normalized(), SpinSpeed * (float)delta);
    }

    public void CreatePlanetData()
    {
        if (!IsInstanceValid(noise)) 
        {
            PrintErr("Invalid Noise Instance!");
            return;
        }

        PlanetData = new sbyte[Radius + 1, Radius + 1, Radius + 1];
        int layers = Radius + Radius/ChunkSize + 1;
        Vector2I MidPoint2D = new Vector2I(Radius/2, Radius/2);
        Vector3I MidPoint3D = new Vector3I(Radius/2, Radius/2, Radius/2);
        for (int y = 0; y < Radius + 1; y++) {
        //  Godot.Image img = Godot.Image.CreateEmpty(Radius + Radius/ChunkSize + 1, Radius + Radius/ChunkSize + 1, false, Image.Format.L8);

            for (int x = 0; x < Radius + 1; x++) {
                float radiusVariation = (Mathf.Abs(y - layers * 0.5f) / (layers * 3f));
                float adjustedRadius = (layers + 1) * (1.0f - radiusVariation) * (1.0f + radiusVariation) + 2f;
                for (int z = 0; z < Radius + 1; z++) {
                    Vector2I pxl = new Vector2I(x, z);
					Vector3I vxl = new Vector3I(x, y, z);
					float distance = Mathf.Sqrt(MidPoint3D.DistanceSquaredTo(vxl));
                   
                    float normalizedDistance = distance / adjustedRadius;
                    float steepness = 8.0f; // Adjust this value to control steepness
                    float steepValue = Mathf.Pow(1.0f - normalizedDistance, steepness);

                    float floatValue = steepValue * 127f;
                    sbyte Value = (sbyte)Mathf.Clamp(floatValue, -127, 127);
                   
                    Vector3 vec = new Vector3(x, y, z);
                    sbyte noiseValue = (sbyte)(noise.GetNoise3Dv(vec) * NoiseScale);
                    
                    Value = (sbyte)(Value + (noiseValue * (1f - Mathf.Pow(1.0f - normalizedDistance, 4f) )));
                    
                    PlanetData[x, y, z] = Value;
                    // img.SetPixel(x, z, new Color((float)Value/255f, (float)Value/255f, (float)Value/127f));
                }
            }
            // img.SavePng($"user://{y}.png");
        }
    }

    public void CreateChunks()
    {
        for (int x = 0; x < Radius / ChunkSize; x++) {
            for (int y = 0; y < Radius / ChunkSize; y++) {
                for (int z = 0; z < Radius / ChunkSize; z++) {
                    PlanetChunk planetChunk = new PlanetChunk();
                    Vector3 off = new Vector3(x * ChunkSize, y * ChunkSize, z * ChunkSize);
                    AddChild(planetChunk);
                    planetChunk.WorldOffset = off;
                    planetChunk.ChunkSize = ChunkSize;
                    planetChunk.ChunkScale = ChunkScale;
                    planetChunk.Planet = this;
                    planetChunk.Position = off * ChunkScale - new Vector3(Radius/2f, Radius/2f, Radius/2f);
                    
                    Thread t = new Thread(new ThreadStart(planetChunk.UpdateChunk));
                    t.Start();
                }
            }
        }
    }
}
