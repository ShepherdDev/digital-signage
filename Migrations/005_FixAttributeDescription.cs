using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 5, "1.6.2" )]
    class FixAttributeDescription : ExtendedMigration
    {
        public override void Up()
        {
            Sql( "UPDATE [Attribute] SET [Description] = 'A link to the image, video or audio mp3 file to be used.' WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_SLIDE_URL } } );
        }

        public override void Down()
        {
        }
    }
}
