using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 9, "1.6.4" )]
    public class FixIsSystemAttributes : ExtendedMigration
    {
        public override void Up()
        {
            Sql( "UPDATE [Attribute] SET [IsSystem] = 1 WHERE [Guid] = @Guid",
                "Guid", SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_SLIDE );
        }

        public override void Down()
        {
        }
    }
}
