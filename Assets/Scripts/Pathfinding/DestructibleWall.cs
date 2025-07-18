using UnityEngine;


//A placeholder class to show how dynamic pathfinding works
public class DestructibleWall : MonoBehaviour
{
    private Collider2D myCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.J))       
        {
            OnDestroyed();
        }
    }
    public void OnDestroyed()
    {
        // Tell the GridManager to update the area this wall used to occupy
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UpdateGridRegion(myCollider.bounds);
        }
        Destroy(gameObject);
    }
}