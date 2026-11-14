using GD.Flow;
using GD.Types;
using UnityEngine;

//Spawns one wave of enemies once the player hits start, one at a time
public class EnemyWaveFlow : FlowBehaviour
{
    [SerializeField] private VoidEventChannel startPressed;
    [SerializeField] private DungeonWalker enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform goal;

    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0f)] private float secondsBetweenEnemies = 1.5f;

    protected override FlowSequence BuildSequence()
    {
        FlowBuilder builder = new FlowBuilder().WaitFor(startPressed);

        for (int i = 0; i < enemyCount; i++)
            builder.Do(SpawnEnemy).Wait(secondsBetweenEnemies);

        return builder.Build();
    }

    private void SpawnEnemy()
    {
        DungeonWalker enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.SetGoal(goal);
    }
}
