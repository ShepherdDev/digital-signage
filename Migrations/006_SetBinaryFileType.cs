using System;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 6, "1.6.2" )]
    class SetBinaryFileType : ExtendedMigration
    {
        public override void Up()
        {
            RockMigrationHelper.AddAttributeQualifier( SystemGuid.Attribute.CONTENT_CHANNEL_ITEM_SLIDE,
                "binaryFileType", Rock.SystemGuid.BinaryFiletype.CONTENT_CHANNEL_ITEM_IMAGE, Guid.NewGuid().ToString() );
        }

        public override void Down()
        {
        }
    }
}
