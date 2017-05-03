namespace FileUploadDemoServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateDb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BlobFiles",
                c => new
                    {
                        BlobFileId = c.Guid(nullable: false),
                        Name = c.String(maxLength: 255),
                        Description = c.String(maxLength: 255),
                        Size = c.Long(nullable: false),
                        CreatedBy = c.String(maxLength: 255),
                        CreatedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.BlobFileId);
            
            CreateTable(
                "dbo.BlobFileChunks",
                c => new
                    {
                        BlobFileId = c.Guid(nullable: false),
                        ChunkId = c.Int(nullable: false),
                        Length = c.Int(nullable: false),
                        Data = c.Binary(),
                    })
                .PrimaryKey(t => new { t.BlobFileId, t.ChunkId })
                .ForeignKey("dbo.BlobFiles", t => t.BlobFileId, cascadeDelete: true)
                .Index(t => t.BlobFileId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BlobFileChunks", "BlobFileId", "dbo.BlobFiles");
            DropIndex("dbo.BlobFileChunks", new[] { "BlobFileId" });
            DropTable("dbo.BlobFileChunks");
            DropTable("dbo.BlobFiles");
        }
    }
}
