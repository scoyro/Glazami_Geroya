using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "JournalData", menuName = "Journal/Journal")]
public class JournalData : ScriptableObject
{
    public string journalTitle = "Полевой журнал";
    public List<TaskData> tasks = new List<TaskData>();
}