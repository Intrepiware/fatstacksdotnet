using System.IO;
using System.Windows;
using Autofac;
using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
using FatStacks.Desktop.ViewModels;
using FatStacks.Infrastructure.Configuration;
using FatStacks.Infrastructure.Connections;
using FatStacks.Infrastructure.Persistence;
namespace FatStacks.Desktop;

public partial class App : Application
{
    private IContainer? container;
    private ILifetimeScope? bookScope;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            // Log only an error category, never exception text, values, or SQL details.
            System.Diagnostics.Trace.TraceError("category=UnexpectedApplicationFailure");
            MessageBox.Show("An unexpected application error occurred. The application will close.", "Fat Stacks.NET", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; Shutdown(1);
        };
        var builder = new ContainerBuilder();
        builder.RegisterType<WindowsSecretProtector>().As<ISecretProtector>().SingleInstance();
        builder.Register(c => new ProfileStore(Environment.GetEnvironmentVariable("FATSTACKS_PROFILE_PATH") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FatStacks", "profiles.json"), c.Resolve<ISecretProtector>())).As<IProfileStore>().SingleInstance();
        builder.RegisterType<SqlTransport>().SingleInstance();
        builder.RegisterType<ConnectionService>().As<IBookConnectionService>().PropertiesAutowired().SingleInstance();
        builder.RegisterType<DatabaseSetup>().As<IDatabaseSetup>().PropertiesAutowired().SingleInstance();
        builder.RegisterType<BookSessionCoordinator>().SingleInstance();
        builder.RegisterType<ConnectionViewModel>().PropertiesAutowired().SingleInstance();
        builder.RegisterType<StartupViewModel>().PropertiesAutowired().SingleInstance();
        builder.RegisterType<MainWindow>().PropertiesAutowired().SingleInstance();
        container = builder.Build();
        var sessions = container.Resolve<BookSessionCoordinator>();
        sessions.SessionChanged += (_, _) =>
        {
            bookScope?.Dispose(); bookScope = null;
            if (sessions.Active is { } active) bookScope = container.BeginLifetimeScope(b =>
            {
                b.RegisterInstance(active.Profile.Settings);
                b.RegisterType<BookDatabase>().InstancePerLifetimeScope();
                b.RegisterType<StoredProcedureRepository>().PropertiesAutowired().InstancePerLifetimeScope();
            });
        };
        MainWindow = container.Resolve<MainWindow>(); MainWindow.Show();
    }
    protected override void OnExit(ExitEventArgs e) { bookScope?.Dispose(); container?.Dispose(); base.OnExit(e); }
}
