public class Fluid2DCPU
{
    public readonly int Width;
    public readonly int Height;
    public float Diffusion = 0.001f;
    public float Viscocity = 0.0001f;
    public int SolverIterationNumber = 10;
    public float DensityReduction = 0.0001f;
    public float DeltaTime = 5f;

    public float[] Density
    {
        get
        {
            return _dArr;
        }
    }
    public float[] VelocityU
    {
        get
        {
            return _uArr;
        }
    }
    public float[] VelocityV
    {
        get
        {
            return _vArr;
        }
    }

    private float[] _dArr;
    private float[] _d0Arr;
    private float[] _uArr;
    private float[] _u0Arr;
    private float[] _vArr;
    private float[] _v0Arr;

    private int _widthWithBoundaries;
    private int _heightWithBoundaries;
    private int _size;

    public Fluid2DCPU(int width, int height)
    {
        Width = width >= 64 ? width : 64;
        Height = height >= 64 ? height : 64;
        _widthWithBoundaries = Width + 2;
        _heightWithBoundaries = Height + 2;
        _size = (_widthWithBoundaries) * (_heightWithBoundaries);

        _dArr = new float[_size];
        _d0Arr = new float[_size];
        _uArr = new float[_size];
        _vArr = new float[_size];
        _u0Arr = new float[_size];
        _v0Arr = new float[_size];

        ResetField();
    }

    public void AddVelocity(int x, int y, float amountX, float amountY)
    {
        if (!(x >= 0 && y >= 0 && x < _widthWithBoundaries && y < _heightWithBoundaries)) return;

        _uArr[((x) + (_widthWithBoundaries) * (y))] += amountX;
        _vArr[((x) + (_widthWithBoundaries) * (y))] += amountY;
    }
    public void AddDensity(int x, int y, float amount)
    {
        if (!(x >= 0 && y >= 0 && x < _widthWithBoundaries && y < _heightWithBoundaries)) return;

        _dArr[((x) + (_widthWithBoundaries) * (y))] += amount;
    }

    public void FluidStep()
    {
        VelocityStep();
        DensityStep();
    }

    public void DensityStep()
    {
        Diffuse(0, _d0Arr, _dArr, Diffusion);
        Advect(0, _dArr, _d0Arr, _uArr, _vArr);

        ReduceDensity();
    }
    public void VelocityStep()
    {
        Diffuse(1, _u0Arr, _uArr, Viscocity);
        Diffuse(2, _v0Arr, _vArr, Viscocity);
        Project(_u0Arr, _v0Arr, _uArr, _vArr);
        Advect(1, _uArr, _u0Arr, _u0Arr, _v0Arr);
        Advect(2, _vArr, _v0Arr, _u0Arr, _v0Arr);
        Project(_uArr, _vArr, _u0Arr, _v0Arr);
    }

