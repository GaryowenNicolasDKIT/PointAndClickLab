namespace GD.Extras
{
    /// <summary>
    /// The kinds of pooled object a scene can ask for. Each value maps to one pool in
    /// <see cref="ObjectPoolProvider"/>.
    /// </summary>
    /// <remarks>
    /// Unity saves an enum field as its number, not its name. Add new values at the end only:
    /// inserting or reordering values changes what every saved pool row refers to.
    /// </remarks>
    public enum PoolType
    {
        Ammo = 0,
        Health = 1,
        Coin = 2,
        Projectile = 3,
        Effect = 4,
        Enemy = 5
    }
}
