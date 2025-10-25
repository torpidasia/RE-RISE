using UnityEngine;

public class SnapHighlight : MonoBehaviour
{
    public Material highlightMaterial;
    public Material defaultMaterial;

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("⚠ No Renderer found on " + gameObject.name + ". SnapHighlight will skip visuals.");
        }
    }

    public void Highlight(bool state)
    {
        if (rend == null) return;

        if (state && highlightMaterial != null)
            rend.material = highlightMaterial;
        else if (defaultMaterial != null)
            rend.material = defaultMaterial;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HousePart"))
            Highlight(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HousePart"))
            Highlight(false);
    }
}