    private void SetBnd(int axesNum, float[] arr)
    {
        for (int i = 1; i <= Height; i++)
        {
            arr[((0) + (_widthWithBoundaries) * (i))] = axesNum == 1 ? -arr[((1) + (_widthWithBoundaries) * (i))] : arr[((1) + (_widthWithBoundaries) * (i))];
            arr[((Width + 1) + (_widthWithBoundaries) * (i))] = axesNum == 1 ? -arr[((Width) + (_widthWithBoundaries) * (i))] : arr[((Width) + (_widthWithBoundaries) * (i))];
        }
        for (int i = 1; i <= Width; i++)
        {
            arr[((i) + (_widthWithBoundaries) * (0))] = axesNum == 2 ? -arr[((i) + (_widthWithBoundaries) * (1))] : arr[((i) + (_widthWithBoundaries) * (1))];
            arr[((i) + (_widthWithBoundaries) * (Height + 1))] = axesNum == 2 ? -arr[((i) + (_widthWithBoundaries) * (Height))] : arr[((i) + (_widthWithBoundaries) * (Height))];
        }
        arr[((0) + (_widthWithBoundaries) * (0))] = 0.5f * (arr[((1) + (_widthWithBoundaries) * (0))] + arr[((0) + (_widthWithBoundaries) * (1))]);
        arr[((0) + (_widthWithBoundaries) * (Height + 1))] = 0.5f * (arr[((1) + (_widthWithBoundaries) * (Height + 1))] + arr[((0) + (_widthWithBoundaries) * (Height))]);
        arr[((Width + 1) + (_widthWithBoundaries) * (0))] = 0.5f * (arr[((Width) + (_widthWithBoundaries) * (0))] + arr[((Width + 1) + (_widthWithBoundaries) * (1))]);
        arr[((Width + 1) + (_widthWithBoundaries) * (Height + 1))] = 0.5f * (arr[((Width) + (_widthWithBoundaries) * (Height + 1))] + arr[((Width + 1) + (_widthWithBoundaries) * (Height))]);
    }
    private void LinSolve(int axesNum, float[] arr, float[] arr0, float a, float b)
    {
        for (int n = 0; n < SolverIterationNumber; n++)
        {
            for (int j = 1; j <= Height; j++)
            {
                for (int i = 1; i <= Width; i++)
                {
                    arr[((i) + (_widthWithBoundaries) * (j))] = (arr0[((i) + (_widthWithBoundaries) * (j))] + a * (arr[((i - 1) + (_widthWithBoundaries) * (j))] + arr[((i + 1) + (_widthWithBoundaries) * (j))] + arr[((i) + (_widthWithBoundaries) * (j - 1))] + arr[((i) + (_widthWithBoundaries) * (j + 1))])) / b;
                }
            }
            SetBnd(axesNum, arr);
        }
    }
    private void Diffuse(int axesNum, float[] arr, float[] arr0, float d)
    {
        float a = DeltaTime * d;
        LinSolve(axesNum, arr, arr0, a, 1 + 4 * a);
    }
    private void Advect(int axesNum, float[] arr, float[] arr0, float[] u, float[] v)
    {
        int i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1;
        for (int j = 1; j <= Height; j++)
        {
            for (int i = 1; i <= Width; i++)
            {
                x = i - DeltaTime * u[((i) + (_widthWithBoundaries) * (j))]; y = j - DeltaTime * v[((i) + (_widthWithBoundaries) * (j))];
                if (x < 0.5f) x = 0.5f; if (x > Width + 0.5f) x = Width + 0.5f; i0 = (int)
                    x; i1 = i0 + 1;
                if (y < 0.5f) y = 0.5f; if (y > Height + 0.5f) y = Height + 0.5f; j0 = (int)
                    y; j1 = j0 + 1;
                s1 = x - i0; s0 = 1 - s1; t1 = y - j0; t0 = 1 - t1;
                arr[((i) + (_widthWithBoundaries) * (j))] = s0 * (t0 * arr0[((i0) + (_widthWithBoundaries) * (j0))] + t1 * arr0[((i0) + (_widthWithBoundaries) * (j1))]) +
                    s1 * (t0 * arr0[((i1) + (_widthWithBoundaries) * (j0))] + t1 * arr0[((i1) + (_widthWithBoundaries) * (j1))]);
            }
        }
        SetBnd(axesNum, arr);
    }
    private void Project(float[] u, float[] v, float[] p, float[] p0)
    {
        for (int j = 1; j <= Height; j++)
        {
            for (int i = 1; i <= Width; i++)
            {
                p0[((i) + (_widthWithBoundaries) * (j))] = -0.5f * (u[((i + 1) + (_widthWithBoundaries) * (j))] - u[((i - 1) + (_widthWithBoundaries) * (j))] + v[((i) + (_widthWithBoundaries) * (j + 1))] - v[((i) + (_widthWithBoundaries) * (j - 1))]); //is divergence but has opposite sign
                p[((i) + (_widthWithBoundaries) * (j))] = 0;
            }
        }
        LinSolve(0, p, p0, 1, 4);
        SetBnd(0, p0); SetBnd(0, p);
        for (int j = 1; j <= Height; j++)
        {
            for (int i = 1; i <= Width; i++)
            {
                u[((i) + (_widthWithBoundaries) * (j))] -= 0.5f * (p[((i + 1) + (_widthWithBoundaries) * (j))] - p[((i - 1) + (_widthWithBoundaries) * (j))]);
                v[((i) + (_widthWithBoundaries) * (j))] -= 0.5f * (p[((i) + (_widthWithBoundaries) * (j + 1))] - p[((i) + (_widthWithBoundaries) * (j - 1))]);
            }
        }
        SetBnd(1, u); SetBnd(2, v);
    }

    private void ReduceDensity()
    {
        for (int i = 0; i < _size; i++)
        {
            _dArr[i] -= DensityReduction * DeltaTime;

            if (_dArr[i] < 0) _dArr[i] = 0;
        }
    }
    private void ResetField() 
    {
        for (int i = 0; i < _size; i++)
        {
            _dArr[i] = 0;
            _d0Arr[i] = 0;
            _uArr[i] = 0;
            _vArr[i] = 0;
            _u0Arr[i] = 0;
            _v0Arr[i] = 0;
        }
    }
}
