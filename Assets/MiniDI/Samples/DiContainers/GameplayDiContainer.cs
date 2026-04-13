namespace MiniDI.Samples
{
    public class GameplayDiContainer : GameObjectScopedDiContainer<GlobalDiContainer>
    {
        private void Awake()
        {
            containerName = "Gameplay DI-Container";
            validateOnBuild = false;
        }

        protected override void InstallServices(IServiceRegister services)
        {
            // Spawner is SCOPED: Only one spawner exists for the duration of this Gameplay session.
            services.Register<IEnemySpawner, EnemySpawner>(ServiceLifetime.Scoped);
        }
    }
}
