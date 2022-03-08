using UnityEngine;

public class MoverCPU : MonoBehaviour
{
    public Fluid2DCPU Fluid;

    public float VelocityAddMultiplier = 1.5f;

    [SerializeField] private Camera _cam;

    private int _prMousePositionX = 0;
    private int _prMousePositionY = 0;

    private bool _firstTouch = true;

    public void MoveDensity() 
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            if (!Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), out hit))
            {
                _firstTouch = true;
                return;
            }

            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x *= Fluid.Width;
            pixelUV.y *= Fluid.Height;

            if (!_firstTouch)
            {
                Fluid.AddDensity(_prMousePositionX, _prMousePositionY, Random.Range(1, 50));
                Fluid.AddVelocity(_prMousePositionX, _prMousePositionY, VelocityAddMultiplier * ((int)pixelUV.x - _prMousePositionX), VelocityAddMultiplier * ((int)pixelUV.y - _prMousePositionY));
            }

            _prMousePositionX = (int)pixelUV.x;
            _prMousePositionY = (int)pixelUV.y;

            _firstTouch = false;
        }
        else
        {
            _firstTouch = true;
        }
    }
}
