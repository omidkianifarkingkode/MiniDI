namespace MiniDI.Samples
{
    public class GameDiContainer : GlobalDiContainer<GameDiContainer>
    {
        protected override void InstallServices(IServiceRegister services)
        {
            // Audio is TRANSIENT: A new instance is created every time Resolve() is called.
            services.Register<IAudioService, AudioService>(ServiceLifetime.Transient);

            // Open Generic Registration ---
            // Register the unbound generic types. The container will JIT compile 
            // closed versions (like IRepository<PlayerProfile>) when requested.
            services.Register(typeof(IRepository<>), typeof(LocalRepository<>), ServiceLifetime.Singleton);
        }
    }
}
