using UnityEngine;

public class MainManager : MonoBehaviour
{
    [SerializeField] private FluidSettings _settings;

    [SerializeField] private Mover _mover;

    [SerializeField] private Fluid2D _fluid;

    [SerializeField] private int Width = 128;
    [SerializeField] private int Height = 64;

    private void Start()
    {
        _fluid.CreateFluid(Width, Height);

        _settings.Fluid = _fluid;
        _mover.Fluid = _fluid;
    }

    private void Update()
    {
        _fluid.FluidStep();

        _mover.MoveDensity();
    }
}
