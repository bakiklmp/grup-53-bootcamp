using UnityEngine;

public class TargetDummy : MonoBehaviour
{
    public float health = 50;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();        
    }
    void Update()
    {
        
    }
    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
        {
            spriteRenderer.color = Color.yellow;
            Debug.Log($"Reamining health of {name} is {health}");
        }
    }
}
