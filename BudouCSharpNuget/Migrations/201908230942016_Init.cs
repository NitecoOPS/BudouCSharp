namespace BudouCSharpNuget.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NatureLanguageModels",
                c => new
                {
                    GroupKey = c.String(nullable: false, maxLength: 128),
                    Id = c.Int(nullable: false),
                    Text = c.String(),
                    Language = c.String(),
                    UseEntity = c.Boolean(nullable: false),
                    AnalyzedText = c.String(),
                })
                .PrimaryKey(t => t.GroupKey);
        }
        
        public override void Down()
        {
            DropIndex("dbo.NatureLanguageModels", new[] { "Language" });
            DropIndex("dbo.NatureLanguageModels", new[] { "Text" });
            DropTable("dbo.NatureLanguageModels");
        }
    }
}
