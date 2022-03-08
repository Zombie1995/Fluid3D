using UnityEngine;

public class MainManagerCPU : MonoBehaviour
{
    [SerializeField] private FluidSettingsCPU _settings;

    [SerializeField] private MoverCPU _mover;

    [SerializeField] private Material _material;

    [SerializeField] private int Width = 128;
    [SerializeField] private int Height = 64;

    private Texture2D _texture;
    private Color32[] _colors;

    private Fluid2DCPU _fluid;

    private void Start()
    {
        _texture = new Texture2D(Width, Height);
        _material.SetTexture("_MainTex", _texture);

        _fluid = new Fluid2DCPU(Width, Height);

        _colors = new Color32[Width * Height];

        _settings.Fluid = _fluid;
        _mover.Fluid = _fluid;
    }

    private void Update()
    {
        _fluid.FluidStep();

        UpdateColors();

        _mover.MoveDensity();
    }

    private void UpdateColors() 
    {
        for (int j = 0; j < Height; j++)
        {
            for (int i = 0; i < Width; i++)
            {
                _colors[((i) + (Width) * (j))] = new Color(0, 0, _fluid.Density[((i + 1) + (Width + 2) * (j + 1))]);
            }
        }
        _texture.SetPixels32(_colors);
        _texture.Apply();
    }
}
