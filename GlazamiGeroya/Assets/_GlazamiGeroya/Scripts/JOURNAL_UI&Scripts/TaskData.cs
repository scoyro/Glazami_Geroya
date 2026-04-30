using UnityEngine;

[CreateAssetMenu(fileName = "NewTask", menuName = "Journal/Task")]
public class TaskData : ScriptableObject
{
    [Header("Основное")]
    public string taskTitle;
    [TextArea(2, 5)]
    public string taskDescription;

    [Header("Информация об объекте")]
    [TextArea(3, 8)]
    public string objectInfo; // текст, который покажется при нажатии E на триггере
}