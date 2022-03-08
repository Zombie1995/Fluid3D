using UnityEngine;

public class Fluid2D : MonoBehaviour
{
    [SerializeField] private ComputeShader _compute;
    [SerializeField] private Material _material;

    public float Diffusion = 0.001f;
    public float Viscocity = 0.0001f;
    public int SolverIterationNumber = 10;
    public float DensityReduction = 0.0001f;
    public float DeltaTime = 5f;

    public int Width
    {
        get
        {
            return _width;
        }
    }
    public int Height
    {
        get
        {
            return _height;
        }
    }

    private int _width;
    private int _height;
    private int _widthWithBoundaries;
    private int _heightWithBoundaries;
    private int _size;

    private ComputeBuffer _dArr;
    private ComputeBuffer _d0Arr;
    private ComputeBuffer _uArr;
    private ComputeBuffer _u0Arr;
    private ComputeBuffer _vArr;
    private ComputeBuffer _v0Arr;

    private RenderTexture _texture;

    private int _linSolveKernel;
    private int _advectKernel;
    private int _determinePressureKernel;
    private int _applyProjectionKernel;
    private int _reduceDensityKernel;
    private int _resetFieldKernel;
    private int _bufferToTextureKernel;

    private delegate void AddVelocityDelegate(int x, int y, float amountX, float amountY);
    private AddVelocityDelegate _addVelocityDelegate;

    private delegate void AddDensityDelegate(int x, int y, float amount);
    private AddDensityDelegate _addDensityDelegate;

    private delegate void FluidStepDelegate();
    private FluidStepDelegate _fluidStepDelegate;

    public void CreateFluid(int width, int height)
    {
        _width = width >= 64 ? width : 64;
        _height = height >= 64 ? height : 64;
        _widthWithBoundaries = _width + 2;
        _heightWithBoundaries = _height + 2;
        _size = (_widthWithBoundaries) * (_heightWithBoundaries);

        _linSolveKernel = _compute.FindKernel("LinSolve");
        _advectKernel = _compute.FindKernel("Advect");
        _determinePressureKernel = _compute.FindKernel("DeterminePressure");
        _applyProjectionKernel = _compute.FindKernel("ApplyProjection");
        _reduceDensityKernel = _compute.FindKernel("ReduceDensity");
        _resetFieldKernel = _compute.FindKernel("ResetField");
        _bufferToTextureKernel = _compute.FindKernel("BufferToTexture");

        _compute.SetInt("width", Width);
        _compute.SetInt("height", Height);
        _compute.SetInt("widthWithBoundaries", _widthWithBoundaries);
        _compute.SetInt("heightWithBoundaries", _heightWithBoundaries);
        _compute.SetInt("size", _size);

        _dArr = new ComputeBuffer(_size, 4);
        _d0Arr = new ComputeBuffer(_size, 4);
        _uArr = new ComputeBuffer(_size, 4);
        _vArr = new ComputeBuffer(_size, 4);
        _u0Arr = new ComputeBuffer(_size, 4);
        _v0Arr = new ComputeBuffer(_size, 4);

        ResetField();

        _texture = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        _texture.enableRandomWrite = true;
        _texture.Create();

        _material.SetTexture("_MainTex", _texture);
        _compute.SetTexture(_bufferToTextureKernel, "b2t_texture", _texture);
        _compute.SetBuffer(_bufferToTextureKernel, "b2t_b", _dArr);

        _addVelocityDelegate = (int x, int y, float amountX, float amountY) =>
        {
            if (!(x >= 0 && y >= 0 && x < _width && y < _height)) return;

            float[] velocityVector = new float[2];
            velocityVector[0] = amountX;
            velocityVector[1] = amountY;
            _uArr.SetData(velocityVector, 0, ((x + 1) + (_widthWithBoundaries) * (y + 1)), 1);
            _vArr.SetData(velocityVector, 1, ((x + 1) + (_widthWithBoundaries) * (y + 1)), 1);
        };
        _addDensityDelegate = (int x, int y, float amount) =>
        {
            if (!(x >= 0 && y >= 0 && x < _width && y < _height)) return;

            float[] dencityAmount = new float[1];
            dencityAmount[0] = amount;
            _dArr.SetData(dencityAmount, 0, ((x + 1) + (_widthWithBoundaries) * (y + 1)), 1);
        };
        _fluidStepDelegate = () =>
        {
            _compute.SetFloat("diffusion", Diffusion);
            _compute.SetFloat("viscocity", Viscocity);
            _compute.SetInt("solverIterationNumber", SolverIterationNumber);
            _compute.SetFloat("densityReduction", DensityReduction);
            _compute.SetFloat("deltaTime", DeltaTime);

            VelocityStep();
            DensityStep();

            BufferToTexture();
        };
    }

