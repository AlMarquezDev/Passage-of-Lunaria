using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyGroup", menuName = "RPG/Enemy Group")]
public class EnemyGroupData : ScriptableObject
{
    public List<EnemyData> enemies;

    public List<EnemyInstance> CreateEnemies()
    {
        List<EnemyInstance> instances = new();
        foreach (var enemyData in enemies)
        {
            instances.Add(new EnemyInstance(enemyData));
        }
        return instances;
    }
}
