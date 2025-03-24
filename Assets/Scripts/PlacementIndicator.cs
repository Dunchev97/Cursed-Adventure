using UnityEngine;

public class PlacementIndicator : MonoBehaviour
{
    private Material material;
    public Color validColor = new Color(0, 1, 0, 0.3f); // Зеленый полупрозрачный
    public Color invalidColor = new Color(1, 0, 0, 0.3f); // Красный полупрозрачный
    
    private Transform followTransform;
    
    void Start()
{
    // Создаем визуальный индикатор
    GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    indicator.transform.parent = transform;
    indicator.transform.localPosition = Vector3.zero;
    indicator.transform.localScale = new Vector3(1.0f, 0.1f, 1.0f);
    
    // Удаляем коллайдер
    Destroy(indicator.GetComponent<Collider>());
    
    // Создаем материал
    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    material.color = validColor;
    indicator.GetComponent<Renderer>().material = material;
    
    // Находим и удаляем любые текстовые компоненты
    TextMesh[] texts = GetComponentsInChildren<TextMesh>();
    foreach (TextMesh text in texts)
    {
        Destroy(text.gameObject);
    }
    
    // Также проверяем и удаляем объекты с TextMeshPro
    TMPro.TextMeshPro[] tmProTexts = GetComponentsInChildren<TMPro.TextMeshPro>();
    foreach (TMPro.TextMeshPro tmProText in tmProTexts)
    {
        Destroy(tmProText.gameObject);
    }
}
    
    public void SetValidPlacement(bool isValid)
    {
        if (material != null)
        {
            material.color = isValid ? validColor : invalidColor;
        }
    }
    
    public void SetFollowTransform(Transform trans)
    {
        followTransform = trans;
    }
    
    void Update()
    {
        if (followTransform != null)
        {
            transform.position = followTransform.position;
        }
    }
}