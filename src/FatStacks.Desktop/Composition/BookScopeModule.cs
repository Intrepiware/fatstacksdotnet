using Autofac;
using FatStacks.Core.Accounts;
using FatStacks.Core.Calendar;
using FatStacks.Core.Journal;
using FatStacks.Core.Connections;
using FatStacks.Core.Register;
using FatStacks.Desktop.ViewModels;
using FatStacks.Infrastructure.Accounts;
using FatStacks.Infrastructure.Persistence;
using FatStacks.Infrastructure.Register;
namespace FatStacks.Desktop.Composition;

// Keep active-book registrations together so their ownership can be tested without WPF.
public sealed class BookScopeModule(BookSession session) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // BookSessionCoordinator cancels and disposes this instance after writes finish.
        builder.RegisterInstance(session).ExternallyOwned();
        builder.RegisterInstance(session.Profile.Settings);
        builder.RegisterInstance(new BookCalendar(session.Book.TimeZoneId));
        builder.RegisterType<JournalService>().As<IJournalService>().PropertiesAutowired();
        builder.RegisterType<JournalEditorViewModel>().PropertiesAutowired();
        builder.RegisterType<AccountService>().As<IAccountService>().PropertiesAutowired();
        builder.RegisterType<RegisterService>().As<IRegisterService>().PropertiesAutowired();
        builder.RegisterType<MainWindowViewModel>().PropertiesAutowired();
        builder.RegisterType<AccountSetupViewModel>().PropertiesAutowired();
        builder.RegisterType<BookDatabase>().InstancePerLifetimeScope();
        builder.RegisterType<StoredProcedureRepository>().AsSelf().As<IJournalStore>().PropertiesAutowired().InstancePerLifetimeScope();
    }
}
