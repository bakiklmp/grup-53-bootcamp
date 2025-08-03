using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomTemplate", menuName = "Roguelike/Room Template")]
public class RoomTemplateSO : ScriptableObject
{
    [Header("Room Settings")]

    [TextArea(10, 20)]
    public string layout;

 
}