    public void AddVelocity(int x, int y, float amountX, float amountY)
    {
        _addVelocityDelegate?.Invoke(x, y, amountX, amountY);
    }
    public void AddDensity(int x, int y, float amount)
    {
        _addDensityDelegate?.Invoke(x, y, amount);
    }

    public void FluidStep()
    {
        _fluidStepDelegate?.Invoke();
    }

    private void OnDisable()
    {
        _dArr?.Release();
        _d0Arr?.Release();
        _uArr?.Release();
        _vArr?.Release();
        _u0Arr?.Release();
        _v0Arr?.Release();
    }

    private void DensityStep()
    {
        Diffuse(_d0Arr, _dArr, Diffusion);
        Advect(_dArr, _d0Arr, _uArr, _vArr);

        ReduceDensity();
    }
    private void VelocityStep()
    {
        Diffuse(_u0Arr, _uArr, Viscocity);
        Diffuse(_v0Arr, _vArr, Viscocity);
        Project(_u0Arr, _v0Arr, _uArr, _vArr);
        Advect(_uArr, _u0Arr, _u0Arr, _v0Arr);
        Advect(_vArr, _v0Arr, _u0Arr, _v0Arr);
        Project(_uArr, _vArr, _u0Arr, _v0Arr);
    }

    private void LinSolve(ComputeBuffer arr, ComputeBuffer arr0, float a, float b)
    {
        _compute.SetBuffer(_linSolveKernel, "ls_arr", arr);
        _compute.SetBuffer(_linSolveKernel, "ls_arr0", arr0);
        _compute.SetFloat("ls_a", a);
        _compute.SetFloat("ls_b", b);
        _compute.Dispatch(_linSolveKernel, _size / 16 + 1, _size / 16 + 1, 1);
    }
    private void Diffuse(ComputeBuffer arr, ComputeBuffer arr0, float d)
    {
        float a = DeltaTime * d;
        LinSolve(arr, arr0, a, 1 + 4 * a);
    }
    private void Advect(ComputeBuffer arr, ComputeBuffer arr0, ComputeBuffer u, ComputeBuffer v)
    {
        _compute.SetBuffer(_advectKernel, "ad_arr", arr);
        _compute.SetBuffer(_advectKernel, "ad_arr0", arr0);
        _compute.SetBuffer(_advectKernel, "ad_u", u);
        _compute.SetBuffer(_advectKernel, "ad_v", v);
        _compute.Dispatch(_advectKernel, _size / 16 + 1, _size / 16 + 1, 1);
    }
    private void Project(ComputeBuffer u, ComputeBuffer v, ComputeBuffer p, ComputeBuffer p0)
    {
        _compute.SetBuffer(_determinePressureKernel, "dp_p", p);
        _compute.SetBuffer(_determinePressureKernel, "dp_p0", p0);
        _compute.SetBuffer(_determinePressureKernel, "dp_u", u);
        _compute.SetBuffer(_determinePressureKernel, "dp_v", v);
        _compute.Dispatch(_determinePressureKernel, _size / 16 + 1, _size / 16 + 1, 1);
        LinSolve(p, p0, 1, 4);
        _compute.SetBuffer(_applyProjectionKernel, "ap_u", u);
        _compute.SetBuffer(_applyProjectionKernel, "ap_v", v);
        _compute.SetBuffer(_applyProjectionKernel, "ap_p", p);
        _compute.Dispatch(_applyProjectionKernel, _size / 16 + 1, _size / 16 + 1, 1);
    }

    private void ReduceDensity()
    {
        _compute.SetBuffer(_reduceDensityKernel, "rd_d", _dArr);
        _compute.Dispatch(_reduceDensityKernel, _size / 256 + 1, 1, 1);
    }
    private void ResetField()
    {
        _compute.SetBuffer(_resetFieldKernel, "rf_d", _dArr);
        _compute.SetBuffer(_resetFieldKernel, "rf_u", _uArr);
        _compute.SetBuffer(_resetFieldKernel, "rf_v", _vArr);
        _compute.Dispatch(_resetFieldKernel, _size / 256 + 1, 1, 1);
    }
    private void BufferToTexture()
    {
        _compute.Dispatch(_bufferToTextureKernel, _size / 16 + 1, _size / 16 + 1, 1);
    }
}
