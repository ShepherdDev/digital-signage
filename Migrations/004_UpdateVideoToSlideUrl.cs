using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 4, "1.6.2" )]
    class UpdateVideoToSlideUrl : ExtendedMigration
    {
        public override void Up()
        {
            Sql( "UPDATE [Attribute] SET [Name] = 'Slide Url',[Key] = 'SlideUrl' WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_SLIDE_URL } } );
        }

        public override void Down()
        {
        }
    }
}
