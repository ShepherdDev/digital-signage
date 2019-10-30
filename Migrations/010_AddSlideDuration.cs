using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 10, "1.8.4" )]
    public class AddSlideDuration : ExtendedMigration
    {
        public override void Up()
        {
            var contentChannelTypeId = ( int ) SqlScalar( "SELECT [Id] FROM [ContentChannelType] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.ContentChannelType.DIGITAL_SIGNAGE } } );

            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannelItem", Rock.SystemGuid.FieldType.INTEGER,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Duration", string.Empty,
                "Overrides the standard slide duration for this single slide.", 0, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_DURATION, "com_shepherdchurch_Duration" );

            Sql( "UPDATE [Attribute] SET [Order] = 4 WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_DURATION } } );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_DURATION );
        }
    }
}
