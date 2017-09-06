using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.DigitalSignage.Migrations
{
    [MigrationNumber( 7, "1.6.2" )]
    class AddContentChannelAttributes : ExtendedMigration
    {
        public override void Up()
        {
            var contentChannelTypeId = ( int ) SqlScalar( "SELECT [Id] FROM [ContentChannelType] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.ContentChannelType.DIGITAL_SIGNAGE } } );

            //
            // Add the attributes to the content channel.
            //
            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannel", Rock.SystemGuid.FieldType.INTEGER,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Slide Interval", string.Empty,
                "How long each slide should remain on screen before the next transition happens. Default is to use values defined on the display block. Must be at least 4 seconds.", 0, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_SLIDE_INTERVAL, "com_shepherdchurch_SlideInterval" );

            RockMigrationHelper.AddEntityAttribute( "Rock.Model.ContentChannel", Rock.SystemGuid.FieldType.MULTI_SELECT,
                "ContentChannelTypeId", contentChannelTypeId.ToString(), "Transitions", string.Empty,
                "Which transitions should be used. If none are selected then the value defined on the display block will be used.", 0, string.Empty,
                SystemGuid.Attribute.CONTENT_CHANNEL_TRANSITIONS, "com_shepherdchurch_Transitions" );

            RockMigrationHelper.AddAttributeQualifier( SystemGuid.Attribute.CONTENT_CHANNEL_TRANSITIONS,
                "values", "bars^Bars,blinds^Blinds,blocks^Blocks,blocks2^Blocks 2,dissolve^Dissolve,slide^Slide,zip^Zip,bars3d^Bars 3D,blinds3d^Blinds 3D,cube^Cube 3D,tiles3d^Tiles 3D,turn^Turn 3D",
                SystemGuid.AttributeQualifier.CONTENT_CHANNEL_TRANSITIONS_VALUES );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_TRANSITIONS );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.CONTENT_CHANNEL_SLIDE_INTERVAL );
        }
    }
}
