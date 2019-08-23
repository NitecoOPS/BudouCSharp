namespace BudouCSharpNuget
{
    using EPiServer.ServiceLocation;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;

    [ServiceConfiguration(typeof(NatureLanguageDbContext), Lifecycle = ServiceInstanceScope.Transient)]
    public class NatureLanguageDbContext : DbContext
    {
        // Your context has been configured to use a 'NatureLanguageModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'BudouCSharpNuget.NatureLanguageModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'NatureLanguageModel' 
        // connection string in the application configuration file.
        public NatureLanguageDbContext() : base("name=EPiServerDB")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<NatureLanguageDbContext, Migrations.Configuration>());
            Configuration.ProxyCreationEnabled = false;
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public virtual DbSet<NatureLanguageModel> NatureLanguageData { get; set; }
    }

    public class NatureLanguageModel
    {
        public NatureLanguageModel()
        {
        }

        public int Id { get; set; }

        [Key]
        public string GroupKey { get; set; }

        public string Text { get; set; }
        public string Language { get; set; }
        public bool UseEntity { get; set; }

        public string AnalyzedText { get; set; }
    }
}