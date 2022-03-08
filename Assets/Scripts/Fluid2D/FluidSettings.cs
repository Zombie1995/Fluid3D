using UnityEngine;
using UnityEngine.UI;

public class FluidSettings : MonoBehaviour
{
    [HideInInspector] public Fluid2D Fluid;

    [SerializeField] private GameObject setButton;
    [SerializeField] private GameObject panel;
    InputField diffusion;
    InputField viscocity;
    InputField densityReduction;
    InputField accuracy;

    private bool setOn = false;

    private void Start()
    {
        diffusion = panel.transform.Find("Diffusion").GetComponent<InputField>();
        viscocity = panel.transform.Find("Viscocity").GetComponent<InputField>();
        densityReduction = panel.transform.Find("Density reduction").GetComponent<InputField>();
        accuracy = panel.transform.Find("Accuracy").GetComponent<InputField>();

        setOn = false;
    }
    
    private void Update()
    {
        if (setOn) 
        {
            float.TryParse(diffusion.text, out Fluid.Diffusion);
            float.TryParse(viscocity.text, out Fluid.Viscocity);
            float.TryParse(densityReduction.text, out Fluid.DensityReduction);
            int.TryParse(accuracy.text, out Fluid.SolverIterationNumber);
        }
    }

    public void SettingOn() 
    {
        setButton.SetActive(false);
        panel.SetActive(true);

        diffusion.text = Fluid.Diffusion.ToString();
        viscocity.text = Fluid.Viscocity.ToString();
        densityReduction.text = Fluid.DensityReduction.ToString();
        accuracy.text = Fluid.SolverIterationNumber.ToString();

        setOn = true;
    }
    public void SettingOff()
    {
        setButton.SetActive(true);
        panel.SetActive(false);

        setOn = false;
    }
}
